using System.Numerics;
using Generic.Trees;
using Generic.Vectors;

namespace ParticleSimulator.Simulation.Baryon {
	public class BarnesHutTree : QuadTreeSIMD<MatterClump> {
		public BarnesHutTree(int dim, Vector<float> size) : base(dim, size) { }
		public BarnesHutTree(int dim) : base(dim, Vector<float>.One) { }
		private BarnesHutTree(int dim, Vector<float> corner1, Vector<float> corner2, AHyperdimensionalBinaryTree<MatterClump, Vector<float>> parent)
		: base(dim, corner1, corner2, parent) { }

		protected override QuadTreeSIMD<MatterClump> NewNode(Vector<float> cornerLeft, Vector<float> cornerRight, AHyperdimensionalBinaryTree<MatterClump, Vector<float>> parent) =>
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
					total = found++ switch {
						//lazy skipping of reweighting if there ends up only being one child
						0 => new(child.MassBaryCenter.Position, child.MassBaryCenter.Weight),
						1 => new(total.Weight * total.Position + child.MassBaryCenter.Weight * child.MassBaryCenter.Position,
								total.Weight + child.MassBaryCenter.Weight),
						_ => new(total.Position + child.MassBaryCenter.Weight * child.MassBaryCenter.Position,
								total.Weight + child.MassBaryCenter.Weight),
					};
				}
			this.MassBaryCenter = found == 1
				? total
				: new(total.Position * (1f / total.Weight), total.Weight);
		}
	}
}