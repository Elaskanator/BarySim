using System;

namespace ParticleSimulator.Simulation.Boids {
	public class Flock : AParticleGroup<Boid> {
		public Flock() : base() {
			this.Corruption = Parameters.BOIDS_PREDATOR_CHANCE_BIAS > 0d
				? Math.Pow(Program.Random.NextDouble(), Parameters.BOIDS_PREDATOR_CHANCE_BIAS)
				: 0d;
		}

		public readonly double Corruption;

		public override double InitialSeparationRadius => Parameters.BOIDS_INITIAL_SEPARATION;
		public override double StartSpeedMax_Group_Angular => Parameters.BOIDS_STARTING_SPEED_MAX_GROUP;
		public override double StartSpeedMax_Group_Rand => Parameters.BOIDS_STARTING_SPEED_MAX_GROUP_RAND;
		public override double StartSpeedMax_Particle_Angular => Parameters.BOIDS_STARTING_SPEED_MAX_INTRAGROUP;
		public override double StartSpeedMax_Particle_Range => Parameters.BOIDS_STARTING_SPEED_MAX_INTRAGROUP_RAND;

		protected override Boid NewParticle(double[] position, double[] velocity) {
			return new Boid(this.ID, position, velocity, this.Corruption);
		}
	}
}