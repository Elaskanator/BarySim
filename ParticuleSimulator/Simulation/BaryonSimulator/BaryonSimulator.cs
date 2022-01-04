using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Generic.Extensions;
using Generic.Models.Trees;

namespace ParticleSimulator.Simulation.Baryon {
	public class BaryonSimulator : ISimulator {//modified Barnes-Hut Algorithm
		public BaryonSimulator() { }

		public Galaxy[] InitialParticleGroups { get; private set; }
		public BarnesHutTree ParticleTree { get; private set; }
		IEnumerable<Particle> ISimulator.Particles => this.ParticleTree.AsEnumerable();
		public int ParticleCount => this.ParticleTree is null ? 0 : this.ParticleTree.Count;//this.ParticleTree is null ? 0 : this.ParticleTree.Count;

		public virtual bool EnableCollisions => false;
		public virtual float WorldBounceWeight => 0f;

		public void Init() {
			this.InitialParticleGroups = Enumerable
				.Range(0, Parameters.PARTICLES_GROUP_COUNT)
				.Select(i => new Galaxy())
				.ToArray();

			this.ParticleTree = (BarnesHutTree)new BarnesHutTree(Parameters.DIM)
				.AddUpOrDown(this.InitialParticleGroups.SelectMany(g => g.InitialParticles));
		}

		public ParticleData[] RefreshSimulation() {
			if (this.ParticleTree.Count > 0) {
				this.ProcessTree();
				return this.ParticleTree.Select(p => new ParticleData(p)).ToArray();
			} else return Array.Empty<ParticleData>();
		}

		private void ProcessTree() {
			Queue<ATree<Particle>> pendingNodes = new(), testNodes = new();
			Stack<BarnesHutTree[]> levelStack = new();

			pendingNodes.Enqueue(this.ParticleTree);
			levelStack.Push(new BarnesHutTree[] { this.ParticleTree });

			BarnesHutTree[] levelNodes;
			ATree<Particle> node;
			bool any = true;
			while (any) {
				any = false;
				while (pendingNodes.TryDequeue(out node))
					if (!node.IsLeaf)
						for (int cIdx = 0; cIdx < node.Children.Length; cIdx++)
							if (node.Children[cIdx].Count > 0) {
								any = true;
								testNodes.Enqueue(node.Children[cIdx]);
							}
				if (any) {
					levelNodes = new BarnesHutTree[testNodes.Count];
					testNodes.CopyTo(levelNodes, 0);//casting magic?
					levelStack.Push(levelNodes);

					(pendingNodes, testNodes) = (testNodes, pendingNodes);
				}
			}

			BarnesHutTree[] leaves = new BarnesHutTree[this.ParticleTree.Count];
			int pIdx = 0;
			while (levelStack.TryPop(out levelNodes))
				for (int i = 0; i < levelNodes.Length; i++)
					if (levelNodes[i].Count > 0) {
						levelNodes[i].UpdateBarycenter();
						if (levelNodes[i].IsLeaf)
							leaves[pIdx++] = levelNodes[i];
					}
			for (int i = 0; i < pIdx; i++)
				this.ProcessLeaf(leaves[i]);

			foreach (Particle p in this.ParticleTree.AsEnumerable()) {
				//p.Test1 = false;
				//p.Test2 = false;
				p.ApplyTimeStep(Parameters.TIME_SCALE);
			}
		}

		private void ProcessLeaf(BarnesHutTree leaf) {
			Queue<BarnesHutTree> remaining = new(),
				farField = new();
			List<Particle> nearField = new();

			ATree<Particle> node = leaf;
			BarnesHutTree other, child;
			bool notFirst = false;
			while (!node.IsRoot) {
				node = node.Parent;
				for (int i = 0; i < node.Children.Length; i++) {
					if (node.Children[i].Count > 0)
						if (notFirst || !ReferenceEquals(node.Children[i], leaf)) {
							child = (BarnesHutTree)node.Children[i];
							if (leaf.CanApproximate(child))
								farField.Enqueue(child);
							else remaining.Enqueue(child);
						}
				}
				while (remaining.TryDequeue(out other)) {
					if (other.IsLeaf) {
						if (leaf.CanApproximate(other))
							farField.Enqueue(other);
						else nearField.AddRange(other.Bin);
					} else {
						for (int i = 0; i < other.Children.Length; i++)
							if (other.Children[i].Count > 0) {
								child = (BarnesHutTree)other.Children[i];
								if (leaf.CanApproximate(child))
									farField.Enqueue(child);
								else remaining.Enqueue(child);
							}
					}
				}
				notFirst = true;
			}

			Vector<float> farFieldContribution = Vector<float>.Zero;

			float distSq;
			Vector<float> toOther;
			while (farField.TryDequeue(out other) && leaf.Barycenter.Item2 > 0f) {
				toOther = other.Barycenter.Item1 - leaf.Barycenter.Item1;
				distSq = Vector.Dot(toOther, toOther);
				if (distSq > Parameters.WORLD_EPSILON)
					farFieldContribution += toOther * (other.Barycenter.Item2 / distSq);
			}

			Particle[] binParticles = leaf.Bin.ToArray();
			for (int i = 0; i < binParticles.Length; i++) {
				//binParticles[i].Test1 = true;
				binParticles[i].Acceleration = farFieldContribution;
				for (int j = i + 1; j < binParticles.Length; j++) {
					toOther = binParticles[j].Position - binParticles[i].Position;
					distSq = Vector.Dot(toOther, toOther);
					if (distSq > Parameters.WORLD_EPSILON) {
						binParticles[i].Acceleration += toOther * (binParticles[j].Mass / distSq);
						binParticles[j].Acceleration -= toOther * (binParticles[i].Mass / distSq);
					}
				}
				for (int n = 0; n < nearField.Count; n++) {
					//nearField[n].Test2 = true;
					toOther = nearField[n].Position - binParticles[i].Position;
					distSq = Vector.Dot(toOther, toOther);
					if (distSq > Parameters.WORLD_EPSILON)
						binParticles[i].Acceleration += toOther * (nearField[n].Mass / distSq);
				}
				binParticles[i].Acceleration *= Parameters.GRAVITATIONAL_CONSTANT;
			}
		}
	}
}