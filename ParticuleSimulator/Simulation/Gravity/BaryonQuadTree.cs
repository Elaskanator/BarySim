using System.Linq;
using Generic.Extensions;
using Generic.Vectors;

namespace ParticleSimulator.Simulation.Gravity {
	public class BaryonQuadTree : AVectorQuadTree<MatterClump, BaryonQuadTree> {
		public BaryonQuadTree(double[] corner1 = null, double[] corner2 = null, BaryonQuadTree parent = null)
		: base(corner1 ?? new double[Parameters.DIM], corner2 ?? Parameters.DOMAIN_SIZE, parent) {
			this.Barycenter = new VectorIncrementalWeightedAverage();
		}

		public override int Capacity => Parameters.GRAVITY_QUADTREE_NODE_CAPACITY;
		public VectorIncrementalWeightedAverage Barycenter { get; private set; }

		protected override BaryonQuadTree NewNode(double[] cornerA, double[] cornerB, BaryonQuadTree parent) {
			return new(cornerA, cornerB, parent);
		}
		
		protected override double[] MakeCenter() {
			return this.CornerLeft.Zip(this.CornerRight, (a, b) => a + Program.Random.NextDouble() * (b - a)).ToArray();
		}
		protected override BaryonQuadTree GetContainingChild(MatterClump element) {
			return this._quadrants.Single(q => q.DoesContain(element));
		}

		protected override void ArrangeNodes() {
			Program.Random.ShuffleInPlace(this._quadrants);
		}
		
		protected override void Incorporate(MatterClump element) {
			this.Barycenter.Update(element.LiveCoordinates, element.Mass);
		}
	}
}
