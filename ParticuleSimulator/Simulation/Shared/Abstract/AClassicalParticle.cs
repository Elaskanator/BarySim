using Generic.Vectors;

namespace ParticleSimulator.Simulation {
	public abstract class AClassicalParticle<TSelf> : AParticle<TSelf>
	where TSelf : AClassicalParticle<TSelf> {
		public AClassicalParticle(int groupID, double[] position, double[] velocity, double mass = 1d, double charge = 0d)
		: base(groupID, position, velocity) {
			this.Momentum = new double[Parameters.DIM];
			this.NearfieldImpulse = new double[Parameters.DIM];
			this.FarfieldImpulse = new double[Parameters.DIM];
			this.Mass = mass;
			this.Charge = charge;
		}

		public virtual double Charge { get; set; }
		public virtual double Mass { get; set; }

		public double[] Momentum { get; set; }

		public double[] NearfieldImpulse { get; set; }
		public double[] FarfieldImpulse { get; set; }

		public override double[] Velocity {
			get => this.Momentum.Divide(this.Mass);
			set { this.Momentum = value.Multiply(this.Mass); }}

		protected override void Incorporate(TSelf other) {
			double totalMass = this.Mass + other.Mass;
			this.LiveCoordinates =
				this.LiveCoordinates.Multiply(this.Mass)
				.Add(other.LiveCoordinates.Multiply(other.Mass))
				.Divide(totalMass);
			this.Mass = totalMass;
			this.Charge += other.Charge;
			this.Momentum = this.Momentum.Add(other.Momentum);
			this.NearfieldImpulse = this.NearfieldImpulse.Add(other.NearfieldImpulse);
			this.FarfieldImpulse = this.FarfieldImpulse.Add(other.FarfieldImpulse);
			this.CollisionAcceleration = this.CollisionAcceleration.Add(other.CollisionAcceleration);
		}
	}
}