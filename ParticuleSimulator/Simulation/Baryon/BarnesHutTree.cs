using System;
using System.Collections.Generic;
using System.Numerics;
using Generic.Vectors;

namespace ParticleSimulator.Simulation.Baryon {
	public class BarnesHutTree : QuadTreeSIMD<MatterClump> {
		public BarnesHutTree(int dim, Vector<float> size) : base(dim, size) { }
		public BarnesHutTree(int dim) : base(dim, Vector<float>.One) { }
		private BarnesHutTree(int dim, Vector<float> corner1, Vector<float> corner2, QuadTreeSIMD<MatterClump> parent)
		: base(dim, corner1, corner2, parent) { }

		protected override BarnesHutTree NewNode(QuadTreeSIMD<MatterClump> parent, Vector<float> cornerLeft, Vector<float> cornerRight) =>
			new BarnesHutTree(this.Dim, cornerLeft, cornerRight, parent);

		public override int LeafCapacity => Parameters.TREE_LEAF_CAPACITY;

		public BaryCenter MassBaryCenter;

		public void InitBaryCenter(MatterClump[] particles) {
			if (particles.Length > 1) {
				BaryCenter total = new(
					particles[0].Mass*particles[0]._position,
					particles[0].Mass);
				for (int i = 1; i < particles.Length; i++)
					total = new(
						total.Position + particles[i].Mass*particles[i]._position,
						total.Weight + particles[i].Mass);
				this.MassBaryCenter = new(
					(1f / total.Weight)*total.Position,
					total.Weight);
			} else this.MassBaryCenter = new(
				particles[0]._position,
				particles[0].Mass);
		}

		public void UpdateBaryCenter() {
			BaryCenter total = new();
			BarnesHutTree child;
			int found = 0;
			for (int i = 0; i < this.Children.Length; i++)
				if (this.Children[i].ItemCount > 0) {
					child = (BarnesHutTree)this.Children[i];
					switch (found++) {
						case 0:
							total = new(child.MassBaryCenter.Position, child.MassBaryCenter.Weight);
							break;
						case 1:
							total = new(
								total.Weight*total.Position + child.MassBaryCenter.Weight*child.MassBaryCenter.Position,
								total.Weight + child.MassBaryCenter.Weight);
							break;
						default:
							total = new(
								total.Position + child.MassBaryCenter.Weight*child.MassBaryCenter.Position,
								total.Weight + child.MassBaryCenter.Weight);
							break;
					}
				}
			this.MassBaryCenter = found == 1
				? total
				: new(total.Position * (1f / total.Weight), total.Weight);
		}

		public Vector<float> DetermineNeighbors(List<MatterClump> nearField) {
			//apply the Barnes Hut proximity criterion to partition the tree into nearby leaves and distance approximations
			Vector<float> farFieldAcceleration = Vector<float>.Zero;

			Stack<Tuple<BarnesHutTree, int>> pathDown = new();
			BarnesHutTree parentNode, childNode;
			int idx = 0;//shut up the compiler error with an initial value
			//get path thru the tree
			parentNode = this;
			while (!parentNode.IsRoot) {
				//determine relative position
				for (int i = 0; i < parentNode.Parent.Children.Length; i++) {
					if (ReferenceEquals(parentNode, parentNode.Parent.Children[i])) {//guaranteed exactly once
						idx = i;
						break;
					}
				}
				pathDown.Push(new(parentNode, idx));
				parentNode = (BarnesHutTree)parentNode.Parent;
			}
			//evaluate from top nodes down to compute furthest (and weakest) interactions first, to reduce floating point errors when aggregating
			Stack<BarnesHutTree> remaining = new();
			Tuple<BarnesHutTree, int> child;
			BarnesHutTree neighbor, tail;
			Vector<float> subTotal1, subTotal2, toOther;
			float distanceSquared, distance;
			while (pathDown.TryPop(out child)) {
				childNode = child.Item1;
				idx = child.Item2;
				subTotal1 = Vector<float>.Zero;
				for (int i = 0; i < parentNode.Children.Length; i++) {
					if (i != idx && parentNode.Children[i].ItemCount > 0) {
						neighbor = (BarnesHutTree)parentNode.Children[i];
						subTotal2 = Vector<float>.Zero;
						do {//recursively test depth-first for nodes that can be approximated as point masses
							if (neighbor.IsLeaf) {
								nearField.AddRange(neighbor.Bin);
							} else {
								toOther = neighbor.MassBaryCenter.Position - this.MassBaryCenter.Position;
								distanceSquared = Vector.Dot(toOther, toOther);
								if (distanceSquared <= Parameters.NODE_EPSILON2) {//TODO check for adjacency instead
									nearField.AddRange(neighbor);
								} else if (distanceSquared * Parameters.INACCURCY2 > neighbor.SizeSquared) {//Barnes-Hut condition
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
				parentNode = childNode;
			}
			//finally apply G
			return farFieldAcceleration * Parameters.GRAVITATIONAL_CONSTANT;
		}
	}
}