using Generic.Models;

namespace Simulation.Gravity {
	public class BaryonQuadTree<T> : QuadTree<T>
	where T : AParticle {
		public WeightedIncrementalVectorAverage Barycenter { get; private set; }
		public double TotalMass { get; private set; }

		public BaryonQuadTree(Vector corner1, Vector corner2, QuadTree<T> parent = null)
		: base(corner1, corner2, parent) {
			this.Barycenter = new WeightedIncrementalVectorAverage(this.Center);
		}

		protected override void Incorporate(T element) {
			this.TotalMass += element.Mass;
			this.Barycenter.Update((Vector)element.Coordinates, VectorFunctions.Distance(element.Coordinates, this.Barycenter.Current));
		}
	}
}