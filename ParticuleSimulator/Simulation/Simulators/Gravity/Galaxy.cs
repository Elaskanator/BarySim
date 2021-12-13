using System;
using System.Collections.Generic;
using System.Linq;
using Generic.Vectors;

namespace ParticleSimulator.Simulation.Gravity {
	public class Galaxy : AParticleGroup {
		public override double InitialSeparationRadius => Parameters.GRAVITY_INITIAL_SEPARATION;
		public override double StartSpeedMax_Group_Angular => Parameters.GRAVITY_STARTING_SPEED_MAX_GROUP;
		public override double StartSpeedMax_Group_Rand => Parameters.GRAVITY_STARTING_SPEED_MAX_GROUP_RAND;
		public override double StartSpeedMax_Particle_Angular => Parameters.GRAVITY_STARTING_SPEED_MAX_INTRAGROUP;
		public override double StartSpeedMax_Particle_Range => Parameters.GRAVITY_STARTING_SPEED_MAX_INTRAGROUP_RAND;

		protected override MatterClump NewParticle(double[] position, double[] velocity) {
			return new MatterClump(this.ID,
				position,
				velocity,
				Parameters.GRAVITY_MIN_MASS
					+ (Math.Pow(Program.Random.NextDouble(), Parameters.GRAVITY_MASS_BIAS)
						* (Parameters.GRAVITY_MAX_MASS - Parameters.GRAVITY_MIN_MASS)),
				Parameters.ELECTROSTATIC_MIN_CHARGE
					+ Program.Random.NextDouble()
						* (Parameters.ELECTROSTATIC_MAX_CHARGE - Parameters.ELECTROSTATIC_MIN_CHARGE));
		}

		protected override double[] NewParticlePosition(double[] center, double radius) {
			double[] result = base
				.NewParticlePosition(new double[Parameters.DIM].Take(2).ToArray(), radius)
				.Concat(new double[2])
				.Take(Parameters.DIM)
				.ToArray()
				.Add(center);

			if (Parameters.DIM > 2) {
				double height =
					this.NumParticles
					* this.InitialSeparationRadius
					* Math.Pow(Program.Random.NextDouble(), 2d)
					/ Math.Pow(2d, Parameters.DIM - 1d);
				
				result = result.Add(
					new double[2].Concat(
						HyperspaceFunctions.RandomCoordinate_Spherical(height, Parameters.DIM - 2, Program.Random)).ToArray());
			}

			return result;
		}
		protected override double[] NewInitialDirection(double[] center, double[] position) {
			if (Parameters.DIM == 1) {
				return new double[Parameters.DIM];
			} else {
				double angle = Math.Atan2(position[1] - center[1], position[0] - center[0]);
				angle += 2 * Math.PI
					* (0.25d
						+ (Math.Pow(Program.Random.NextDouble(), Parameters.GRAVITY_ALIGNMENT_SKEW_POW)
							* Parameters.GRAVITY_ALIGNMENT_SKEW_RANGE_PCT / 100d));

				IEnumerable<double> rotation = new double[] {
					Math.Cos(angle),
					Math.Sin(angle)};
				if (Parameters.DIM > 2)
					rotation = rotation.Concat(Enumerable.Repeat(0d, Parameters.DIM - 2));
				return rotation.ToArray().Multiply(Math.Log(1d + center.Distance(position), Parameters.GRAVITY_INITIAL_SEPARATION));
			}
		}
	}
}