using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Generic.Trees;
using ParticleSimulator.Simulation.Particles;

namespace ParticleSimulator.Simulation {
	public abstract class ABinaryTreeSimulator<TParticle, TTree> : ISimulator
	where TParticle : AParticle<TParticle>
	where TTree : ATree<TParticle> {
		protected ABinaryTreeSimulator(TTree tree) {
			this.ParticleTree = tree;
		}

		public int IterationCount { get; private set; }
		public AParticleGroup<TParticle>[] InitialParticleGroups { get; private set; }
		public TTree ParticleTree { get; private set; }
		IEnumerable<IParticle> ISimulator.Particles => this.ParticleTree.AsEnumerable();
		public int ParticleCount => this.ParticleTree is null ? 0 : this.ParticleTree.ItemCount;//this.ParticleTree is null ? 0 : this.ParticleTree.Count;

		public void Init() {
			this.IterationCount = -1;
			this.InitialParticleGroups = Enumerable
				.Range(0, Parameters.PARTICLES_GROUP_COUNT)
				.Select(i => this.NewParticleGroup())
				.ToArray();
			for (int i = 0; i < this.InitialParticleGroups.Length; i++)
				this.InitialParticleGroups[i].Init();

			this.ParticleTree = (TTree)this.ParticleTree.Add(
				this.InitialParticleGroups.SelectMany(g => g.InitialParticles));
		}

		public abstract Vector<float> Center { get; }

		protected abstract bool AccumulateTreeNodeData { get; }

		protected abstract AParticleGroup<TParticle> NewParticleGroup();
		protected abstract void ProcessLeaf(TTree leaf, TParticle[] particles);

		protected virtual void ComputeLeafNode(TTree child, TParticle[] particles) => throw null;
		protected virtual void ComputeInnerNode(TTree node) => throw null;

		protected virtual TTree PruneTree() => (TTree)this.ParticleTree.Prune();

		public ParticleData[] RefreshSimulation() {
			this.IterationCount++;
			if (this.IterationCount > 0)//show starting data on first result
				if (this.ParticleTree.ItemCount == 0) {
					Program.CancelAction(null, null);
				} else {
					this.ParticleTree = this.PruneTree();
					this.Refresh();
				}

			return this.ParticleTree.Select(p => new ParticleData(p)).ToArray();
		}

		private void Refresh() {
			List<Tuple<TTree, TParticle[]>> leaves = this.PrepareTree();

			Parallel.ForEach(
				leaves,
				leaf => this.ProcessLeaf(
					leaf.Item1,
					leaf.Item2));
			
			Queue<TParticle> pending;
			ATree<TParticle> node, leaf, otherLeaf;
			TParticle particle, other, tail;
			for (int i = 0; i < leaves.Count; i++) {
				for (int j = 0; j < leaves[i].Item2.Length; j++) {
					particle = leaves[i].Item2[j];

					if (particle.Enabled) {
						node = leaves[i].Item1;
						leaf = node.GetContainingLeaf(particle);
						particle.ApplyTimeStep(Parameters.TIME_SCALE, this.ParticleTree);

						if (particle.Enabled) {
							if (!(particle.Mergers is null)) {
								pending = particle.Mergers;
								particle.Mergers = null;

								while (pending.TryDequeue(out other)) {
									if (other.Enabled) {
										other.Enabled = false;
										otherLeaf = node.GetContainingLeaf(other);
										otherLeaf.RemoveFromLeaf(other, false);

										particle.Incorporate(other);

										if (!(other.Mergers is null)) {
											while (other.Mergers.TryDequeue(out tail))
												if (particle.Id != tail.Id)
													pending.Enqueue(tail);
											other.Mergers = null;
										}
									}
								}
							}

							leaf.MoveFromLeaf(particle, false);
						
							if (!(particle.NewParticles is null)) {
								while (particle.NewParticles.TryDequeue(out other))
									node.Add(other);
								particle.NewParticles = null;
							}
						} else leaf.RemoveFromLeaf(particle, false);
					}
				}
			}
		}

		private List<Tuple<TTree, TParticle[]>> PrepareTree() {
			List<Tuple<TTree, TParticle[]>> leaves = new((this.ParticleTree.ItemCount / this.ParticleTree.LeafCapacity) << (this.ParticleTree.LeafCapacity > 1 ? 1 : 0));
			TParticle[] particles;

			if (this.ParticleTree.IsLeaf) {
				particles = this.ParticleTree.Bin.ToArray();
				if (this.AccumulateTreeNodeData)
					this.ComputeLeafNode(this.ParticleTree, particles);
				leaves.Add(new(this.ParticleTree, particles));
			} else {
				Stack<TTree> pendingNodes = new(), testNodes = new();
				Stack<TTree[]> levelStack = new();
				pendingNodes.Push(this.ParticleTree);
				levelStack.Push(new TTree[] { this.ParticleTree });

				TTree[] levelNodes;
				TTree child, node;
				do {
					while (pendingNodes.TryPop(out node))
						for (int cIdx = 0; cIdx < node.Children.Length; cIdx++)
							if (node.Children[cIdx].ItemCount > 0) {
								child = (TTree)node.Children[cIdx];
								if (child.IsLeaf) {
									particles = child.Bin.ToArray();
									if (this.AccumulateTreeNodeData)
										this.ComputeLeafNode(child, particles);
									leaves.Add(new(child, particles));
								} else testNodes.Push(child);
							} else node.Children[cIdx].Children = null;

					if (testNodes.Count > 0) {
						if (this.AccumulateTreeNodeData) {
							levelNodes = new TTree[testNodes.Count];
							testNodes.CopyTo(levelNodes, 0);//casting magic?
							levelStack.Push(levelNodes);
						}
						(pendingNodes, testNodes) = (testNodes, pendingNodes);
					}
				} while (pendingNodes.Count > 0);
				
				if (this.AccumulateTreeNodeData)
					while (levelStack.TryPop(out levelNodes))
						for (int i = 0; i < levelNodes.Length; i++)
							this.ComputeInnerNode(levelNodes[i]);
			}

			return leaves;
		}
	}
}