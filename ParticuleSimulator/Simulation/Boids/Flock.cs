using System;

namespace ParticleSimulator.Simulation.Boids {
	public class Flock : AParticleGroup<Boid> {
		public readonly double Corruption;
		public Flock() : base() {
			this.Corruption = Parameters.BOIDS_PREDATOR_CHANCE_BIAS > 0.000001d
				? Math.Pow(Program.Random.NextDouble(), 1d / Parameters.BOIDS_PREDATOR_CHANCE_BIAS)
				: 0d;
		}

		public override Boid NewParticle(double[] position, double[] velocity, Random random) {
			return new Boid(this.ID, position, velocity, this.Corruption);
		}
	}
}