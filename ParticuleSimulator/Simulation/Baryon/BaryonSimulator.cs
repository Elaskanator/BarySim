using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Generic.Extensions;
using Generic.Models.Trees;
using ParticleSimulator.Simulation.Particles;

namespace ParticleSimulator.Simulation.Baryon {
	public class BaryonSimulator : ISimulator {//modified Barnes-Hut Algorithm
		public int IterationCount { get; private set; }
		public Galaxy[] InitialParticleGroups { get; private set; }
		public BarnesHutTree ParticleTree { get; private set; }
		ITree ISimulator.ParticleTree => this.ParticleTree;
		IEnumerable<IParticle> ISimulator.Particles => this.ParticleTree.AsEnumerable();
		public int ParticleCount => this.ParticleTree is null ? 0 : this.ParticleTree.ItemCount;//this.ParticleTree is null ? 0 : this.ParticleTree.Count;

		public void Init() {
			this.IterationCount = -1;
			this.InitialParticleGroups = Enumerable
				.Range(0, Parameters.PARTICLES_GROUP_COUNT)
				.Select(i => new Galaxy(Parameters.GALAXY_RADIUS, Parameters.GALAXY_SOFTENING))
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
			Tuple<ATree<MatterClump>, MatterClump[]>[] leaves = new Tuple<ATree<MatterClump>, MatterClump[]>[this.ParticleTree.ItemCount];
			int numLeaves = 0;

			if (this.ParticleTree.IsLeaf) {
				leaves[numLeaves++] = new(this.ParticleTree, this.ParticleTree.Bin.ToArray());
			} else {
				Stack<BarnesHutTree> pendingNodes = new(), testNodes = new();
				Stack<BarnesHutTree[]> levelStack = new();
				pendingNodes.Push(this.ParticleTree);
				levelStack.Push(new BarnesHutTree[] { this.ParticleTree });

				BarnesHutTree[] levelNodes;
				BarnesHutTree child, node;
				MatterClump[] particles;
				bool any = true;
				while (any) {
					any = false;
					while (pendingNodes.TryPop(out node))
						for (int cIdx = 0; cIdx < node.Children.Length; cIdx++)
							if (node.Children[cIdx].ItemCount > 0) {
								child = (BarnesHutTree)node.Children[cIdx];
								if (node.Children[cIdx].IsLeaf) {
									particles = child.Bin.ToArray();
									child.InitBaryCenter(particles);
									leaves[numLeaves++] = new(child, particles);
								} else testNodes.Push(child);
								any = true;
							}

					if (any) {
						levelNodes = new BarnesHutTree[testNodes.Count];
						testNodes.CopyTo(levelNodes, 0);//casting magic?
						levelStack.Push(levelNodes);

						(pendingNodes, testNodes) = (testNodes, pendingNodes);
					}
				}

				while (levelStack.TryPop(out levelNodes))
					for (int i = 0; i < levelNodes.Length; i++)
						levelNodes[i].UpdateBaryCenter();
			}

			Parallel.ForEach(
				Enumerable.Range(0, numLeaves).Select(i => leaves[i]),
				leaf => this.ProcessNode(
					(BarnesHutTree)leaf.Item1,
					leaf.Item2));
			
			ATree<MatterClump> node2;
			for (int i = 0; i < numLeaves; i++)
				for (int j = 0; j < leaves[i].Item2.Length; j++)
					if (leaves[i].Item2[j].Enabled) {
						node2 = leaves[i].Item1.GetContainingLeaf(leaves[i].Item2[j]);
						leaves[i].Item2[j].ApplyTimeStep(timeStep, this.ParticleTree);
						node2.MoveFromLeaf(leaves[i].Item2[j]);
					}

			if (Parameters.MERGE_ENABLE)
				this.HandleMergers();
			//trim top of tree
			this.ParticleTree = (BarnesHutTree)this.ParticleTree.Prune();
		}

		private void ProcessNode(BarnesHutTree evalNode, MatterClump[] directParticles) {
			List<MatterClump> nearField = new();
			Queue<BarnesHutTree> farField = new(), remaining = new();

			ATree<MatterClump> node = evalNode, lastNode;
			BarnesHutTree other, child;
			while (!node.IsRoot) {
				lastNode = node;
				node = node.Parent;
				for (int i = 0; i < node.Children.Length; i++)
					if (node.Children[i].ItemCount > 0)
						if (!ReferenceEquals(lastNode, node.Children[i])) {
							child = (BarnesHutTree)node.Children[i];
							if (evalNode.CanApproximate(child))
								farField.Enqueue(child);
							else remaining.Enqueue(child);
						}

				while (remaining.TryDequeue(out other))
					if (other.IsLeaf)
						if (evalNode.CanApproximate(other))
							farField.Enqueue(other);
						else nearField.AddRange(other.Bin);
					else for (int i = 0; i < other.Children.Length; i++)
						if (other.Children[i].ItemCount > 0) {
							child = (BarnesHutTree)other.Children[i];
							if (evalNode.CanApproximate(child))
								farField.Enqueue(child);
							else remaining.Enqueue(child);
						}
			}

			Vector<float> farFieldContribution = Vector<float>.Zero;

			float distSq;
			Vector<float> toOther;
			while (farField.TryDequeue(out other)) {
				toOther = other.MassBaryCenter.Position - evalNode.MassBaryCenter.Position;
				distSq = Vector.Dot(toOther, toOther);
				if (distSq > Parameters.WORLD_EPSILON)
					farFieldContribution += toOther * (other.MassBaryCenter.Weight / distSq);
			}
			farFieldContribution *= Parameters.GRAVITATIONAL_CONSTANT;

			Tuple<Vector<float>, Vector<float>> influence;
			for (int i = 0; i < directParticles.Length; i++) {
				directParticles[i].Acceleration = farFieldContribution;
				for (int j = i + 1; j < directParticles.Length; j++) {
					influence = directParticles[i].ComputeInfluence(directParticles[j]);
					directParticles[i].Acceleration += directParticles[j].Mass*influence.Item1 + influence.Item2*(1f/directParticles[i].Mass);
					directParticles[j].Acceleration -= directParticles[i].Mass*influence.Item1 + influence.Item2*(1f/directParticles[j].Mass);
				}
				for (int n = 0; n < nearField.Count; n++) {
					influence = directParticles[i].ComputeInfluence(nearField[n]);
					directParticles[i].Acceleration += nearField[n].Mass*influence.Item1 + influence.Item2*(1f/directParticles[i].Mass);
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
				if (originalParticles[i].IsInRange) {
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
				} else this.ParticleTree.Remove(originalParticles[i]);

				if (originalParticles[i].NewParticles.Count > 0) {
					while (originalParticles[i].NewParticles.TryDequeue(out other))
						this.ParticleTree.Add(other);
					this.ParticleTree = (BarnesHutTree)this.ParticleTree.Root;
				}
			}
		}
	}
}