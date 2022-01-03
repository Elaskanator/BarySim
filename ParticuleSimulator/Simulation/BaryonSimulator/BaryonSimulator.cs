using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Generic.Extensions;
using Generic.Models;
using Generic.Models.Trees;
using Generic.Vectors;

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
			this.ProcessTree();

			return this.ParticleTree.Select(p => new ParticleData(p)).ToArray();
		}

		private void ProcessTree() {
			/// 1 - Compute barycenters
			///		a - 
			/// 2 - Update particles (leaf nodes)
			///		a - 

			Queue<ATree<Particle>> pendingNodes = new(), testNodes = new();
			Stack<BarnesHutTree[]> levelStack = new();

			pendingNodes.Enqueue(this.ParticleTree);
			levelStack.Push(new BarnesHutTree[] { this.ParticleTree });

			bool any = true;
			ATree<Particle> node;
			BarnesHutTree[] levelNodes;

			while (any) {
				any = false;
				while (pendingNodes.TryDequeue(out node)) {
					if (!node.IsLeaf) {
						for (int cIdx = 0; cIdx < node.Children.Length; cIdx++) {
							if (node.Children[cIdx].Count > 0) {
								any = true;
								testNodes.Enqueue(node.Children[cIdx]);
							}
						}
					}
				}
				if (any) {
					levelNodes = new BarnesHutTree[testNodes.Count];
					testNodes.CopyTo(levelNodes, 0);//casting magic?
					levelStack.Push(levelNodes);

					(pendingNodes, testNodes) = (testNodes, pendingNodes);
				}
			}

			Queue<BarnesHutTree> leaves = new(levelStack.Pop());

			while (levelStack.TryPop(out levelNodes))
				for (int i = 0; i < levelNodes.Length; i++)
					if (levelNodes[i].IsLeaf)
						leaves.Enqueue(levelNodes[i]);
					else levelNodes[i].UpdateBarycenter();

			foreach (BarnesHutTree leaf in leaves)
				this.ProcessLeaf(leaf);
		}

		private void ProcessLeaf(BarnesHutTree leaf) {
			// TODODODO

			foreach (Particle p in leaf.Bin.AsEnumerable()) {
				p.ApplyTimeStep(Parameters.TIME_SCALE);
			}
		}
	}
}