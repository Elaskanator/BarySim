using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Generic.Extensions;
using Generic.Trees;
using ParticleSimulator.Simulation.Particles;

namespace ParticleSimulator.Simulation.Baryon {
	public class BaryonSimulator : ISimulator {//modified Barnes-Hut Algorithm
		public int IterationCount { get; private set; }
		public AParticleGroup<MatterClump>[] InitialParticleGroups { get; private set; }
		public BarnesHutTree ParticleTree { get; private set; }
		ITree ISimulator.ParticleTree => this.ParticleTree;
		IEnumerable<IParticle> ISimulator.Particles => this.ParticleTree.AsEnumerable();
		public int ParticleCount => this.ParticleTree is null ? 0 : this.ParticleTree.ItemCount;//this.ParticleTree is null ? 0 : this.ParticleTree.Count;

		public void Init() {
			this.IterationCount = -1;
			this.InitialParticleGroups = Enumerable
				.Range(0, Parameters.PARTICLES_GROUP_COUNT)
				.Select(i => new SpinningDisk<MatterClump>((p, v) => new(p, v), Parameters.GALAXY_RADIUS))
				.ToArray();
			for (int i = 0; i < this.InitialParticleGroups.Length; i++)
				this.InitialParticleGroups[i].Init();

			ATree<MatterClump> node = new BarnesHutTree(Parameters.DIM);
			node.Add(this.InitialParticleGroups.SelectMany(g => g.InitialParticles));
			this.ParticleTree = (BarnesHutTree)node.Root;
		}

		public ParticleData[] RefreshSimulation() {
			this.IterationCount++;
			if (this.IterationCount > 0)//show starting data on first result
				if (this.ParticleTree.ItemCount == 0) {
					Program.CancelAction(null, null);
				} else this.Refresh(Parameters.TIME_SCALE);

			return this.ParticleTree.Select(p => new ParticleData(p)).ToArray();
		}

		private void Refresh(float timeStep) {
			int numLeaves = 0;
			Tuple<ATree<MatterClump>, MatterClump[]>[] leaves;

			if (this.ParticleTree.IsLeaf) {
				numLeaves = 1;
				leaves = new Tuple<ATree<MatterClump>, MatterClump[]>[] {
					new(this.ParticleTree, this.ParticleTree.Bin.ToArray())
				};
			} else {
				leaves = new Tuple<ATree<MatterClump>, MatterClump[]>[this.ParticleTree.ItemCount];

				Stack<BarnesHutTree> pendingNodes = new(), testNodes = new();
				Stack<BarnesHutTree[]> levelStack = new();
				pendingNodes.Push(this.ParticleTree);
				levelStack.Push(new BarnesHutTree[] { this.ParticleTree });

				BarnesHutTree[] levelNodes;
				BarnesHutTree child, node;
				MatterClump[] particles;

				do {
					while (pendingNodes.TryPop(out node))
						for (int cIdx = 0; cIdx < node.Children.Length; cIdx++)
							if (node.Children[cIdx].ItemCount > 0) {
								child = (BarnesHutTree)node.Children[cIdx];
								if (child.IsLeaf) {
									particles = child.Bin.ToArray();
									child.InitBaryCenter(particles);
									leaves[numLeaves++] = new(child, particles);
								} else testNodes.Push(child);
							}

					if (testNodes.Count > 0) {
						levelNodes = new BarnesHutTree[testNodes.Count];
						testNodes.CopyTo(levelNodes, 0);//casting magic?
						levelStack.Push(levelNodes);

						(pendingNodes, testNodes) = (testNodes, pendingNodes);
					}
				} while (pendingNodes.Count > 0);

				while (levelStack.TryPop(out levelNodes))
					for (int i = 0; i < levelNodes.Length; i++)
						levelNodes[i].UpdateBaryCenter();
			}

			Parallel.ForEach(
				leaves.Take(numLeaves),
				leaf => this.ProcessLeaf(
					(BarnesHutTree)leaf.Item1,
					leaf.Item2));
			
			ATree<MatterClump> node2;
			for (int i = 0; i < leaves.Length; i++)
				for (int j = 0; j < leaves[i].Item2.Length; j++) {
					node2 = leaves[i].Item1.GetContainingLeaf(leaves[i].Item2[j]);
					if (leaves[i].Item2[j].Enabled) {
						leaves[i].Item2[j].ApplyTimeStep(timeStep, this.ParticleTree);
						node2.MoveFromLeaf(leaves[i].Item2[j]);
					} else node2.Remove(leaves[i].Item2[j]);
				}


			if (Parameters.MERGE_ENABLE)
				this.HandleMergers();
			//trim top of tree
			this.ParticleTree = (BarnesHutTree)this.ParticleTree.Prune();
		}

