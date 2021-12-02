using System;
using Generic.Extensions;
using Generic.Models.Vectors;

namespace ParticleSimulator.Simulation.Gravity {
	public class Clustering : AParticleGroup<CelestialBody> {
		public override CelestialBody NewParticle(double[] position, double[] groupVelocity) {
			return new CelestialBody(this.ID,
				position,
				groupVelocity
					.Add(NumberExtensions
						.RandomUnitVector_Spherical(Parameters.DIM, Program.Random)
						.Multiply(Parameters.MAX_STARTING_SPEED)),
				Parameters.GRAVITY_MIN_MASS
					+ (Math.Pow(Program.Random.NextDouble(), Parameters.GRAVITY_MASS_BIAS)
						* (Parameters.GRAVITY_MAX_MASS - Parameters.GRAVITY_MIN_MASS)));
		}
	}
}