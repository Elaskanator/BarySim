using System;
using System.Numerics;
using System.Linq;
using Generic.Models.Trees;
using Generic.Vectors;

namespace ParticleSimulator.Simulation.Baryon {
	public class BarnesHutTree : QuadTreeSIMD<MatterClump> {
		public BarnesHutTree(int dim, Vector<float> size) : base(dim, size) { }
		public BarnesHutTree(int dim) : base(dim, Vector<float>.One) { }
		private BarnesHutTree(int dim, Vector<float> corner1, Vector<float> corner2, QuadTreeSIMD<MatterClump> parent)
		: base(dim, corner1, corner2, parent) { }

		protected override BarnesHutTree NewNode(QuadTreeSIMD<MatterClump> parent, Vector<float> cornerLeft, Vector<float> cornerRight) =>
			new BarnesHutTree(this.Dim, cornerLeft, cornerRight, parent);

		public override int Capacity => Parameters.QUADTREE_NODE_CAPACITY;

		public BaryCenter MassBaryCenter { get; private set; }

		//protected override bool TryMerge(Particle p1) {
		//	if (Parameters.GRAVITY_COLLISION_COMBINE)
		//		foreach (Particle p2 in this.Bin)
		//			if (p2.TryMerge(p1))
		//				return true;
		//	return false;
		//}

		public void UpdateBarycenter() {
			Tuple<Vector<float>, float> total = new(Vector<float>.Zero, 0f);
			BarnesHutTree child;
			MatterClump particle;
			if (this.IsLeaf) {
				if (this.Bin.Count == 1) {
					particle = this.Bin.First();
					this.MassBaryCenter = new(particle.Position, particle.Mass);
				} else {
					foreach (MatterClump p in this.Bin.Where(p => p.Mass > 0f))
						total = new(
							total.Item1 + (p.Position * p.Mass),
							total.Item2 + p.Mass);
					if (total.Item2 > 0f)
						this.MassBaryCenter = new(total.Item1 * (1f / total.Item2), total.Item2);
					else this.MassBaryCenter = new(this.Center, 0f);
				}
			} else {
				for (int i = 0; i < this.Children.Length; i++) {
					if (this.Children[i].Count > 0) {
						child = (BarnesHutTree)this.Children[i];
						if (child.MassBaryCenter.Weight > 0f) {
							total = new(
								total.Item1 + (child.MassBaryCenter.Position * child.MassBaryCenter.Weight),
								total.Item2 + child.MassBaryCenter.Weight);
						}
					}
				}
				if (total.Item2 > 0f)
					this.MassBaryCenter = new(total.Item1 * (1f / total.Item2), total.Item2);
				else this.MassBaryCenter = new(this.Center, 0f);
			}
		}
		
		public bool CanApproximate(BarnesHutTree node) {
			float dist = this.MassBaryCenter.Position.Distance(node.MassBaryCenter.Position);
			return Parameters.WORLD_EPSILON < dist
				&& Parameters.INACCURCY > node.Size[0] / dist;
		}
	}
}