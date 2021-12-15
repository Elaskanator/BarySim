using Generic.Vectors;

namespace ParticleSimulator.Simulation {
	public abstract class ABaryonParticle<TSelf> : AParticle<TSelf>
	where TSelf : ABaryonParticle<TSelf> {
		public ABaryonParticle(int groupID, double[] position, double[] velocity, double mass = 1d, double charge = 0d)
		: base(groupID, position, velocity) {
			this.NearfieldImpulse = new double[Parameters.DIM];
			this.FarfieldImpulse = new double[Parameters.DIM];
			this.Mass = mass;
			this.Charge = charge;
			this.Velocity = velocity;
		}

		public virtual double Charge { get; set; }
		public virtual double Mass { get; set; }

		public double[] Momentum { get; set; }
		public double[] CollisionImpulse { get; set; }

		public double[] NearfieldImpulse { get; set; }
		public double[] FarfieldImpulse { get; set; }

		public override double[] Velocity {
			get => this.Momentum.Divide(this.Mass);
			set { this.Momentum = value.Multiply(this.Mass); }}
	}
}