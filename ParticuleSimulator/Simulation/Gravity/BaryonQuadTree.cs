using System.Linq;
using Generic.Models;

namespace ParticleSimulator.Simulation.Gravity {
	public class BaryonQuadTree : ASimulationQuadTree<CelestialBody, BaryonQuadTree> {
		public WeightedIncrementalVectorAverage Barycenter { get; private set; }
		public double TotalMass { get; private set; }

		public BaryonQuadTree(double[] corner1, double[] corner2, BaryonQuadTree parent = null)
		: base(corner1, corner2, parent) {
			this.Barycenter = new WeightedIncrementalVectorAverage(this.LeftCorner.Zip(this.RightCorner, (a, b) => (a + b) / 2d).ToArray());
		}

		protected override BaryonQuadTree NewNode(double[] cornerA, double[] cornerB, BaryonQuadTree parent) {
			return new BaryonQuadTree(cornerA, cornerB, parent);
		}

		protected override void Incorporate(CelestialBody element) {
			this.TotalMass += element.Mass;
			this.Barycenter.Update(element.TrueCoordinates, VectorFunctions.Distance(element.TrueCoordinates, this.Barycenter.Current));
		}
	}
}