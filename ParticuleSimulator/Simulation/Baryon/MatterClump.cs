using System;
using System.Numerics;
using Generic.Vectors;
using ParticleSimulator.Simulation.Particles;

namespace ParticleSimulator.Simulation.Baryon {
	public class MatterClump : AParticle<MatterClump> {
		public MatterClump(Vector<float> position, Vector<float> velocity)
		: base(position, velocity) {
			this.SetMass(Parameters.MASS_SCALAR);
		}
		
		public float Mass;
		private void SetMass(float value) {
			this.Mass = value;
			this._density = (1f + MathF.Log(1f + value, 16f)) * Parameters.MASS_RADIAL_DENSITY;
			this._radius = (float)VectorFunctions.HypersphereRadius(value, 3) / this._density;
			this.Luminosity = this.IsCollapsed
				? -1f
				: Parameters.MASS_LUMINOSITY_SCALAR * MathF.Pow(value, Parameters.MASS_LUMINOSITY_POW);
		}
		
		private float _density = Parameters.MASS_RADIAL_DENSITY;
		public override float Density => this._density;
		public bool IsCollapsed { get; private set; }

		public override Vector<float> Momentum {
			get => this.Velocity * this.Mass;
			set { this.Velocity = value * (1f / this.Mass); } }

		public override Vector<float> Impulse {
			get => this.Acceleration * this.Mass;
			set { this.Acceleration = value * (1f / this.Mass); } }

		protected override Vector<float> ComputeForceImpulse(MatterClump other, Vector<float> toOther, float distance, float distance2) {
			float largerRadius = this._radius > other._radius ? this._radius : other._radius;
			distance = distance >= largerRadius ? distance : largerRadius;
			return toOther * (Parameters.GRAVITATIONAL_CONSTANT * this.Mass * other.Mass / (distance2 * distance));
		}

		public override Vector<float> ComputeCollisionImpulse(MatterClump other, float engulfRelativeDistance) {
			if (Parameters.DRAG_CONSTANT > 0f) {
				Vector<float> dV = other.Velocity - this.Velocity;
				float smallerMass = this.Mass > other.Mass ? other.Mass : this.Mass;
				return dV * ((1f - engulfRelativeDistance) * smallerMass * Parameters.DRAG_CONSTANT);
			} else return Vector<float>.Zero;
		}

		protected override void Absorb(MatterClump other) {
			float totalMass = this.Mass + other.Mass;
			float totalMassInv = 1f / totalMass;

			Vector<float> weightedPosition = ((this.Mass*this._position) + (other.Mass*other._position)) * totalMassInv;
			Vector<float> weightedAcceleration1 = ((this.Mass*this._acceleration1) + (other.Mass*other._acceleration1)) * totalMassInv;
			Vector<float> weightedAcceleration2 = ((this.Mass*this._acceleration2) + (other.Mass*other._acceleration2)) * totalMassInv;
			Vector<float> totalMomentum = this.Momentum + other.Momentum;
			Vector<float> totalImpulse = this.Impulse + other.Impulse;

			this.SetMass(totalMass);

			this.IsCollapsed |= other.IsCollapsed;
			this._position = weightedPosition;
			this.Momentum = totalMomentum;
			this.Impulse = totalImpulse;
			this._acceleration1 = weightedAcceleration1;
			this._acceleration2 = weightedAcceleration2;
		}

		protected override void AfterMove() {
			if (Parameters.SUPERNOVA_ENABLE && !this.IsCollapsed && this.Mass >= Parameters.SUPERNOVA_CRITICAL_MASS) {
				if (Parameters.BLACKHOLE_ENABLE && this.Mass >= Parameters.BLACKHOLE_THRESHOLD * Parameters.SUPERNOVA_CRITICAL_MASS) {
					this.IsCollapsed = true;
					this.Luminosity = -1f;
				} else {
					int numParticles = (int)(Parameters.SUPERNOVA_EJECTA_MASS > 0
						? this.Mass / Parameters.SUPERNOVA_EJECTA_MASS
						: this.Mass);
					if (numParticles > 1) {
						float radiusRange = MathF.Pow(this._radius*this._density*Parameters.SUPERNOVA_RADIUS_SCALAR, Parameters.DIM);
						float ratio = (1f / numParticles);
						float avgMass = ratio * this.Mass;
						Vector<float> avgImpulse = ratio * this.Impulse;

						this.NewParticles ??= new();
						this.SetMass(avgMass);
						this.Impulse = avgImpulse;

						Vector<float> direction;
						float rand, radius;
						MatterClump newParticle;
						for (int i = 1; i < numParticles; i++) {
							direction = VectorFunctions.New(
								VectorFunctions.RandomUnitVector_Spherical(Parameters.DIM, Program.Random));
							rand = (float)Program.Random.NextDouble();
							radius = MathF.Pow(rand*radiusRange, (1f / Parameters.DIM));

							newParticle = new(
								this._position + direction * radius,
								this.Velocity + direction * Parameters.SUPERNOVA_EJECTA_SPEED)
							{
								GroupId = this.GroupId,
							};
							newParticle.SetMass(avgMass);
							newParticle.Impulse += avgImpulse;

							this.NewParticles.Enqueue(newParticle);
						}
					}
				}
			}
		}

		public override bool IsInRange(BaryCenter center) {
			Vector<float> toCenter = (center.Position - this._position) * (1f / Parameters.WORLD_SCALE);
			float distanceSquared = Vector.Dot(toCenter, toCenter);
			return distanceSquared <= Parameters.WORLD_PRUNE_RADII_SQUARED//always preserve if near enough the center
				|| Vector.Dot(this.Velocity, this.Velocity) <//below escape velocity
					2f * Parameters.GRAVITATIONAL_CONSTANT * center.Weight
					/ MathF.Sqrt(distanceSquared);
		}
	}
}