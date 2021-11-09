using System;
using Generic.Models;

namespace ParticleSimulator.Simulation.Boids {
	public class Flock : AParticleGroup<Boid> {
		public double Separation = Parameters.DEFAULT_SEPARATION;
		public double SeparationWeight = Parameters.DEFAULT_SEPARATION_WEIGHT;
		public double AlignmentWeight = Parameters.DEFAULT_ALIGNMENT_WEIGHT;
		public double CohesionWeight = Parameters.DEFAULT_COHESION_WEIGHT;
		public double SpeedDecay = Math.Exp(Parameters.DEFAULT_SPEED_DECAY);

		public Flock(Random rand) : base(rand) { }

		public override Boid NewParticle(SimpleVector position, SimpleVector velocity, Random random) {
			return new Boid(this, position, velocity);
		}
	}
}