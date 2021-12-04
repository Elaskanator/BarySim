using System;
using System.Collections.Generic;
using System.Linq;
using Generic.Vectors;

namespace ParticleSimulator.Simulation.Gravity {
	public class Galaxy : AParticleGroup<MatterClump> {
		protected override double InitialSeparationRadius => Parameters.GRAVITY_INITIAL_SEPARATION;

		protected override MatterClump NewParticle(double[] position, double[] velocity) {
			return new MatterClump(this.ID,
				position,
				velocity,
				Parameters.GRAVITY_MIN_MASS
					+ (Math.Pow(Program.Random.NextDouble(), Parameters.GRAVITY_MASS_BIAS)
						* (Parameters.GRAVITY_MAX_MASS - Parameters.GRAVITY_MIN_MASS)));
		}

		protected override double[] NewParticlePosition(double[] center, double radius) {
			double[] result = base
				.NewParticlePosition(new double[Parameters.DIM].Take(2).ToArray(), radius * Program.Random.NextDouble())//cluster more in the center
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
						+ ( Math.Pow(Program.Random.NextDouble(), Parameters.GRAVITY_ALIGNMENT_SKEW_POW)
							* Parameters.GRAVITY_ALIGNMENT_SKEW_RANGE_PCT / 100d));

				IEnumerable<double> rotation = new double[] {
					Math.Cos(angle),
					Math.Sin(angle)};
				if (Parameters.DIM > 2)
					rotation = rotation.Concat(Enumerable.Repeat(0d, Parameters.DIM - 2));
				return rotation.ToArray().Multiply(Math.Log(1d + center.Distance(position), Parameters.GRAVITY_INITIAL_SEPARATION));
			}
		}

		//public Galaxy() {
			//while ((this.OrbitalPlane = Enumerable
			//	.Range(0, Parameters.DIM - 1)
			//	.Select(n => HyperspaceFunctions
			//		.RandomUnitVector_Spherical(Parameters.DIM, Program.Random)
			//		.ToArray())
			//	.ToArray())
			//	.Determinant() == 0d);//cannot be linearly dependent
			//this.OrbitalPlane = this.OrbitalPlane.Orthonormalize();
			//this.OrbitalPlaneNormal = this.OrbitalPlane.NewNormalVector();
			//this.OrbitalPlaneNormal = HyperspaceFunctions.RandomUnitVector_Spherical(Parameters.DIM, Program.Random);
		//}

		//public readonly double[][] OrbitalPlane;
		//public readonly double[] OrbitalPlaneNormal;
		//public readonly double[] OrbitalPlaneDiagonal;
		
		/*
		protected override double[] NewInitialPosition(double radius) {
			return this.SpawnCenter.Add(
				HyperspaceFunctions.RandomCoordinate_Spherical(
					radius * Program.Random.NextDouble(), Parameters.DIM, Program.Random));//cluster more in the center
			//result = base.NewInitialPosition(radius);
			//if (Parameters.DIM > 2) {//does work for 2 but does nothing
			//	double range = result.Distance(this.SpawnCenter);
			//	result = result.Add(//random "height" from the orbital plane
			//		this.OrbitalPlaneNormal.Multiply(
			//			Math.Pow(Program.Random.NextDouble() * (1d - range/radius), 2d)
			//			* this.InitialSeparationRadius
			//			/ Math.Pow(2d, Parameters.DIM - 1d)));
			//}
		}
		protected override double[] NewInitialDirection(double[] center, double[] position) {
			double dist = center.Distance(position);
			if (dist <= Parameters.WORLD_EPSILON) {
				return new double[Parameters.DIM];
			} else {
				//double perpendicularity = 1d - position.Subtract(center).Normalize().DotProduct(this.OrbitalPlaneNormal);
				//double[]
				//	projected = ,
				//	fromCenter = projected.Subtract(center);
			}
		}
		*/
	}
}