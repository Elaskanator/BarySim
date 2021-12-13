using System.Linq;
using Generic.Extensions;
using Generic.Vectors;

namespace ParticleSimulator.Simulation {
	public sealed class NearFieldQuadTree : AVectorQuadTree<AClassicalParticle> {
		public NearFieldQuadTree(double[] corner1, double[] corner2, AVectorQuadTree<AClassicalParticle> parent = null)
		: base(corner1, corner2, parent) { }
		
		public override int Capacity => Parameters.BOIDS_QUADTREE_NODE_CAPACITY;

		protected override AVectorQuadTree<AClassicalParticle> NewNode(double[] cornerA, double[] cornerB, AVectorQuadTree<AClassicalParticle> parent) {
			return new NearFieldQuadTree(cornerA, cornerB, parent);
		}

		//protected override double[] MakeCenter() {
		//	double minDivision = 1d / (1 << (this.Capacity / 2));
		//	return Enumerable
		//		.Range(0, Parameters.DIM)
		//		.Select(d => this._members.Average(m => m.Coordinates[d]))
		//		.Select((avg, d) =>
		//			avg < this.CornerLeft[d] + minDivision * (this.CornerRight[d] - this.CornerLeft[d])
		//				? this.CornerLeft[d] + minDivision * (this.CornerRight[d] - this.CornerLeft[d])
		//				: avg > this.CornerRight[d] - minDivision * (this.CornerRight[d] - this.CornerLeft[d])
		//					? this.CornerRight[d] - minDivision * (this.CornerRight[d] - this.CornerLeft[d])
		//					: avg)
		//		.ToArray();
		//}
		protected override double[] MakeCenter() {
			return this.CornerLeft.Zip(this.CornerRight, (a, b) => a + Program.Random.NextDouble() * (b - a)).ToArray();
		}
		protected override AVectorQuadTree<AClassicalParticle> GetContainingChild(AClassicalParticle element) {
			return this._quadrants.Single(q => q.DoesContain(element));
		}

		protected override void ArrangeNodes() {
			Program.Random.ShuffleInPlace(this._quadrants);
		}
	}
}