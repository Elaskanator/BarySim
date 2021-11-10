using System;

namespace ParticleSimulator.Simulation.Boids {
	public class Flock : AParticleGroup<Boid> {
		public Flock(Random rand) : base(rand) { }

		public override Boid NewParticle(double[] position, double[] velocity, Random random) {
			return new Boid(this, position, velocity);
		}
	}
}