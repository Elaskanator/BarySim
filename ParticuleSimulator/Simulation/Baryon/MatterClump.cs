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

		private void SetMass(float value) {
			this.Mass = value;
			this._density = (1f + MathF.Log(value, 16f)) * Parameters.MASS_RADIAL_DENSITY;
			this.Radius = (float)VectorFunctions.HypersphereRadius(value, 3) / this._density;
			this.Luminosity = this.IsCollapsed
				? -1f
				: Parameters.MASS_LUMINOSITY_SCALAR * MathF.Pow(value, Parameters.MASS_LUMINOSITY_POW);
		}

		private float _density = Parameters.MASS_RADIAL_DENSITY;
		public override float Density => this._density;
		public bool IsCollapsed { get; private set; }

		public float Mass;

		public Vector<float> Momentum {
			get => this.Velocity * this.Mass;
			set { this.Velocity = value * (1f / this.Mass); } }

		public Vector<float> Impulse {
			get => this.Acceleration * this.Mass;
			set { this.Acceleration = value * (1f / this.Mass); } }

		public override Tuple<Vector<float>, Vector<float>> ComputeInfluence(MatterClump other) {
			Vector<float> toOther = other.Position - this.Position;
			float distanceSquared = Vector.Dot(toOther, toOther);
			float distance = MathF.Sqrt(distanceSquared);
			float radiusSum = this.Radius + other.Radius;
			
			Vector<float> gravitationalInfluence =
				distance >= radiusSum
					? toOther * (Parameters.GRAVITATIONAL_CONSTANT / (distanceSquared * distance))
				: distance > Parameters.PRECISION_EPSILON//gravity at contact distance
					? toOther * (Parameters.GRAVITATIONAL_CONSTANT / (radiusSum * radiusSum * distance))
					//? toOther * (Parameters.GRAVITATIONAL_CONSTANT / (distanceSquared * distance))
				: Vector<float>.Zero;//cannot safely normalize the direction vector
			
			Vector<float> collisionImpulse = Vector<float>.Zero;
			if (Parameters.COLLISION_ENABLE && distance < radiusSum) {
				if (Parameters.MERGE_ENABLE && distance <= Parameters.MERGE_WITHIN) {
					(this.Mergers ??= new()).Enqueue(other);
					gravitationalInfluence = Vector<float>.Zero;
				} else {
					MatterClump smaller, larger;
					(smaller, larger) = this.Radius <= other.Radius
						? (this, other)
						: (other, this);
					
					float relativeDistance = (distance + smaller.Radius - larger.Radius) / (2f * smaller.Radius);
					if (Parameters.MERGE_ENABLE && relativeDistance + Parameters.MERGE_ENGULF_RATIO <= 1f) {
						(this.Mergers ??= new()).Enqueue(other);
						gravitationalInfluence = Vector<float>.Zero;
					} else {
						Vector<float> dV = other.Velocity - this.Velocity;
						//gravitationalInfluence *= relativeDistance;
						if (Parameters.DRAG_CONSTANT > 0f)
							collisionImpulse = dV * ((1f - relativeDistance) * smaller.Mass * Parameters.DRAG_CONSTANT);
					}
				}
			}

			return new(collisionImpulse, gravitationalInfluence);
		}

		protected override bool IsInRange(BaryCenter center) {
			Vector<float> toCenter = (center.Position - this.Position) * (1f / Parameters.WORLD_SCALE);
			float distanceSquared = Vector.Dot(toCenter, toCenter);
			return distanceSquared <= Parameters.WORLD_PRUNE_RADII_SQUARED//always preserve if near enough the center
				|| Vector.Dot(this.Velocity, this.Velocity) <//below escape velocity
					2f * Parameters.GRAVITATIONAL_CONSTANT * center.Weight
					/ MathF.Sqrt(distanceSquared);
		}

		public override void Consume(MatterClump other) {
			float totalMass = this.Mass + other.Mass;
			float totalMassInv = 1f / totalMass;

			Vector<float> weightedPosition = ((this.Mass*this.Position) + (other.Mass*other.Position)) * totalMassInv;
			Vector<float> weightedAcceleration1 = ((this.Mass*this._acceleration1) + (other.Mass*other._acceleration1)) * totalMassInv;
			Vector<float> weightedAcceleration2 = ((this.Mass*this._acceleration2) + (other.Mass*other._acceleration2)) * totalMassInv;
			Vector<float> totalMomentum = this.Momentum + other.Momentum;
			Vector<float> totalImpulse = this.Impulse + other.Impulse;

			this.SetMass(totalMass);

			this.IsCollapsed |= other.IsCollapsed;
			this.Position = weightedPosition;
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
						float radiusRange = MathF.Pow(this.Radius*this._density*Parameters.SUPERNOVA_RADIUS_SCALAR, Parameters.DIM);
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
								VectorFunctions.RandomUnitVector_Spherical(Parameters.DIM, Program.Engine.Random));
							rand = (float)Program.Engine.Random.NextDouble();
							radius = MathF.Pow(rand*radiusRange, (1f / Parameters.DIM));

							newParticle = new(
								this.Position + direction * radius,
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
	}
}