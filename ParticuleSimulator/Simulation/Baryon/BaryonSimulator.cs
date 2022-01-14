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
			List<Tuple<ATree<MatterClump>, MatterClump[]>> leaves = this.BuildLeavesAndBaryCenters();

			Parallel.ForEach(
				leaves,
				leaf => this.ProcessLeaf(
					(BarnesHutTree)leaf.Item1,
					leaf.Item2));
			
			Queue<MatterClump> pending;
			ATree<MatterClump> node, leaf, otherLeaf;
			MatterClump particle, other, tail;
			for (int i = 0; i < leaves.Count; i++) {
				for (int j = 0; j < leaves[i].Item2.Length; j++) {
					node = leaves[i].Item1;
					particle = leaves[i].Item2[j];
					leaf = node.GetContainingLeaf(particle);

					if (particle.Enabled) {
						particle.ApplyTimeStep(timeStep, this.ParticleTree.MassBaryCenter);

						if (!(particle.Mergers is null)) {
							pending = particle.Mergers;
							particle.Mergers = null;

							while (pending.TryDequeue(out other)) {
								if (other.Enabled) {
									otherLeaf = node.GetContainingLeaf(other);
									particle.Incorporate(other);
									other.Enabled = false;
									node.Remove(other, false);

									if (!(other.Mergers is null)) {
										while (other.Mergers.TryDequeue(out tail))
											pending.Enqueue(tail);
										other.Mergers = null;
									}
								}
							}
						}
						
						if (!(particle.NewParticles is null)) {
							while (particle.NewParticles.TryDequeue(out other))
								node.Add(other);
							particle.NewParticles = null;
						}
					}

					if (particle.Enabled)
						leaf.MoveFromLeaf(particle, false);
					else leaf.RemoveFromLeaf(particle, false);
				}
			}

			//trim top of tree
			this.ParticleTree = (BarnesHutTree)this.ParticleTree.Prune();
		}

		private List<Tuple<ATree<MatterClump>, MatterClump[]>> BuildLeavesAndBaryCenters() {
			List<Tuple<ATree<MatterClump>, MatterClump[]>> leaves = new((this.ParticleTree.ItemCount / this.ParticleTree.LeafCapacity) << (this.ParticleTree.LeafCapacity > 1 ? 1 : 0));
			MatterClump[] particles;

			if (this.ParticleTree.IsLeaf) {
				particles = this.ParticleTree.Bin.ToArray();
				this.ParticleTree.InitBaryCenter(particles);
				leaves.Add(new(this.ParticleTree, particles));
			} else {
				Stack<BarnesHutTree> pendingNodes = new(), testNodes = new();
				Stack<BarnesHutTree[]> levelStack = new();
				pendingNodes.Push(this.ParticleTree);
				levelStack.Push(new BarnesHutTree[] { this.ParticleTree });

				BarnesHutTree[] levelNodes;
				BarnesHutTree child, node;
				do {
					while (pendingNodes.TryPop(out node))
						for (int cIdx = 0; cIdx < node.Children.Length; cIdx++)
							if (node.Children[cIdx].ItemCount > 0) {
								child = (BarnesHutTree)node.Children[cIdx];
								if (child.IsLeaf) {
									particles = child.Bin.ToArray();
									child.InitBaryCenter(particles);
									leaves.Add(new(child, particles));
								} else testNodes.Push(child);
							} else node.Children[cIdx].Children = null;

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

			return leaves;
		}

		private void ProcessLeaf(BarnesHutTree leaf, MatterClump[] particles) {
			Vector<float> farFieldContribution = Vector<float>.Zero;
			List<MatterClump> nearField = new();
			Queue<BarnesHutTree> farField = new();
			this.DetermineNeighbors(leaf, nearField, farField);

			float distSq;
			Vector<float> toOther;
			BarnesHutTree otherNode;
			while (farField.TryDequeue(out otherNode)) {
				toOther = otherNode.MassBaryCenter.Position - leaf.MassBaryCenter.Position;
				distSq = Vector.Dot(toOther, toOther);
				if (distSq > Parameters.WORLD_EPSILON)
					farFieldContribution += toOther * (otherNode.MassBaryCenter.Weight / distSq);
			}
			farFieldContribution *= Parameters.GRAVITATIONAL_CONSTANT;

			Tuple<Vector<float>, Vector<float>> influence;
			for (int i = 0; i < particles.Length; i++) {
				particles[i].Acceleration = Vector<float>.Zero;
				for (int j = 0; j < i; j++) {
					influence = particles[i].ComputeInfluence(particles[j]);
					particles[i].Acceleration += particles[j].Mass*influence.Item1 + influence.Item2*(1f/particles[i].Mass);
					particles[j].Acceleration -= particles[i].Mass*influence.Item1 + influence.Item2*(1f/particles[j].Mass);
				}
				for (int n = 0; n < nearField.Count; n++) {
					influence = particles[i].ComputeInfluence(nearField[n]);
					particles[i].Acceleration += nearField[n].Mass*influence.Item1 + influence.Item2*(1f/particles[i].Mass);
				}
				particles[i].Acceleration += farFieldContribution;//add after to reduce floating point errors
			}
		}

		private void DetermineNeighbors(BarnesHutTree leaf, List<MatterClump> nearField, Queue<BarnesHutTree> farField) {
			BarnesHutTree other;

			/*
			//top down approach
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
			*/
			
			//bottom up approach
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
	}
}