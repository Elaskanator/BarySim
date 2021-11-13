using System;
using System.Linq;

namespace ParticleSimulator.Simulation.Boids {
	public class BoidSimulator : AParticleSimulator<Boid, Flock> {
		public BoidSimulator() : base() { }

		public override bool IsDiscrete => true;
		public override int InteractionLimit => Parameters.DESIRED_INTERACTION_NEIGHBORS;
		public override Flock NewParticleGroup() { return new Flock(); }

		public override ConsoleColor ChooseGroupColor(AParticle[] others) {
			return others.Cast<Boid>().Any(p => p.IsPredator)
				? Parameters.BOIDS_PREDATOR_COLOR
				: base.ChooseGroupColor(others);
		}
	}
}