		private void ProcessLeaf(BarnesHutTree leaf, MatterClump[] particles) {
			List<MatterClump> nearField = new();
			Queue<BarnesHutTree> farField = new();
			BarnesHutTree other;
			/*
			{//top down approach
				Queue<BarnesHutTree> remaining = new();
				remaining.Enqueue(this.ParticleTree);

				BarnesHutTree node;
				while (remaining.TryDequeue(out node))
					if (node.IsLeaf) {
						if (!ReferenceEquals(evalNode, node))
							nearField.AddRange(node.Bin);
					} else for (int c = 0; c < node.Children.Length; c++)
						if (node.Children[c].ItemCount > 0) {
							other = (BarnesHutTree)node.Children[c];
							if (evalNode.CanApproximate(other))//how are we guaranteed to not approximate a parent node? I don't like this
								farField.Enqueue(other);
							else remaining.Enqueue(other);
						}
			}
			*/
			
			{//bottom up approach
				Queue<BarnesHutTree> remaining = new();
				ATree<MatterClump> node = leaf, lastNode;
				BarnesHutTree child;
				while (!node.IsRoot) {
					lastNode = node;
					node = node.Parent;
					for (int i = 0; i < node.Children.Length; i++)
						if (node.Children[i].ItemCount > 0)
							if (!ReferenceEquals(lastNode, node.Children[i])) {
								child = (BarnesHutTree)node.Children[i];
								if (leaf.CanApproximate(child))
									farField.Enqueue(child);
								else remaining.Enqueue(child);
							}

					while (remaining.TryDequeue(out other))
						if (other.IsLeaf)
							if (leaf.CanApproximate(other))
								farField.Enqueue(other);
							else nearField.AddRange(other.Bin);
						else for (int i = 0; i < other.Children.Length; i++)
							if (other.Children[i].ItemCount > 0) {
								child = (BarnesHutTree)other.Children[i];
								if (leaf.CanApproximate(child))
									farField.Enqueue(child);
								else remaining.Enqueue(child);
							}
				}
			}
			
			Vector<float> farFieldContribution = Vector<float>.Zero;

			float distSq;
			Vector<float> toOther;
			while (farField.TryDequeue(out other)) {
				toOther = other.MassBaryCenter.Position - leaf.MassBaryCenter.Position;
				distSq = Vector.Dot(toOther, toOther);
				if (distSq > Parameters.WORLD_EPSILON)
					farFieldContribution += toOther * (other.MassBaryCenter.Weight / distSq);
			}
			farFieldContribution *= Parameters.GRAVITATIONAL_CONSTANT;

			Tuple<Vector<float>, Vector<float>> influence;
			for (int i = 0; i < particles.Length; i++) {
				particles[i].Acceleration = farFieldContribution;
				for (int j = i + 1; j < particles.Length; j++) {
					influence = particles[i].ComputeInfluence(particles[j]);
					particles[i].Acceleration += particles[j].Mass*influence.Item1 + influence.Item2*(1f/particles[i].Mass);
					particles[j].Acceleration -= particles[i].Mass*influence.Item1 + influence.Item2*(1f/particles[j].Mass);
				}
				for (int n = 0; n < nearField.Count; n++) {
					influence = particles[i].ComputeInfluence(nearField[n]);
					particles[i].Acceleration += nearField[n].Mass*influence.Item1 + influence.Item2*(1f/particles[i].Mass);
				}
			}
		}

		private void HandleMergers() {
			MatterClump other, tail;

			Queue<MatterClump> pending = new();
			HashSet<MatterClump> evaluated = new();
			MatterClump[] originalParticles = this.ParticleTree.AsArray();
			ATree<MatterClump> leaf;
			for (int i = 0; i < originalParticles.Length; i++) {
				if (originalParticles[i].Enabled && evaluated.Add(originalParticles[i])) {
					pending.Clear();
					while (originalParticles[i].Mergers.TryDequeue(out other))
						if (other.Enabled)
							pending.Enqueue(other);

					while (pending.TryDequeue(out other))
						if (evaluated.Add(other)) {
							leaf = this.ParticleTree.GetContainingLeaf(originalParticles[i]);
							originalParticles[i].Incorporate(other);
							leaf.MoveFromLeaf(originalParticles[i]);
							this.ParticleTree.Remove(other);
							while (other.Mergers.TryDequeue(out tail))
								if (tail.Enabled)
									pending.Enqueue(tail);
						}
				}

				if (originalParticles[i].NewParticles.Count > 0)
					while (originalParticles[i].NewParticles.TryDequeue(out other))
						this.ParticleTree.Add(other);
			}
		}
	}
}