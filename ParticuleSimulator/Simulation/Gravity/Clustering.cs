using System;

namespace ParticleSimulator.Simulation.Gravity {
	public class Clustering : AParticleGroup<CelestialBody> {
		public Clustering(Random rand) : base(rand) { }

		public override CelestialBody NewParticle(double[] position, double[] velocity, Random random) {
			return new CelestialBody(this.ID, position, velocity, Parameters.MIN_MASS + (random.NextDouble() * (Parameters.MAX_MASS - Parameters.MIN_MASS)));
		}
	}
}