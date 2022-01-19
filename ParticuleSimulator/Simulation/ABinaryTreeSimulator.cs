using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Generic.Trees;
using ParticleSimulator.Simulation.Baryon;
using ParticleSimulator.Simulation.Particles;

namespace ParticleSimulator.Simulation {
	public abstract class ABinaryTreeSimulator<TParticle, TTree> : ISimulator
	where TParticle : AParticle<TParticle>
	where TTree : ATree<TParticle> {
		protected ABinaryTreeSimulator(TTree tree) {
			this._tree = tree;
		}
		private readonly CountdownEvent _cde = new(0);
		private readonly object _cdeLock = new();
		private ConcurrentBag<Queue<Tuple<TTree, TParticle[]>>> _partitionedLeafData = new();

		public int IterationCount { get; private set; }
		public AParticleGroup<TParticle>[] ParticleGroups { get; private set; }

		protected TTree _tree;

		public int ParticleCount => this._tree is null ? 0 : this._tree.ItemCount;//this.ParticleTree is null ? 0 : this.ParticleTree.Count;
		public ICollection<TParticle> Particles => this._tree;
		IEnumerable<IParticle> ISimulator.Particles => this.Particles;
		
		public abstract BaryCenter Center { get; }
		protected abstract bool AccumulateTreeNodeData { get; }

		protected abstract AParticleGroup<TParticle> NewParticleGroup();
		protected abstract void ComputeInteractions(TTree leaf, TParticle[] particles);

		protected virtual void AccumulateLeafNode(TTree node, TParticle[] particles) => throw null;
		protected virtual void AccumulateInnerNode(TTree node) => throw null;

		protected virtual TTree PruneTree() => (TTree)this._tree.Prune();

		public void Init() {
			this.IterationCount = -1;

			this.ParticleGroups = new AParticleGroup<TParticle>[Parameters.PARTICLES_GROUP_COUNT];
			for (int i = 0; i < this.ParticleGroups.Length; i++) {
				this.ParticleGroups[i] = this.NewParticleGroup();
				this.ParticleGroups[i].Init();
			}

			this._tree.Clear();
			this._tree = (TTree)this._tree
				.Add(this.ParticleGroups.SelectMany(g => g.InitialParticles))
				.Prune();
		}

		public ParticleData[] Update() {
			if (++this.IterationCount > 0)//show starting data on first result
				this.Refresh();
			return this._tree.Select(p => new ParticleData(p)).ToArray();
		}

		private void Refresh() {
			//determine all leaves and aggregate barycenters in parallel
			this.PrepareNode(this._tree, true, null);

			//compute particle interactions on leaves in parallel
			this._cde.Reset(this._partitionedLeafData.Count);
			foreach (Queue<Tuple<TTree, TParticle[]>> nodeLeaves in this._partitionedLeafData)//do not consume yet
				ThreadPool.QueueUserWorkItem(this.NodeInteractionHelper, nodeLeaves);
			this._cde.Wait();

			BaryCenter center = this.Center;
			while (this._partitionedLeafData.TryTake(out Queue<Tuple<TTree, TParticle[]>> nodeLeaves))//random order
				while (nodeLeaves.TryDequeue(out Tuple<TTree, TParticle[]> leaf))
					this.MoveLeafParticles(center, leaf.Item1, leaf.Item2);

			this._tree = this.PruneTree();
		}

		private void MoveLeafParticles(BaryCenter center, TTree originLeaf, TParticle[] particles) {
			ATree<TParticle> leaf;
			TParticle particle;
			for (int j = 0; j < particles.Length; j++) {
				particle = particles[j];
				if (particle.Enabled) {
					leaf = originLeaf;
					while (!leaf.IsLeaf)
						leaf = leaf.Children[leaf.ChildIndex(particle)];

					if (!(particle.Mergers is null)) {
						while (particle.Mergers.TryDequeue(out TParticle other)) {
							if (other.Enabled) {
								other.Enabled = false;
								originLeaf.GetContainingLeaf(other).RemoveFromLeaf(other, false);

								particle.Incorporate(other);
								if (!(other.Mergers is null))
									while (other.Mergers.TryDequeue(out TParticle tail))
										if (particle.Id != tail.Id)
											particle.Mergers.Enqueue(tail);
							}
						}
					}

					particle.ApplyTimeStep(Parameters.TIME_SCALE, center);

					if (particle.Enabled)
						leaf.MoveFromLeaf(particle, false);
					else leaf.RemoveFromLeaf(particle, false);
						
					if (!(particle.NewParticles is null))
						while (particle.NewParticles.TryDequeue(out TParticle other))
							originLeaf.Add(other);
				}
			}
		}

		private void PrepareNode(TTree root, bool isPrep, Queue<Tuple<TTree, TParticle[]>> leaves) {
			//identical recursive behavior is needed for both prep and "normal" modes, so this remains one convoluted method...
			if (isPrep && (root.IsLeaf || Parameters.PARTICLES_PER_BATCH < 1 || root.Count <= Parameters.PARTICLES_PER_BATCH)) {
				Queue<TTree> work = new();
				work.Enqueue(root);
				this._cde.Reset(1);
				ThreadPool.QueueUserWorkItem(this.PrepareNodeHelper, work);
				this._cde.Wait();
			} else if (root.IsLeaf) {
				TParticle[] leafParticles = root.Bin.ToArray();
				leaves.Enqueue(new(root, leafParticles));
				if (this.AccumulateTreeNodeData)
					this.AccumulateLeafNode(root, leafParticles);
			} else {
				int numFilled = 0;
				Queue<TTree> work = isPrep ? new() : null;
				Stack<TTree[]> levelStack = this.AccumulateTreeNodeData ? new() : null;
				Stack<TTree> pendingNodes = new(), testNodes = new();
				pendingNodes.Push(root);
				
				TTree[] levelNodes; TTree child; TParticle[] leafParticles;
				do {
					while (pendingNodes.TryPop(out TTree node)) {
						for (int cIdx = 0; cIdx < node.Children.Length; cIdx++) {
							child = (TTree)node.Children[cIdx];
							if (child.ItemCount > 0) {
								if (isPrep && child.IsLeaf) {
									work.Enqueue(child);
									numFilled += child.ItemCount;
									if (numFilled >= Parameters.PARTICLES_PER_BATCH) {
										lock (this._cdeLock)
											if (this._cde.IsSet)
												this._cde.Reset(1);
											else this._cde.AddCount();
										ThreadPool.QueueUserWorkItem(this.PrepareNodeHelper, work);
										work = new();
										numFilled = 0;
									}
								} else if (child.IsLeaf) {
									leafParticles = child.Bin.ToArray();
									leaves.Enqueue(new(child, leafParticles));
									if (this.AccumulateTreeNodeData)
										this.AccumulateLeafNode(child, leafParticles);
								} else if (isPrep
								&& (numFilled + child.Count <= Parameters.PARTICLES_PER_BATCH
									|| (double)((numFilled + child.Count) - Parameters.PARTICLES_PER_BATCH) / Parameters.PARTICLES_PER_BATCH < 0.1d))
								{
									work.Enqueue(child);
									numFilled += child.ItemCount;
									if (numFilled >= Parameters.PARTICLES_PER_BATCH) {
										lock (this._cdeLock)
											if (this._cde.IsSet)
												this._cde.Reset(1);
											else this._cde.AddCount();
										ThreadPool.QueueUserWorkItem(this.PrepareNodeHelper, work);
										work = new();
										numFilled = 0;
									}
								} else testNodes.Push(child);
							} else node.Children[cIdx].Children = null;
						}
					}
					if (this.AccumulateTreeNodeData && testNodes.Count > 0) {
						levelNodes = new TTree[testNodes.Count];
						testNodes.CopyTo(levelNodes, 0);//casting magic?
						levelStack.Push(levelNodes);
					}
					(pendingNodes, testNodes) = (testNodes, pendingNodes);
				} while (pendingNodes.Count > 0);
				
				if (isPrep) {
					if (work.Count > 0) {
						lock (this._cdeLock)
							if (this._cde.IsSet)
								this._cde.Reset(1);
							else this._cde.AddCount();
						ThreadPool.QueueUserWorkItem(this.PrepareNodeHelper, work);
					}
					this._cde.Wait();
				}

				if (this.AccumulateTreeNodeData) {
					while (levelStack.TryPop(out levelNodes))
						for (int i = 0; i < levelNodes.Length; i++)
							this.AccumulateInnerNode(levelNodes[i]);

					this.AccumulateInnerNode(root);
				}
			}
		}

		private void PrepareNodeHelper(object work) {
			Queue<TTree> nodes = (Queue<TTree>)work;
			Queue<Tuple<TTree, TParticle[]>> result = new();
			while (nodes.TryDequeue(out TTree node))
				this.PrepareNode(node, false, result);

			this._partitionedLeafData.Add(result);

			lock (this._cdeLock)
				this._cde.Signal();
		}

		private void NodeInteractionHelper(object work) {
			Queue<Tuple<TTree, TParticle[]>> nodeLeaves = (Queue<Tuple<TTree, TParticle[]>>)work;
			foreach (Tuple<TTree, TParticle[]> leaf in nodeLeaves)//do not consume the queue
				this.ComputeInteractions(leaf.Item1, leaf.Item2);

			this._cde.Signal();
		}
	}
}