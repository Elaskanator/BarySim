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

		public void Init() {
			this.InitialParticleGroups = Enumerable
				.Range(0, Parameters.PARTICLES_GROUP_COUNT)
				.Select(i => new Galaxy())
				.ToArray();

			ATree<Particle> node = new BarnesHutTree(Parameters.DIM);
			node.Add(this.InitialParticleGroups.SelectMany(g => g.InitialParticles));
			this.ParticleTree = (BarnesHutTree)node.Root;
		}

		public Queue<ParticleData> RefreshSimulation() {
			if (this.ParticleTree.Count > 0)
				return this.ProcessTree();
			else return new(0);
		}

		private Queue<ParticleData> ProcessTree() {
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
					if (node.Count > 0 && !node.IsLeaf)
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

			Tuple<BarnesHutTree, Particle[]>[] leaves = new Tuple<BarnesHutTree, Particle[]>[this.ParticleTree.Count];
			int lIdx = 0;
			while (levelStack.TryPop(out levelNodes))
				for (int i = 0; i < levelNodes.Length; i++) {
					levelNodes[i].UpdateBarycenter();
					if (levelNodes[i].IsLeaf)
						leaves[lIdx++] = new(levelNodes[i], levelNodes[i].Bin.ToArray());
				}

			for (int i = 0; i < lIdx; i++)
				this.ProcessLeaf(leaves[i]);

			Queue<ParticleData> result = new(this.ParticleTree.Count);
			for (int i = 0; i < lIdx; i++)
				for (int p = 0; p < leaves[i].Item2.Length; p++) {
					node = leaves[i].Item1;
					while (!node.IsLeaf)
						node = node.Children[node.ChildIndex(leaves[i].Item2[p])];
					leaves[i].Item2[p].ApplyTimeStep(Parameters.TIME_SCALE);
					node.MoveFromLeaf(leaves[i].Item2[p]);
					if (leaves[i].Item2[p].Enabled)
						result.Enqueue(new(leaves[i].Item2[p]));
				}

			this.ParticleTree = (BarnesHutTree)this.ParticleTree.Root;

			return result;
		}

		private void ProcessLeaf(Tuple<BarnesHutTree, Particle[]> leafData) {
			Queue<BarnesHutTree> remaining = new(),
				farField = new();
			List<Particle> nearField = new();

			ATree<Particle> node = leafData.Item1, lastNode;
			BarnesHutTree other, child;
			while (!node.IsRoot) {
				lastNode = node;
				node = node.Parent;
				for (int i = 0; i < node.Children.Length; i++) {
					if (node.Children[i].Count > 0)
						if (!ReferenceEquals(lastNode, node.Children[i])) {
							child = (BarnesHutTree)node.Children[i];
							if (leafData.Item1.CanApproximate(child))
								farField.Enqueue(child);
							else remaining.Enqueue(child);
						}
				}
				while (remaining.TryDequeue(out other)) {
					if (other.IsLeaf) {
						if (leafData.Item1.CanApproximate(other))
							farField.Enqueue(other);
						else nearField.AddRange(other.Bin);
					} else {
						for (int i = 0; i < other.Children.Length; i++)
							if (other.Children[i].Count > 0) {
								child = (BarnesHutTree)other.Children[i];
								if (leafData.Item1.CanApproximate(child))
									farField.Enqueue(child);
								else remaining.Enqueue(child);
							}
					}
				}
			}

			Vector<float> farFieldContribution = Vector<float>.Zero;

			float distSq;
			Vector<float> toOther;
			while (farField.TryDequeue(out other) && leafData.Item1.Barycenter.Item2 > 0f) {
				toOther = other.Barycenter.Item1 - leafData.Item1.Barycenter.Item1;
				distSq = Vector.Dot(toOther, toOther);
				if (distSq > Parameters.WORLD_EPSILON)
					farFieldContribution += toOther * (other.Barycenter.Item2 / distSq);
			}

			for (int i = 0; i < leafData.Item2.Length; i++) {
				//binParticles[i].Test1 = true;
				leafData.Item2[i].Acceleration = farFieldContribution;
				for (int j = i + 1; j < leafData.Item2.Length; j++) {
					toOther = leafData.Item2[j].Position - leafData.Item2[i].Position;
					distSq = Vector.Dot(toOther, toOther);
					if (distSq > Parameters.WORLD_EPSILON) {
						leafData.Item2[i].Acceleration += toOther * (leafData.Item2[j].Mass / distSq);
						leafData.Item2[j].Acceleration -= toOther * (leafData.Item2[i].Mass / distSq);
					}
				}
				for (int n = 0; n < nearField.Count; n++) {
					//nearField[n].Test2 = true;
					toOther = nearField[n].Position - leafData.Item2[i].Position;
					distSq = Vector.Dot(toOther, toOther);
					if (distSq > Parameters.WORLD_EPSILON)
						leafData.Item2[i].Acceleration += toOther * (nearField[n].Mass / distSq);
				}
				leafData.Item2[i].Acceleration *= Parameters.GRAVITATIONAL_CONSTANT;
			}
		}
	}
}