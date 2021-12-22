using System.Numerics;
using Generic.Models.Trees;

namespace ParticleSimulator.Simulation {
	public class BarnesHutTree<T> : QuadTreeSIMD<T, float>
	where T : BaryonParticle {
		public BarnesHutTree(int dim, Vector<float> corner1, Vector<float> corner2, QuadTreeSIMD<T, float> parent = null)
		: base(dim, corner1, corner2, parent) { }
		public BarnesHutTree(int dim) : base(dim) { }
		protected override BarnesHutTree<T> NewNode(QuadTreeSIMD<T, float> parent, Vector<float> cornerLeft, Vector<float> cornerRight) =>
			new BarnesHutTree<T>(this.Dim, cornerLeft, cornerRight, parent);

		public override int Capacity => Parameters.QUADTREE_NODE_CAPACITY;

		public readonly BaryonCenter BaryCenter_Position = new();
		public readonly BaryonCenter BaryCenter_Mass = new();
		public readonly BaryonCenter BaryCenter_Charge = new();

		protected override void AfterRemove(T item) {
			this.BaryCenter_Position.Update(item.Position, -1d);
			this.BaryCenter_Mass.Update(item.Position, -item.Mass);
			this.BaryCenter_Charge.Update(item.Position, -item.Charge);
		}

		protected override void Incorporate(T item) {
			this.BaryCenter_Position.Update(item.Position, 1d);
			this.BaryCenter_Mass.Update(item.Position, item.Mass);
			this.BaryCenter_Charge.Update(item.Position, item.Charge);
		}
	}
}