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
		public override float StartSpeedMax_Particle_Angular => Parameters.GRAVITY_STARTING_SPEED_MAX_INTRAGROUP;
		public override float StartSpeedMax_Particle_Min => Parameters.GRAVITY_STARTING_SPEED_MAX_INTRAGROUP_RAND;
		public override float StartSpeedMax_Particle_Max => Parameters.GRAVITY_STARTING_SPEED_MAX_INTRAGROUP_RAND;

		public override float ComputeInitialSeparationRadius(IEnumerable<MatterClump> particles) =>
			Parameters.INITIAL_SEPARATION_SCALER
			* (float)VectorFunctions.HypersphereRadius(particles.Sum(p => p.Mass)
			/ Parameters.GRAVITY_RADIAL_DENSITY, Parameters.DIM);

		protected override MatterClump NewParticle(Vector<float> position, Vector<float> velocity) {
			float massRange = Parameters.GRAVITY_MAX_STARTING_MASS - Parameters.GRAVITY_MIN_STARTING_MASS,
				chargeRange = Parameters.ELECTROSTATIC_MAX_CHARGE - Parameters.ELECTROSTATIC_MIN_CHARGE;
			return new MatterClump(this.ID, position, velocity) {
				Mass = Parameters.GRAVITY_MIN_STARTING_MASS + massRange * (float)Program.Engine.Random.NextDouble(),
				Charge = Parameters.ELECTROSTATIC_MIN_CHARGE + chargeRange * (float)Program.Engine.Random.NextDouble(),
			};
		}

		protected override Vector<float> NewInitialDirection(Vector<float> center, Vector<float> position) {
			if (Parameters.DIM == 1) {
				return VectorFunctions.New(Program.Engine.Random.NextDouble() < 0.5f ? -1f : 1f);
			} else {
				float angle = MathF.Atan2(position[1] - center[1], position[0] - center[0]);
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