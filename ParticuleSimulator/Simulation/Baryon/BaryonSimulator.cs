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

		protected override BarnesHutTree PruneTreeTop() {
			BaryCenter center = this._tree.MassBaryCenter;
			BarnesHutTree result;
			lock (this._lock) {
				result = (BarnesHutTree)this._tree.PruneTop();
				result.MassBaryCenter = center;
			}
			return result;
		}

		protected override void ComputeInteractions(BarnesHutTree leaf, MatterClump[] particles) {
			List<MatterClump> nearField = new();
			Vector<float> farFieldContribution = this.DetermineNeighbors(leaf, nearField);

			Tuple<Vector<float>, Vector<float>> influence;
			for (int i = 0; i < particles.Length; i++) {
				for (int n = 0; n < nearField.Count; n++) {
					influence = particles[i].ComputeInfluence(nearField[n]);
					particles[i].Velocity += (1f/particles[i].Mass)*influence.Item1;
					particles[i].Acceleration += nearField[n].Mass*influence.Item2;
				}
				for (int j = 0; j < i; j++) {
					influence = particles[i].ComputeInfluence(particles[j]);
					particles[i].Velocity += (1f/particles[i].Mass)*influence.Item1;
					particles[i].Acceleration += particles[j].Mass*influence.Item2;
					particles[j].Velocity -= (1f/particles[j].Mass)*influence.Item1;
					particles[j].Acceleration -= particles[i].Mass*influence.Item2;
				}
				particles[i].Acceleration += farFieldContribution;//add last to reduce floating point errors
			}
		}

		private Vector<float> DetermineNeighbors(BarnesHutTree leaf, List<MatterClump> nearField) {
			//minimize floating point error by computing nodes likely to be more distant first
			Vector<float> farFieldContribution = Vector<float>.Zero, subTotal1, subTotal2, toOther;
			Stack<BarnesHutTree> pathDown = new(), remaining = new();
			BarnesHutTree parent, child, other, tail;
			float distanceSquared, distance;
			//compute path thru the tree
			parent = leaf;
			while (!parent.IsRoot) {
				pathDown.Push(parent);
				parent = (BarnesHutTree)parent.Parent;
			}
			//compute from top nodes down
			while (pathDown.TryPop(out child)) {
				subTotal1 = Vector<float>.Zero;
				for (int i = 0; i < parent.Children.Length; i++) {
					subTotal2 = Vector<float>.Zero;
					other = (BarnesHutTree)parent.Children[i];
					if (!ReferenceEquals(child, other) && other.ItemCount > 0) {
						do {//recursively test depth-first for nodes that can be approximated as point masses
							if (other.IsLeaf) {
								nearField.AddRange(other.Bin);
							} else {
								toOther = other.MassBaryCenter.Position - leaf.MassBaryCenter.Position;
								distanceSquared = Vector.Dot(toOther, toOther);
								if (distanceSquared <= Parameters.WORLD_EPSILON) {//TODO check for adjacency instead
									nearField.AddRange(other);
								} else if (distanceSquared * Parameters.INACCURCY_SQUARED > other.SizeSquared) {//Barnes-Hut condition
									distance = MathF.Sqrt(distanceSquared);
									subTotal2 += toOther * (other.MassBaryCenter.Weight / distanceSquared / distance);//gravity
								} else {//recurse down
									for (int j = 0; j < other.Children.Length; j++) {
										tail = (BarnesHutTree)other.Children[j];
										if (tail.ItemCount > 0)
											remaining.Push(tail);
									}
								}
							}
						} while (remaining.TryPop(out other));
					}
					subTotal1 += subTotal2;
				}
				//reduce floating point error with subtotalling before adding to running total
				farFieldContribution += subTotal1;
				parent = child;
			}
			//finally apply G
			return farFieldContribution * Parameters.GRAVITATIONAL_CONSTANT;
		}
		/*
		//old version more susceptible to floating point errors from evaluating more local neighbors first
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
								if (distanceSquared <= Parameters.WORLD_EPSILON) {
									nearField.AddRange(child);
								} else if (distanceSquared * Parameters.INACCURCY_SQUARED > child.SizeSquared) {
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
		*/
	}
}