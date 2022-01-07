using System;
using System.Linq;
using System.Numerics;
using Generic.Models.Trees;
using Generic.Vectors;
using ParticleSimulator.Simulation.Particles;

namespace ParticleSimulator.Simulation.Baryon {
	public class MatterClump : AParticle<MatterClump> {
		public MatterClump(int groupId, Vector<float> position, Vector<float> velocity)
		: base(groupId, position, velocity) { }

		public float Density => Parameters.GRAVITY_RADIAL_DENSITY;
		private float _mass = 0f;
		public float Mass {
			get => this._mass;
			set {
				this._mass = value;
				this.Radius = (float)VectorFunctions.HypersphereRadius(value / this.Density, 3);
				this.Luminosity = MathF.Pow(value * Parameters.MASS_LUMINOSITY_SCALAR, 3.5f); }}

		public Vector<float> Impulse {
			get => this.Acceleration * this.Mass;
			set { this.Acceleration = value * (1f / this.Mass); } }

		public Vector<float> Momentum {
			get => this.Velocity * this.Mass;
			set { this.Velocity = value * (1f / this.Mass); } }

		public override Tuple<Vector<float>, Vector<float>> ComputeInfluence(MatterClump other) {
			Vector<float> toOther = other.Position - this.Position;
			float distanceSquared = Vector.Dot(toOther, toOther);

			if (Parameters.MERGE_ENABLE && distanceSquared <= Parameters.WORLD_EPSILON) {
				this.Mergers.Enqueue(other);
				return new(Vector<float>.Zero, Vector<float>.Zero);
			} else {
				Vector<float> gravitationalInfluence = toOther * (Parameters.GRAVITATIONAL_CONSTANT / distanceSquared);
				float sumRadiusSquared = this.RadiusSquared + 2f*this.Radius*other.Radius + other.RadiusSquared;
				if (distanceSquared >= sumRadiusSquared) {
					return new(gravitationalInfluence, Vector<float>.Zero);
				} else {
					MatterClump larger, smaller;
					if (this.Radius >= other.Radius) {
						larger = this;
						smaller = other;
					} else {
						larger = other;
						smaller = this;
					}

					if (distanceSquared <= Parameters.MERGE_ENGULF_RATIO * (larger.RadiusSquared - 2f*larger.Radius*smaller.Radius + smaller.RadiusSquared)) {
						this.Mergers.Enqueue(other);
						return new(Vector<float>.Zero, Vector<float>.Zero);
					} else {
						float relativeDistance = sumRadiusSquared <= Parameters.WORLD_EPSILON ? 0f : MathF.Sqrt(distanceSquared / sumRadiusSquared);
						return new(
							gravitationalInfluence,
							(1f - relativeDistance) * Parameters.DRAG_CONSTANT * smaller.Mass * (other.Velocity - this.Velocity));
					}
				}
			}
		}


		public override void Incorporate(MatterClump other) {
			float totalMass = this.Mass + other.Mass;
			float totalCharge = this.Charge + other.Charge;

			Vector<float> totalImpulse = this.Impulse + other.Impulse;
			Vector<float> totalMomentum = this.Momentum + other.Momentum;
			Vector<float> weightedPosition = ((this.Position*this.Mass) + (other.Position*other.Mass)) * (1f / totalMass);

			this.Mass = totalMass;
			this.Charge = totalCharge;

			this.Position = weightedPosition;
			this.Momentum = totalMomentum;
			this.Impulse = totalImpulse;

			other.Enabled = false;

			this.EvaluateExplosion();
		}

		private void EvaluateExplosion() {
			if (this.Mass >= Parameters.GRAVITY_CRITICAL_MASS) {//supernova!
				int numParticles = (int)(Parameters.GRAVITY_EJECTA_PARTICLE_MASS > 0
					? this.Mass / Parameters.GRAVITY_EJECTA_PARTICLE_MASS
					: this.Mass);
				numParticles = numParticles > 0 ? numParticles : 1;

				float ratio = (1f / numParticles);
				float avgMass = ratio * this.Mass;
				float avgCharge = ratio * this.Charge;
				Vector<float> avgImpulse = ratio * this.Impulse;
				
				float radiusRange = 2f*this.Radius;

				this.Mass = avgMass;
				this.Charge = avgCharge;
				this.Impulse = avgImpulse;

				Vector<float> direction;
				float rand;
				MatterClump newParticle;
				for (int i = 1; i < numParticles; i++) {
					direction = VectorFunctions.New(
						VectorFunctions.RandomUnitVector_Spherical(Parameters.DIM, Program.Engine.Random)
							.Select(x => (float)x));
					rand = (float)Program.Engine.Random.NextDouble();

					newParticle = new(
						this.GroupId,
						this.Position + (((1f - rand*rand) * radiusRange) * direction),
						this.Velocity + ((rand * Parameters.GRAVITY_EJECTA_SPEED) * direction));
					newParticle.Mass = avgMass;
					newParticle.Charge = avgCharge;
					newParticle.Impulse += avgImpulse;

					this.NewParticles.Enqueue(newParticle);
				}
			}
		}

		protected override bool TestInRange(ATree<MatterClump> world) {
			BaryCenter center = ((BarnesHutTree)world).MassBaryCenter;
			return Vector.Dot(this.Velocity, this.Velocity) <
				2f * Parameters.GRAVITATIONAL_CONSTANT * center.Weight
				/ this.Position.Distance(center.Position);
		}
	}
}