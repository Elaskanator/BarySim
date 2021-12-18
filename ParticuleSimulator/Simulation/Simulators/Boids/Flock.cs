using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace ParticleSimulator.Simulation.Boids {
	public class Flock : AParticleGroup<Boid> {
		public Flock() : base() {
			this.Corruption = Parameters.BOIDS_PREDATOR_CHANCE_BIAS > 0f
				? MathF.Pow((float)Program.Random.NextDouble(), Parameters.BOIDS_PREDATOR_CHANCE_BIAS)
				: 0f;
		}

		public readonly float Corruption;

		public override float StartSpeedMax_Group_Angular => Parameters.BOIDS_STARTING_SPEED_MAX_GROUP;
		public override float StartSpeedMax_Group_Rand => Parameters.BOIDS_STARTING_SPEED_MAX_GROUP_RAND;
		public override float StartSpeedMax_Particle_Angular => Parameters.BOIDS_STARTING_SPEED_MAX_INTRAGROUP;
		public override float StartSpeedMax_Particle_Range => Parameters.BOIDS_STARTING_SPEED_MAX_INTRAGROUP_RAND;

		public override float ComputeInitialSeparationRadius(IEnumerable<Boid> particles) =>
			particles.Count() * Parameters.BOIDS_INITIAL_SEPARATION;

		protected override Boid NewParticle(Vector<float> position, Vector<float> velocity) {
			return new Boid(this.ID, position, velocity, this.Corruption);
		}
	}
}