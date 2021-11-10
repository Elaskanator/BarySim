using System;

namespace ParticleSimulator.Simulation.Gravity {
	public class Clustering : AParticleGroup<double, CelestialBody> {
		public Clustering(Random rand) : base(rand) { }

		public override CelestialBody NewParticle(double[] position, double[] velocity, Random random) {
			return new CelestialBody(position, velocity, Parameters.DEFAULT_MIN_MASS + (random.NextDouble() * (Parameters.DEFAULT_MAX_MASS - Parameters.DEFAULT_MIN_MASS)));
		}
	}
}