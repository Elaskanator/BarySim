using System.Linq;
using Generic.Extensions;
using Generic.Models;

namespace ParticleSimulator.Simulation.Boids {
	public sealed class BoidQuadTree : AVectorQuadTree<Boid, BoidQuadTree> {//NOT thread safe
		public BoidQuadTree(double[] corner1 = null, double[] corner2 = null, BoidQuadTree parent = null)
		: base(corner1 ?? new double[Parameters.DIM], corner2 ?? Parameters.DOMAIN, parent) { }

		protected override BoidQuadTree NewNode(double[] cornerA, double[] cornerB, BoidQuadTree parent) {
			return new(cornerA, cornerB, parent);
		}

		protected override double[] MakeCenter() {
			return this.LeftCorner.Zip(this.RightCorner, (a, b) => a + Program.Random.NextDouble() * (b - a)).ToArray();
		}

		protected override BoidQuadTree GetContainingChild(Boid element) {
			return this._quadrants.Single(q => q.DoesContain(element));
		}

		protected override void ArrangeNodes() {
			Program.Random.ShuffleInPlace(this._quadrants);
		}
	}
}