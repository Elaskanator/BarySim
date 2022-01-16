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
			this.ParticleTree = tree;
		}

		public int IterationCount { get; private set; }
		public AParticleGroup<TParticle>[] ParticleGroups { get; private set; }

		protected TTree ParticleTree;

		public int ParticleCount => this.ParticleTree is null ? 0 : this.ParticleTree.ItemCount;//this.ParticleTree is null ? 0 : this.ParticleTree.Count;
		public ICollection<TParticle> Particles => this.ParticleTree;
		IEnumerable<IParticle> ISimulator.Particles => this.Particles;
		
		public abstract BaryCenter Center { get; }
		protected abstract bool AccumulateTreeNodeData { get; }

		protected abstract AParticleGroup<TParticle> NewParticleGroup();
		protected abstract void ComputeInteractions(TTree leaf, TParticle[] particles);

		protected virtual void ComputeLeafNode(TTree node, TParticle[] particles) => throw null;
		protected virtual void ComputeInnerNode(TTree node) => throw null;

		protected virtual TTree PruneTree() => (TTree)this.ParticleTree.Prune();
		public void Init() {
			this.IterationCount = -1;

			this.ParticleGroups = new AParticleGroup<TParticle>[Parameters.PARTICLES_GROUP_COUNT];
			for (int i = 0; i < this.ParticleGroups.Length; i++) {
				this.ParticleGroups[i] = this.NewParticleGroup();
				this.ParticleGroups[i].Init();
			}

			this.ParticleTree.Clear();
			this.ParticleTree = (TTree)this.ParticleTree
				.Add(this.ParticleGroups.SelectMany(g => g.InitialParticles))
				.Prune();
		}

		public ParticleData[] RefreshSimulation() {
			this.IterationCount++;
			if (this.IterationCount > 0) {//show starting data on first result
				List<Tuple<TTree, TParticle[]>> leaves = this.PrepareTree();

				Parallel.ForEach(
					leaves,
					leaf => this.ComputeInteractions(
						leaf.Item1,
						leaf.Item2));

				this.MoveParticles(leaves);

				this.ParticleTree = this.PruneTree();
			}
			return this.ParticleTree.Select(p => new ParticleData(p)).ToArray();
		}

		private List<Tuple<TTree, TParticle[]>> PrepareTree() {
			List<Tuple<TTree, TParticle[]>> leaves = new((int)(
				((float)this.ParticleTree.ItemCount / this.ParticleTree.LeafCapacity)
				* (this.ParticleTree.LeafCapacity > 1 ? 1.5f : 1f)));
			TParticle[] particles;

			if (this.ParticleTree.IsLeaf) {
				particles = this.ParticleTree.Bin.ToArray();
				leaves.Add(new(this.ParticleTree, particles));
				if (this.AccumulateTreeNodeData)
					this.ComputeLeafNode(this.ParticleTree, particles);
			} else {
				Stack<TTree> pendingNodes = new(), testNodes = new();
				pendingNodes.Push(this.ParticleTree);

				Stack<TTree[]> levelStack = new();
				if (this.AccumulateTreeNodeData)
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
									leaves.Add(new(child, particles));
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

			return leaves;
		}

		private void MoveParticles(List<Tuple<TTree, TParticle[]>> leaves) {
			BaryCenter center = this.Center;
			ATree<TParticle> node, leaf;
			TParticle particle, other, tail;
			for (int i = 0; i < leaves.Count; i++) {
				for (int j = 0; j < leaves[i].Item2.Length; j++) {
					particle = leaves[i].Item2[j];
					if (particle.Enabled) {
						node = leaves[i].Item1;
						leaf = node;
						while (!leaf.IsLeaf)
							leaf = leaf.Children[leaf.ChildIndex(particle)];

						if (!(particle.Mergers is null))
							while (particle.Mergers.TryDequeue(out other))
								if (other.Enabled) {
									other.Enabled = false;
									node.GetContainingLeaf(other).RemoveFromLeaf(other, false);

									particle.Incorporate(other);
									if (!(other.Mergers is null))
										while (other.Mergers.TryDequeue(out tail))
											if (particle.Id != tail.Id)
												particle.Mergers.Enqueue(tail);
								}
							
						particle.ApplyTimeStep(Parameters.TIME_SCALE, center);
						if (particle.Enabled)
							leaf.MoveFromLeaf(particle, false);
						else leaf.RemoveFromLeaf(particle, false);
						
						if (!(particle.NewParticles is null))
							while (particle.NewParticles.TryDequeue(out other))
								node.Add(other);
					}
				}
			}
		}
	}
}