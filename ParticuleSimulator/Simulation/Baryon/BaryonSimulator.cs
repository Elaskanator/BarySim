using System;
using System.Collections.Generic;
using System.Numerics;
using Generic.Trees;
using ParticleSimulator.Simulation.Particles;

namespace ParticleSimulator.Simulation.Baryon {
	public class BaryonSimulator : ABinaryTreeSimulator<MatterClump, BarnesHutTree> {
		public BaryonSimulator(int dim) {
			this._tree = new(dim);
		}
		
		private readonly object _lock = new();//needed to avoid camera autocenter from spazzing upon pruning
		private BarnesHutTree _tree;
		public override BarnesHutTree Tree {
			get {
				lock (this._lock)
					return this._tree;
			} protected set {
				lock (this._lock)
					this._tree = value;
			}}
		public override BaryCenter Center => this.Tree.MassBaryCenter;
		protected override bool AccumulateTreeNodeData => true;

		protected override AParticleGroup<MatterClump> NewParticleGroup() =>
			new SpinningDisk<MatterClump>((p, v) => new(p, v), Parameters.GALAXY_RADIUS);

		protected override void AccumulateLeafNode(BarnesHutTree node, MatterClump[] particles) =>
			node.InitBaryCenter(particles);
		protected override void AccumulateInnerNode(BarnesHutTree node) =>
			node.UpdateBaryCenter();

		protected override void PruneTreeTop() {
			BaryCenter center = this._tree.MassBaryCenter;
			lock (this._lock) {//prevents camera autofollowing from tweaking out if the tree shrinks
				this._tree = (BarnesHutTree)this._tree.PruneTop();
				this._tree.MassBaryCenter = center;
			}
		}

		protected override void ComputeInteractions(BarnesHutTree leaf, MatterClump[] particles) {
			List<MatterClump> nearField = new();
			Vector<float> farFieldAcceleration = DetermineNeighbors(leaf, nearField);

			Vector<float> influence;
			for (int i = 0; i < particles.Length; i++) {
				//add weaker forces first to reduce floating point errors
				for (int n = 0; n < nearField.Count; n++) {
					influence = particles[i].ComputeInteractionInfluence(nearField[n]);
					particles[i].Acceleration += influence * nearField[n].Mass;
				}
				for (int j = 0; j < i; j++) {
					influence = particles[i].ComputeInteractionInfluence(particles[j]);
					particles[i].Acceleration += influence * particles[j].Mass;
					particles[j].Acceleration -= influence * particles[i].Mass;
				}
				//add last to reduce floating point errors
				particles[i].Acceleration += farFieldAcceleration;//cheeky optimization to skip impulse/mass conversion
			}
		}

		public static Vector<float> DetermineNeighbors(BarnesHutTree leaf, List<MatterClump> nearField) {
			//apply the Barnes Hut proximity criterion to partition the tree into nearby leaves and distance approximations
			Vector<float> farFieldAcceleration = Vector<float>.Zero;

			Stack<int> pathDown = new();
			ABinaryTree<MatterClump> parent = leaf,
				child = null;//STFU compiler
			int idx = 0;//shut up the compiler
			//get path thru the tree
			while (!parent.IsRoot) {
				//determine relative position
				for (int i = 0; i < parent.Parent.Children.Length; i++) {
					if (ReferenceEquals(parent, parent.Parent.Children[i])) {//guaranteed exactly once
						idx = i;
						break;
					}
				}
				pathDown.Push(idx);
				parent = parent.Parent;
			}
			//evaluate from top nodes down to compute furthest (and weakest) interactions first, to reduce floating point errors when aggregating
			Stack<BarnesHutTree> remaining = new();
			BarnesHutTree neighbor, tail;
			Vector<float> subTotal1, subTotal2, toOther;
			float distanceSquared, distance;
			while (pathDown.TryPop(out idx)) {
				subTotal1 = Vector<float>.Zero;
				for (int i = 0; i < parent.Children.Length; i++) {
					if (i == idx) {
						child = parent.Children[i];
					} else if (parent.Children[i].ItemCount > 0) {
						neighbor = (BarnesHutTree)parent.Children[i];
						subTotal2 = Vector<float>.Zero;
						do {//recursively test depth-first for nodes that can be approximated as point masses
							if (neighbor.IsLeaf) {
								nearField.AddRange(neighbor.Bin);
							} else {
								toOther = neighbor.MassBaryCenter.Position - leaf.MassBaryCenter.Position;
								distanceSquared = Vector.Dot(toOther, toOther);
								if (distanceSquared > Parameters.NODE_APPROX_CUTOFF2
								&& distanceSquared * Parameters.INACCURCY2 > neighbor.SizeSquared) {//Barnes-Hut condition
									distance = MathF.Sqrt(distanceSquared);
									subTotal2 += toOther * (neighbor.MassBaryCenter.Weight / distanceSquared / distance);//gravity
								} else {//recurse down
									for (int j = 0; j < neighbor.Children.Length; j++) {
										tail = (BarnesHutTree)neighbor.Children[j];
										if (tail.ItemCount > 0)
											remaining.Push(tail);
									}
								}
							}
						} while (remaining.TryPop(out neighbor));
						subTotal1 += subTotal2;
					}
				}
				//reduce floating point error with subtotalling before adding to running total
				farFieldAcceleration += subTotal1;
				parent = child;
			}
			//finally apply G
			return farFieldAcceleration * Parameters.GRAVITATIONAL_CONSTANT;
		}
	}
}