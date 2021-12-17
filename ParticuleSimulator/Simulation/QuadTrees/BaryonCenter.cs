using System.Numerics;
using Generic.Vectors;

namespace ParticleSimulator.Simulation {
	public class BaryonCenter {
		public BaryonCenter(VectorIncrementalWeightedAverage center = null) {
			this.Center = center ?? new();
		}

		public readonly VectorIncrementalWeightedAverage Center;

		public Vector<float> Coordinates => this.Center.Current;
		public float TotalWeight => this.Center.TotalWeight;

		public void Update(Vector<float> position, double weight) {
			this.Center.Update(position, weight);
		}
	}
}