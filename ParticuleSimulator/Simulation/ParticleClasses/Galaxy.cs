using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Generic.Extensions;
using Generic.Vectors;

namespace ParticleSimulator.Simulation {
	public class Galaxy : AParticleGroup<MatterClump> {
		public override float StartSpeedMax_Group_Angular => Parameters.GRAVITY_STARTING_SPEED_MAX_GROUP;
		public override float StartSpeedMax_Group_Rand => Parameters.GRAVITY_STARTING_SPEED_MAX_GROUP_RAND;
		public override float StartSpeedMax_Particle_Angular => Parameters.GRAVITY_STARTING_SPEED_MAX_INTRAGROUP;
		public override float StartSpeedMax_Particle_Range => Parameters.GRAVITY_STARTING_SPEED_MAX_INTRAGROUP_RAND;

		public override float ComputeInitialSeparationRadius(IEnumerable<MatterClump> particles) =>
			Parameters.GRAVITY_INITIAL_SEPARATION_SCALER
			* MathF.Pow(particles.Sum(p => p.Mass) / Parameters.GRAVITY_RADIAL_DENSITY, 1f / Parameters.DIM);

		static Vector<float> asdf = new Vector<float>(new float[] { 0, 0, 0.02f, 0, 0, 0, 0, 0 });
		protected override MatterClump NewParticle(Vector<float> position, Vector<float> velocity) {
			float massRange = Parameters.GRAVITY_MAX_STARTING_MASS - Parameters.GRAVITY_MIN_STARTING_MASS,
				chargeRange = Parameters.ELECTROSTATIC_MAX_CHARGE - Parameters.ELECTROSTATIC_MIN_CHARGE;
			return new MatterClump() {
				GroupID = this.ID,
				Mass = Parameters.GRAVITY_MIN_STARTING_MASS + massRange * (float)Program.Random.NextDouble(),
				Charge = Parameters.ELECTROSTATIC_MIN_CHARGE + chargeRange * (float)Program.Random.NextDouble(),
				Position = position,
				Velocity = velocity + asdf*(float)Program.Random.NextDouble(),
			};
		}

		//protected override Vector<float> NewParticlePosition(Vector<float> center, float radius) {
		//	Vector<float> result = base.NewParticlePosition(new float[Parameters.DIM].Take(2).ToArray(), radius)
		//		.Concat(new float[2])
		//		.Take(Parameters.DIM)
		//		.ToArray()
		//		.Add(center);

		//	if (Parameters.DIM > 2) {
		//		float height =
		//			this.NumParticles
		//			* this.InitialSeparationRadius
		//			* MathF.Pow((float)Program.Random.NextDouble(), 2f)
		//			/ MathF.Pow(2f, Parameters.DIM - 1f);
				
		//		result = result.Add(
		//			new float[2].Concat(
		//				HyperspaceFunctions.RandomCoordinate_Spherical(height, Parameters.DIM - 2, Program.Random).Select(x => (float)x))
		//			.ToArray());
		//	}

		//	return result;
		//}
		protected override Vector<float> NewInitialDirection(Vector<float> center, Vector<float> position) {
			if (Parameters.DIM == 1) {
				return Vector<float>.Zero;
			} else {
				float angle = MathF.Atan2(position[1] - center[1], position[0] - center[0]);
				angle += 2f * MathF.PI
					* (0.25f//90 degree rotation
						+ (MathF.Pow((float)Program.Random.NextDouble(), Parameters.GRAVITY_ALIGNMENT_SKEW_POW)
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