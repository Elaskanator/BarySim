using System;

namespace ParticleSimulator.Simulation.Boids {
	public class Flock : AParticleGroup<Boid> {
		public Flock() : base() {
			this.Corruption = Parameters.BOIDS_PREDATOR_CHANCE_BIAS > 0d
				? Math.Pow(Program.Random.NextDouble(), 1d / Parameters.BOIDS_PREDATOR_CHANCE_BIAS)
				: 0d;
		}

		public readonly double Corruption;

		protected override double InitialSeparationRadius => Parameters.BOIDS_INITIAL_SEPARATION;

		protected override Boid NewParticle(double[] position, double[] velocity) {
			return new Boid(this.ID, position, velocity, this.Corruption);
		}
	}
}