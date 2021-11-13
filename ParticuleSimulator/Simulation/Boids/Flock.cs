using System;

namespace ParticleSimulator.Simulation.Boids {
	public class Flock : AParticleGroup<Boid> {
		public Flock(double[] center, Random rand) : base(center, rand) { }

		public override Boid NewParticle(double[] position, double[] velocity, Random random) {
			return new Boid(this.ID, position, velocity);
		}
	}
}