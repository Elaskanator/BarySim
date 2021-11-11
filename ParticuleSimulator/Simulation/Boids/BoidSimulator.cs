using System;

namespace ParticleSimulator.Simulation.Boids {
	public class BoidSimulator : AParticleSimulator<Boid, Flock, BoidQuadTree> {
		public BoidSimulator(Random rand = null) : base(rand) { }

		public override bool IsDiscrete => true;
		public override int? InteractionLimit => Parameters.DESIRED_NEIGHBORS;
		public override BoidQuadTree NewTree => new BoidQuadTree(new double[Parameters.DOMAIN.Length], Parameters.DOMAIN);
		public override Flock NewParticleGroup(Random rand) { return new Flock(rand); }
	}
}