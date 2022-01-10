using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Generic.Vectors;
using ParticleSimulator.Simulation.Particles;

namespace ParticleSimulator.Simulation.Baryon {
	public class Galaxy : AParticleGroup<MatterClump> {
		public override float StartSpeedMax_Group_Angular => Parameters.GRAVITY_STARTING_SPEED_MAX_GROUP;
		public override float StartSpeedMax_Group_Rand => Parameters.GRAVITY_STARTING_SPEED_MAX_GROUP_RAND;
		public override float StartSpeedMax_Particle_Min => Parameters.GRAVITY_STARTING_SPEED_MAX_INTRAGROUP_RAND;
		public override float StartSpeedMax_Particle_Max => Parameters.GRAVITY_STARTING_SPEED_MAX_INTRAGROUP_RAND;

		public Galaxy(float r, float a) : base(r) {
			this.BulgeScalar = a;
			this.ParticleMass = this.NumParticles;
		}

		public readonly float BulgeScalar;

		public readonly float ParticleMass;

		protected override MatterClump NewParticle(Vector<float> position, Vector<float> velocity) {
			float chargeRange = Parameters.ELECTROSTATIC_MAX_CHARGE - Parameters.ELECTROSTATIC_MIN_CHARGE;
			return new MatterClump(this.ID, position, velocity) {
				Mass = 1f / this.NumParticles,
				Charge = Parameters.ELECTROSTATIC_MIN_CHARGE + chargeRange * (float)Program.Engine.Random.NextDouble(),
			};
		}

		//Plummer distribution
		//see https://articles.adsabs.harvard.edu/pdf/1974A%26A....37..183A
		protected override void ParticleAddPositionVelocity(MatterClump particle) {
			float rand = (float)Program.Engine.Random.NextDouble();
			float radius = this.Radius * this.BulgeScalar / MathF.Sqrt(MathF.Pow(rand, -2f/3f) - 1f);
			Vector<float> uniformUnitVector = VectorFunctions.New(VectorFunctions.RandomUnitVector_Spherical(Parameters.DIM, Program.Engine.Random).Select(x => (float)x));
			Vector<float> particleOffset = radius * uniformUnitVector;
			particle.Position += particleOffset;
			
			float bulgeRadius = this.Radius * this.BulgeScalar;
			float velocity = this.VelocitySampling(radius, bulgeRadius);
			uniformUnitVector = VectorFunctions.New(VectorFunctions.RandomUnitVector_Spherical(Parameters.DIM, Program.Engine.Random).Select(x => (float)x));
			particle.Velocity += velocity * uniformUnitVector;
		}

		private float VelocitySampling(float radius, float bulgeRadius) {
			float rand1 = (float)Program.Engine.Random.NextDouble(),
				rand2 = 0.1f*(float)Program.Engine.Random.NextDouble();
			while (rand2 > rand1*rand1*MathF.Pow(1f - rand1*rand1, 3.5f)) {
				rand1 = (float)Program.Engine.Random.NextDouble();
				rand2 = 0.1f*(float)Program.Engine.Random.NextDouble();
			}

			return rand1 * MathF.Sqrt(2)
				* MathF.Pow(bulgeRadius*bulgeRadius + radius*radius, -0.25f);
		}

		private Vector<float> DirectionUnitVector(Vector<float> offset) {
			if (Parameters.DIM == 1) {
				return VectorFunctions.New(Program.Engine.Random.NextDouble() < 0.5d ? -1f : 1f);
			} else {
				float angle = MathF.Atan2(offset[1], offset[0]);
				angle += 2f * MathF.PI
					* (0.25f//90 degree rotation
						+ (MathF.Pow((float)Program.Engine.Random.NextDouble(), Parameters.GRAVITY_ALIGNMENT_SKEW_POW)
							* Parameters.GRAVITY_ALIGNMENT_SKEW_RANGE_PCT / 100f));

				IEnumerable<float> rotation = new float[] {
					MathF.Cos(angle),
					MathF.Sin(angle) };
				if (Parameters.DIM > 2)
					rotation = rotation.Concat(Enumerable.Repeat(0f, Parameters.DIM - 2));
				else rotation = rotation.Take(Parameters.DIM);

				return VectorFunctions.New(rotation);
			}
		}
	}
}