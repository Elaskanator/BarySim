using System;
using System.Collections.Generic;
using System.Numerics;
using Generic.Trees;
using ParticleSimulator.Simulation.Particles;

namespace ParticleSimulator.Simulation.Baryon {
	public class BaryonSimulator : ABinaryTreeSimulator<MatterClump, BarnesHutTree> {
		public BaryonSimulator() : base(new(Parameters.DIM)) { }

		public override Vector<float> Center {
			get { lock (this._lock)
				return this.ParticleTree.MassBaryCenter.Position; } }

		protected override bool AccumulateTreeNodeData => true;

		private readonly object _lock = new();

		protected override AParticleGroup<MatterClump> NewParticleGroup() =>
			new SpinningDisk<MatterClump>((p, v) => new(p, v), Parameters.GALAXY_RADIUS);

		protected override void ComputeLeafNode(BarnesHutTree child, MatterClump[] particles) =>
			child.InitBaryCenter(particles);
		protected override void ComputeInnerNode(BarnesHutTree node) =>
			node.UpdateBaryCenter();

		protected override BarnesHutTree PruneTree() {
			BaryCenter center = this.ParticleTree.MassBaryCenter;
			BarnesHutTree result;
			lock (this._lock) {
				result = (BarnesHutTree)this.ParticleTree.Prune();
				result.MassBaryCenter = center;
			}
			return result;
		}

		protected override void ProcessLeaf(BarnesHutTree leaf, MatterClump[] particles) {
			List<MatterClump> nearField = new();
			Vector<float> farFieldContribution = this.DetermineNeighbors(leaf, nearField);

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

		private Vector<float> DetermineNeighbors(BarnesHutTree leaf, List<MatterClump> nearField) {
			Vector<float> farFieldContribution = Vector<float>.Zero;

			Stack<BarnesHutTree> remaining = new();
			ATree<MatterClump> node = leaf, lastNode;
			BarnesHutTree child;
			float distanceSquared;
			Vector<float> toOther;
			while (!node.IsRoot) {
				lastNode = node;
				node = node.Parent;
				for (int i = 0; i < node.Children.Length; i++) {
					if (!ReferenceEquals(lastNode, node.Children[i]) && node.Children[i].ItemCount > 0) {
						child = (BarnesHutTree)node.Children[i];
						do {
							if (leaf.CanApproximate(child)) {
								toOther = child.MassBaryCenter.Position - leaf.MassBaryCenter.Position;
								distanceSquared = Vector.Dot(toOther, toOther);
								if (distanceSquared > Parameters.WORLD_EPSILON)
									farFieldContribution += toOther * (child.MassBaryCenter.Weight / distanceSquared);
							} else if (child.IsLeaf) {
								nearField.AddRange(child.Bin);
							} else for (int j = 0; j < child.Children.Length; j++)
								if (child.Children[j].ItemCount > 0)
									remaining.Push((BarnesHutTree)child.Children[j]);
						} while (remaining.TryPop(out child));
					}
				}
			}

			farFieldContribution *= Parameters.GRAVITATIONAL_CONSTANT;
			return farFieldContribution;
		}
	}
}