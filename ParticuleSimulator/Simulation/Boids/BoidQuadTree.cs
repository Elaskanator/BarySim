using System.Linq;
using Generic.Extensions;
using Generic.Vectors;

namespace ParticleSimulator.Simulation.Boids {
	public sealed class BoidQuadTree : AVectorQuadTree<Boid, BoidQuadTree> {
		public BoidQuadTree(double[] corner1 = null, double[] corner2 = null, BoidQuadTree parent = null)
		: base(corner1 ?? new double[Parameters.DIM], corner2 ?? Parameters.DOMAIN_SIZE, parent) { }

		protected override BoidQuadTree NewNode(double[] cornerA, double[] cornerB, BoidQuadTree parent) {
			return new(cornerA, cornerB, parent);
		}

		protected override double[] MakeCenter() {
			double minDivision = 1d / (1 << (this.Capacity / 2));
			return Enumerable
				.Range(0, Parameters.DIM)
				.Select(d => this._members.Average(m => m.Coordinates[d]))
				.Select((avg, d) =>
					avg < this.CornerLeft[d] + minDivision * (this.CornerRight[d] - this.CornerLeft[d])
						? this.CornerLeft[d] + minDivision * (this.CornerRight[d] - this.CornerLeft[d])
						: avg > this.CornerRight[d] - minDivision * (this.CornerRight[d] - this.CornerLeft[d])
							? this.CornerRight[d] - minDivision * (this.CornerRight[d] - this.CornerLeft[d])
							: avg)
				.ToArray();
		}
		protected override BoidQuadTree GetContainingChild(Boid element) {
			return this._quadrants.Single(q => q.DoesContain(element));
		}

		protected override void ArrangeNodes() {
			Program.Random.ShuffleInPlace(this._quadrants);
		}
	}
}