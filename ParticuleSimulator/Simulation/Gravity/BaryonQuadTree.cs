using System.Linq;
using Generic.Models;
using Generic.Extensions;

namespace ParticleSimulator.Simulation.Gravity {
	public class BaryonQuadTree : AVectorQuadTree<CelestialBody, BaryonQuadTree> {//NOT thread safe
		public BaryonQuadTree(double[] corner1 = null, double[] corner2 = null, BaryonQuadTree parent = null)
		: base(corner1 ?? new double[Parameters.DIM], corner2 ?? Parameters.DOMAIN, parent) {
			this.Barycenter = new VectorIncrementalWeightedAverage();
		}

		public VectorIncrementalWeightedAverage Barycenter { get; private set; }
		public double TotalMass { get; private set; }
		public override int Capacity => Parameters.GRAVITY_QUADTREE_NODE_CAPACITY;

		protected override BaryonQuadTree NewNode(double[] cornerA, double[] cornerB, BaryonQuadTree parent) {
			return new(cornerA, cornerB, parent);
		}
		
		protected override double[] MakeCenter() {
			return this.LeftCorner.Zip(this.RightCorner, (a, b) => a + Program.Random.NextDouble() * (b - a)).ToArray();
		}
		protected override BaryonQuadTree GetContainingChild(CelestialBody element) {
			return this._quadrants.Single(q => q.DoesContain(element));
		}

		protected override void Incorporate(CelestialBody element) {
			this.TotalMass += element.Mass;
			this.Barycenter.Update(element.LiveCoordinates, element.Mass / this.TotalMass);
		}
	}
}
