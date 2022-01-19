using System;
using System.Collections.Generic;
using System.Numerics;
using ParticleSimulator.Simulation.Particles;

namespace ParticleSimulator.Simulation.Baryon {
	public class BaryonSimulator : ABinaryTreeSimulator<MatterClump, BarnesHutTree> {
		public BaryonSimulator(int dim) : base(new(dim)) { }

		public override BaryCenter Center {
			get { lock (this._lock)
				return this._tree.MassBaryCenter; } }

		protected override bool AccumulateTreeNodeData => true;

		private readonly object _lock = new();

		protected override AParticleGroup<MatterClump> NewParticleGroup() =>
			new SpinningDisk<MatterClump>((p, v) => new(p, v), Parameters.GALAXY_RADIUS);

		protected override void AccumulateLeafNode(BarnesHutTree node, MatterClump[] particles) =>
			node.InitBaryCenter(particles);
		protected override void AccumulateInnerNode(BarnesHutTree node) =>
			node.UpdateBaryCenter();

		protected override BarnesHutTree PruneTree() {
			BaryCenter center = this._tree.MassBaryCenter;
			BarnesHutTree result;
			lock (this._lock) {
				result = (BarnesHutTree)this._tree.Prune();
				result.MassBaryCenter = center;
			}
			return result;
		}

		protected override void ComputeInteractions(BarnesHutTree leaf, MatterClump[] particles) {
			List<MatterClump> nearField = new();
			Vector<float> farFieldContribution = this.DetermineNeighbors(leaf, nearField);

			Tuple<Vector<float>, Vector<float>> influence;
			for (int i = 0; i < particles.Length; i++) {
				particles[i].Acceleration = Vector<float>.Zero;
				for (int j = 0; j < i; j++) {
					influence = particles[i].ComputeInfluence(particles[j]);
					particles[i].Acceleration += particles[j].Mass*influence.Item1 + influence.Item2;
					particles[j].Acceleration -= particles[i].Mass*influence.Item1 + influence.Item2;
				}
				for (int n = 0; n < nearField.Count; n++) {
					influence = particles[i].ComputeInfluence(nearField[n]);
					particles[i].Acceleration += nearField[n].Mass*influence.Item1 + influence.Item2;
				}
				particles[i].Acceleration += farFieldContribution;//add after to reduce floating point errors
			}
		}

		private Vector<float> DetermineNeighbors(BarnesHutTree leaf, List<MatterClump> nearField) {
			Vector<float> farFieldContribution = Vector<float>.Zero;
			Stack<BarnesHutTree> remaining = new();
			BarnesHutTree node = leaf, lastNode, child, tail;
			Vector<float> toOther;
			float distanceSquared, distance;

			while (!node.IsRoot) {
				lastNode = node;
				node = (BarnesHutTree)node.Parent;
				for (int i = 0; i < node.Children.Length; i++) {
					child = (BarnesHutTree)node.Children[i];
					if (!ReferenceEquals(lastNode, child) && child.ItemCount > 0) {
						do {
							if (child.IsLeaf) {
								nearField.AddRange(child.Bin);
							} else {
								toOther = child.MassBaryCenter.Position - leaf.MassBaryCenter.Position;
								distanceSquared = Vector.Dot(toOther, toOther);
								if (distanceSquared * Parameters.INACCURCY_SQUARED > child.SizeSquared) {
									distance = MathF.Sqrt(distanceSquared);
									farFieldContribution += toOther * (child.MassBaryCenter.Weight / distanceSquared / distance);
								} else {
									for (int j = 0; j < child.Children.Length; j++) {
										tail = (BarnesHutTree)child.Children[j];
										if (tail.ItemCount > 0)
											remaining.Push(tail);
									}
								}
							}
						} while (remaining.TryPop(out child));
					}
				}
			}

			return farFieldContribution * Parameters.GRAVITATIONAL_CONSTANT;
		}
	}
}