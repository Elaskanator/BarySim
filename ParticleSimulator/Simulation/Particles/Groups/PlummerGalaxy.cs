using System;
using System.Linq;
using System.Numerics;
using Generic.Vectors;
using ParticleSimulator.Simulation.Baryon;

namespace ParticleSimulator.Simulation.Particles {
	public class PlummerGalaxy: AParticleGroup<MatterClump> {
		public PlummerGalaxy(Func<Vector<float>, Vector<float>, MatterClump> initializer, float radius)
		: base(initializer, radius) {
			//this.GlobalDirection = Program.Engine.Random.NextDouble() < 0.5d;
			this.InternalDirection = Program.Random.NextDouble() < 0.5d;
		}

		public readonly bool GlobalDirection;
		public readonly bool InternalDirection;

		public float InitialMass { get; private set; }

		protected override void InitGroupPositionVelocity() {
			base.InitGroupPositionVelocity();
			this.InitialMass = this.InitialParticles.Sum(p => p.Mass);
			if (Parameters.PARTICLES_GROUP_COUNT > 1) {
				this.Velocity +=
					  (this.GlobalDirection ? 1f : -1f)
					* Parameters.GALAXY_SPEED_ANGULAR
					* this.DirectionUnitVector(this.Position);
			} else this.Position = Vector<float>.Zero;
		}

		protected override void InitializeParticles(MatterClump particle) {
			float nextRand = (float)Math.Clamp(Program.Random.NextDouble(), Parameters.PRECISION_EPSILON, 1d);
			float plummerRadius = MathF.Pow(MathF.Pow(nextRand, -2f/3f) - 1f, -0.5f);
			Vector<float> position = plummerRadius * VectorFunctions.RandomDirectionVector(Parameters.DIM, Program.Random);
			Vector<float> velocity = SamplePlummerSpeed(plummerRadius);

			particle._position += position * Parameters.GALAXY_PLUMMER_RADIUS;
			float velocityScale = MathF.Sqrt(Parameters.GRAVITATIONAL_CONSTANT * this.InitialMass / Parameters.GALAXY_PLUMMER_RADIUS);
			particle.Velocity += velocity * velocityScale;
		}
		private Vector<float> SamplePlummerSpeed(float r) {
			float escapeVelocity = MathF.Sqrt(2f / MathF.Sqrt(r*r + 1f));

			float resultVelocity, trialSpeedFraction, acceptanceThreshold;
			while (true) {
				trialSpeedFraction = (float)Program.Random.NextDouble();
				acceptanceThreshold = (float)Program.Random.NextDouble();
				if (acceptanceThreshold < MathF.Pow(1f - trialSpeedFraction*trialSpeedFraction, 3.5f)) {
					resultVelocity = trialSpeedFraction * escapeVelocity;
					break;
				}
			}

			return resultVelocity * VectorFunctions.RandomDirectionVector(Parameters.DIM, Program.Random);
		}

		protected Vector<float> DirectionUnitVector(Vector<float> offset) {//only in 2D
			if (Parameters.DIM > 1) {
				float angle = MathF.Atan2(offset[1], offset[0]) + 0.5f*MathF.PI;
				return VectorFunctions.New(new float[] {
					MathF.Cos(angle),
					MathF.Sin(angle) });
			} else return Vector<float>.Zero;
		}
	}
}