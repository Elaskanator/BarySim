using Generic.Vectors;

namespace ParticleSimulator.Simulation {
	public class BaryonCenter : VectorDouble {
		public BaryonCenter(VectorIncrementalWeightedAverage center = null) {
			this.Center = center ?? new();
		}

		public readonly VectorIncrementalWeightedAverage Center = new();

		public override double[] Coordinates => this.Center.Current;
		public double TotalWeight => this.Center.TotalWeight;

		public void Update(double[] position, double weight) {
			this.Center.Update(position, weight);
		}
	}
}