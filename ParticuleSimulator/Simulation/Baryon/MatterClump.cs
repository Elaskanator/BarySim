using System;
using System.Numerics;
using Generic.Trees;
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
			this.Luminosity = this.IsCollapsed
				? -1f
				: MathF.Pow(value * Parameters.MASS_LUMINOSITY_SCALAR, 3.5f);
			this.Radius = (float)VectorFunctions.HypersphereRadius(value / this.Density, 3);
		}

		public override float Density => Parameters.GRAVITY_RADIAL_DENSITY;
		public bool IsCollapsed { get; private set; }

		public float Mass;

		public Vector<float> Impulse {
			get => this.Acceleration * this.Mass;
			set { this.Acceleration = value * (1f / this.Mass); } }

		public Vector<float> Momentum {
			get => this.Velocity * this.Mass;
			set { this.Velocity = value * (1f / this.Mass); } }

		public override Tuple<Vector<float>, Vector<float>> ComputeInfluence(MatterClump other) {
			Vector<float> toOther = other.Position - this.Position;
			float distanceSquared = Vector.Dot(toOther, toOther);

			Vector<float> gravitationalInfluence;
			Vector<float> collisionInfluence = Vector<float>.Zero;
			if (distanceSquared <= Parameters.WORLD_EPSILON) {
				gravitationalInfluence = Vector<float>.Zero;
				if (Parameters.MERGE_ENABLE)
					(this.Mergers ??= new()).Enqueue(other);
			} else {
				float distance = MathF.Sqrt(distanceSquared);
				gravitationalInfluence = toOther * (Parameters.GRAVITATIONAL_CONSTANT / (distanceSquared * distance));
				if (Parameters.COLLISION_ENABLE) {
					float radiusSum = this.Radius + other.Radius;
					if (distance < radiusSum) {
						MatterClump smaller, larger;
						(smaller, larger) = this.Radius <= other.Radius
							? (this, other)
							: (other, this);

						if (Parameters.MERGE_ENABLE && distance <= (larger.Radius - Parameters.MERGE_ENGULF_RATIO*smaller.Radius)) {
							gravitationalInfluence = Vector<float>.Zero;
							(this.Mergers ??= new()).Enqueue(other);
						} else {
							float relativeDistance = distance / radiusSum;
							Vector<float> dV = other.Velocity - this.Velocity;
							gravitationalInfluence *= relativeDistance;
							collisionInfluence = dV * ((1f - relativeDistance) * Parameters.DRAG_CONSTANT * smaller.Mass);
						}
					}
				}
			}
			return new(gravitationalInfluence, collisionInfluence);
		}

		protected override bool IsInRange(ATree<MatterClump> tree) {
			BaryCenter center = ((BarnesHutTree)tree).MassBaryCenter;
			Vector<float> toCenter = center.Position - this.Position;
			float distanceSquared = Vector.Dot(toCenter, toCenter);
			return distanceSquared <= Parameters.WORLD_DEATH_BOUND_RADIUS_SQUARED
				|| Vector.Dot(this.Velocity, this.Velocity) <
					2f * Parameters.GRAVITATIONAL_CONSTANT * center.Weight
					/ MathF.Sqrt(distanceSquared);
		}

		protected override void AfterMove() {
			if (!this.IsCollapsed)
				this.EvaluateExplosion();
		}

		public override void Incorporate(MatterClump other) {
			float totalMass = this.Mass + other.Mass;

			Vector<float> totalImpulse = this.Impulse + other.Impulse;
			Vector<float> totalMomentum = this.Momentum + other.Momentum;
			Vector<float> weightedPosition = ((this.Position*this.Mass) + (other.Position*other.Mass)) * (1f / totalMass);

			this.IsCollapsed |= other.IsCollapsed;

			this.SetMass(totalMass);

			this.Position = weightedPosition;
			this.Momentum = totalMomentum;
			this.Impulse = totalImpulse;
		}

		private void EvaluateExplosion() {
			if (Parameters.GRAVITY_SUPERNOVA_ENABLE && this.Mass > Parameters.GRAVITY_CRITICAL_MASS) {//supernova!
				if (Parameters.GRAVITY_BLACK_HOLE_ENABLE && this.Mass > Parameters.GRAVITY_BLACKHOLE_THRESHOLD_RATIO * Parameters.GRAVITY_CRITICAL_MASS) {
					this.IsCollapsed = true;
					this.Luminosity = -1f;
				} else {
					int numParticles = (int)(Parameters.GRAVITY_EJECTA_PARTICLE_MASS > 0
						? this.Mass / Parameters.GRAVITY_EJECTA_PARTICLE_MASS
						: this.Mass);
					numParticles = numParticles > 0 ? numParticles : 1;

					float ratio = (1f / numParticles);
					float avgMass = ratio * this.Mass;
					Vector<float> avgImpulse = ratio * this.Impulse;
				
					float radiusRange = this.Radius * Parameters.GRAVITY_EJECTA_RADIUS_SCALAR;

					this.SetMass(avgMass);
					this.Impulse = avgImpulse;

					Vector<float> direction;
					float rand;
					MatterClump newParticle;
					for (int i = 1; i < numParticles; i++) {
						direction = VectorFunctions.New(
							VectorFunctions.RandomUnitVector_Spherical(Parameters.DIM, Program.Engine.Random));
						rand = (float)Program.Engine.Random.NextDouble();

						newParticle = new(
							this.Position + (((1f - rand*rand) * radiusRange) * direction),
							this.Velocity + ((rand * Parameters.GRAVITY_EJECTA_SPEED) * direction))
						{
							GroupId = this.GroupId
						};
						newParticle.SetMass(avgMass);
						newParticle.Impulse += avgImpulse;

						this.NewParticles ??= new();
						this.NewParticles.Enqueue(newParticle);
					}
				}
			}
		}
	}
}