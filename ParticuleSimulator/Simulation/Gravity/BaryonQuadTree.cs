using Generic.Extensions;
using Generic.Models;

namespace ParticleSimulator.Simulation.Gravity {
	public class BaryonQuadTree : AQuadTree<CelestialBody, BaryonQuadTree> {
		public WeightedIncrementalVectorAverage Barycenter { get; private set; }
		public double TotalMass { get; private set; }

		public BaryonQuadTree(VectorDouble corner1, VectorDouble corner2, BaryonQuadTree parent = null)
		: base(corner1, corner2, parent) {
			this.Barycenter = new WeightedIncrementalVectorAverage(this.Center);
		}

		protected override BaryonQuadTree NewNode(double[] cornerA, double[] cornerB, BaryonQuadTree parent) {
			return new BaryonQuadTree(cornerA, cornerB, parent);
		}
		protected override BaryonQuadTree[] OrganizeNodes(BaryonQuadTree[] nodes) {
			Program.Random.ShuffleInPlace(nodes);
			return nodes;
		}

		protected override void Incorporate(CelestialBody element) {
			this.TotalMass += element.Mass;
			this.Barycenter.Update(element.Coordinates, VectorFunctions.Distance(element.Coordinates, this.Barycenter.Current));
		}
	}
}