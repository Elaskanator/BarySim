namespace ParticleSimulator.Simulation {
	public abstract class AForce {
		public abstract PhysicalAttribute InteractedPhysicalAttribute { get; }

		public abstract double[] ComputeImpulse(AClassicalParticle p1, AClassicalParticle p2);
		public abstract double[] ComputeImpulse(FarFieldQuadTree n1, FarFieldQuadTree n2);
	}
}