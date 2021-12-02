using System;
using Generic.Extensions;
using Generic.Models.Vectors;

namespace ParticleSimulator.Simulation.Boids {
	public class Flock : AParticleGroup<Boid> {
		public Flock() : base() {
			this.Corruption = Parameters.BOIDS_PREDATOR_CHANCE_BIAS > 0.000001d
				? Math.Pow(Program.Random.NextDouble(), 1d / Parameters.BOIDS_PREDATOR_CHANCE_BIAS)
				: 0d;
		}

		public readonly double Corruption;
		protected override double InitialSeparation => Parameters.BOIDS_INITIAL_SEPARATION;

		public override Boid NewParticle(double[] position, double[] groupVelocity) {
			return new Boid(this.ID, position,
				groupVelocity.Add(NumberExtensions.RandomUnitVector_Spherical(Parameters.DIM, Program.Random).Multiply(Parameters.MAX_STARTING_SPEED)),
				this.Corruption * Parameters.BOIDS_PREDATOR_CHANCE);
		}
	}
}