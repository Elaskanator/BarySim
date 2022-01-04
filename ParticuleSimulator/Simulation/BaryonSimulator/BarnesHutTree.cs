using System;
using System.Numerics;
using System.Linq;
using Generic.Models.Trees;
using Generic.Vectors;

namespace ParticleSimulator.Simulation.Baryon {
	public class BarnesHutTree : QuadTreeSIMD<Particle> {
		public BarnesHutTree(int dim, Vector<float> size) : base(dim, size) { }
		public BarnesHutTree(int dim) : base(dim, Vector<float>.One) { }
		private BarnesHutTree(int dim, Vector<float> corner1, Vector<float> corner2, QuadTreeSIMD<Particle> parent)
		: base(dim, corner1, corner2, parent) { }

		protected override BarnesHutTree NewNode(QuadTreeSIMD<Particle> parent, Vector<float> cornerLeft, Vector<float> cornerRight) =>
			new BarnesHutTree(this.Dim, cornerLeft, cornerRight, parent);

		public override int Capacity => Parameters.QUADTREE_NODE_CAPACITY;

		public Tuple<Vector<float>, float> Barycenter { get; private set; }

		protected override bool TryMerge(Particle p1) {
			if (Parameters.GRAVITY_COLLISION_COMBINE)
				foreach (Particle p2 in this.Bin)
					if (p2.TryMerge(p1))
						return true;
			return false;
		}

		public void UpdateBarycenter() {
			Tuple<Vector<float>, float> total = new(Vector<float>.Zero, 0f);
			BarnesHutTree child;
			Particle particle;
			if (this.IsLeaf) {
				if (this.Bin.Count == 1) {
					particle = this.Bin.First();
					this.Barycenter = new(particle.Position, particle.Mass);
				} else {
					foreach (Particle p in this.Bin.Where(p => p.Mass > 0f))
						total = new(
							total.Item1 + (p.Position * p.Mass),
							total.Item2 + p.Mass);
					if (total.Item2 > 0f)
						this.Barycenter = new(total.Item1 * (1f / total.Item2), total.Item2);
					else this.Barycenter = new(this.Center, 0f);
				}
			} else {
				for (int i = 0; i < this.Children.Length; i++) {
					if (this.Children[i].Count > 0) {
						child = (BarnesHutTree)this.Children[i];
						if (child.Barycenter.Item2 > 0f) {
							total = new(
								total.Item1 + (child.Barycenter.Item1 * child.Barycenter.Item2),
								total.Item2 + child.Barycenter.Item2);
						}
					}
				}
				if (total.Item2 > 0f)
					this.Barycenter = new(total.Item1 * (1f / total.Item2), total.Item2);
				else this.Barycenter = new(this.Center, 0f);
			}
		}
		
		public bool CanApproximate(BarnesHutTree node) {
			float dist = this.Barycenter.Item1.Distance(node.Barycenter.Item1);
			return Parameters.WORLD_EPSILON < dist
				&& Parameters.INACCURCY > node.Size[0] / dist;
		}
	}
}