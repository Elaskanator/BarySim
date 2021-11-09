using Generic.Models;
using System;

namespace ParticleSimulator.Simulation.Gravity {
	public class Clustering : AParticleGroup<CelestialBody> {
		public Clustering(Random rand) : base(rand) { }

		public override CelestialBody NewParticle(SimpleVector position, SimpleVector velocity, Random random) {
			return new CelestialBody(position, velocity, random.NextDouble() * Parameters.DEFAULT_MAX_MASS);
		}
	}
}