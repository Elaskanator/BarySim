using System.Numerics;

namespace ParticleSimulator.Simulation {
	public abstract class ABaryonParticle<TSelf> : ASimulationParticle<TSelf>
	where TSelf : ABaryonParticle<TSelf> {
		public ABaryonParticle(int groupID, Vector<float> position, Vector<float> velocity, float mass = 1f, float charge = 0f)
		: base(groupID, position, velocity) {
			this.NearfieldImpulse = Vector<float>.Zero;
			this.FarfieldImpulse = Vector<float>.Zero;
			this.Mass = mass;
			this.Charge = charge;
			this.Velocity = velocity;
		}

		public virtual float Charge { get; set; }
		public virtual float Mass { get; set; }

		public Vector<float> Momentum { get; set; }
		public Vector<float> CollisionImpulse { get; set; }

		public Vector<float> NearfieldImpulse { get; set; }
		public Vector<float> FarfieldImpulse { get; set; }

		public override Vector<float> Velocity {
			get => this.Momentum * (1f / this.Mass);
			set { this.Momentum = this.Mass * value; }}
	}
}