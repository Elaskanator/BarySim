using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

		public int IterationCount { get; private set; }
		public AParticleGroup<TParticle>[] ParticleGroups { get; private set; }

		protected TTree _tree;
		private readonly Queue<Tuple<TTree, TParticle[]>> _leaves = new();

		public int ParticleCount => this._tree is null ? 0 : this._tree.ItemCount;//this.ParticleTree is null ? 0 : this.ParticleTree.Count;
		public ICollection<TParticle> Particles => this._tree;
		IEnumerable<IParticle> ISimulator.Particles => this.Particles;
		
		public abstract BaryCenter Center { get; }
		protected abstract bool AccumulateTreeNodeData { get; }

		protected abstract AParticleGroup<TParticle> NewParticleGroup();
		protected abstract void ComputeInteractions(TTree leaf, TParticle[] particles);

		protected virtual void ComputeLeafNode(TTree node, TParticle[] particles) => throw null;
		protected virtual void ComputeInnerNode(TTree node) => throw null;

		protected virtual TTree PruneTree() => (TTree)this._tree.Prune();
		public void Init() {
			this.IterationCount = -1;

			this.ParticleGroups = new AParticleGroup<TParticle>[Parameters.PARTICLES_GROUP_COUNT];
			for (int i = 0; i < this.ParticleGroups.Length; i++) {
				this.ParticleGroups[i] = this.NewParticleGroup();
				this.ParticleGroups[i].Init();
			}

			this._leaves.Clear();
			this._tree.Clear();
			this._tree = (TTree)this._tree
				.Add(this.ParticleGroups.SelectMany(g => g.InitialParticles))
				.Prune();
		}

		public ParticleData[] Update() {
			if (++this.IterationCount > 0) {//show starting data on first result
				this.PrepareTree();
				this.RefreshInteractions();
				this.MoveParticles();
				this._tree = this.PruneTree();
			}
			return this._tree.Select(p => new ParticleData(p)).ToArray();
		}

		private void PrepareTree() {
			TParticle[] particles;

			if (this._tree.IsLeaf) {
				particles = this._tree.Bin.ToArray();
				this._leaves.Enqueue(new(this._tree, particles));
				if (this.AccumulateTreeNodeData)
					this.ComputeLeafNode(this._tree, particles);
			} else {
				Stack<TTree> pendingNodes = new(), testNodes = new();
				pendingNodes.Push(this._tree);

				Stack<TTree[]> levelStack = new();
				if (this.AccumulateTreeNodeData)
					levelStack.Push(new TTree[] { this._tree });

				TTree[] levelNodes;
				TTree child, node;
				do {
					while (pendingNodes.TryPop(out node))
						for (int cIdx = 0; cIdx < node.Children.Length; cIdx++)
							if (node.Children[cIdx].ItemCount > 0) {
								child = (TTree)node.Children[cIdx];
								if (child.IsLeaf) {
									particles = child.Bin.ToArray();
									this._leaves.Enqueue(new(child, particles));
									if (this.AccumulateTreeNodeData)
										this.ComputeLeafNode(child, particles);
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
				
				while (levelStack.TryPop(out levelNodes))
					for (int i = 0; i < levelNodes.Length; i++)
						this.ComputeInnerNode(levelNodes[i]);
			}
		}

		private void RefreshInteractions() {
			if (Parameters.PARALLEL_SIMULATION)
				Parallel.ForEach(
					this._leaves,//creates copy of collection
					leaf => this.ComputeInteractions(
						leaf.Item1,
						leaf.Item2));
			else foreach (Tuple<TTree, TParticle[]> leaf in this._leaves)
				this.ComputeInteractions(
					leaf.Item1,
					leaf.Item2);
		}

		private void MoveParticles() {
			BaryCenter center = this.Center;

			Tuple<TTree, TParticle[]> oldLeaf;
			ATree<TParticle> leaf;
			TParticle particle;
			while (this._leaves.TryDequeue(out oldLeaf)) {
				for (int j = 0; j < oldLeaf.Item2.Length; j++) {
					particle = oldLeaf.Item2[j];
					if (particle.Enabled) {
						leaf = oldLeaf.Item1;
						while (!leaf.IsLeaf)
							leaf = leaf.Children[leaf.ChildIndex(particle)];

						if (!(particle.Mergers is null))
							while (particle.Mergers.TryDequeue(out TParticle other))
								if (other.Enabled) {
									other.Enabled = false;
									oldLeaf.Item1.GetContainingLeaf(other).RemoveFromLeaf(other, false);

									particle.Incorporate(other);
									if (!(other.Mergers is null))
										while (other.Mergers.TryDequeue(out TParticle tail))
											if (particle.Id != tail.Id)
												particle.Mergers.Enqueue(tail);
								}
							
						particle.ApplyTimeStep(Parameters.TIME_SCALE, center);
						if (particle.Enabled)
							leaf.MoveFromLeaf(particle, false);
						else leaf.RemoveFromLeaf(particle, false);
						
						if (!(particle.NewParticles is null))
							while (particle.NewParticles.TryDequeue(out TParticle other))
								oldLeaf.Item1.Add(other);
					}
				}
			}
		}
	}
}