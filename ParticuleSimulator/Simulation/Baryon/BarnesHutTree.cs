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

		public override int LeafCapacity => Parameters.QUADTREE_LEAF_CAPACITY;

		public BaryCenter MassBaryCenter;

		public void InitBaryCenter(MatterClump[] particles) {
			if (particles.Length > 1) {
				BaryCenter total = new(
					particles[0].Mass*particles[0].Position,
					particles[0].Mass);
				for (int i = 1; i < particles.Length; i++)
					total = new(
						total.Position + particles[i].Mass*particles[i].Position,
						total.Weight + particles[i].Mass);
				this.MassBaryCenter = new(
					(1f / total.Weight)*total.Position,
					total.Weight);
			} else this.MassBaryCenter = new(
				particles[0].Position,
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

		public bool CanApproximate(BarnesHutTree node) {
			Vector<float> pointingVector = node.MassBaryCenter.Position - this.MassBaryCenter.Position;
			return Parameters.INACCURCY_SQUARED > node.SizeSquared / Vector.Dot(pointingVector, pointingVector);
		}
	}
}