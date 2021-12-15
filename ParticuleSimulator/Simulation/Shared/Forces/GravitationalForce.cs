namespace ParticleSimulator.Simulation {
	public class GravitationalForce<TParticle> : ABaryonForce<TParticle>
	where TParticle : ABaryonParticle<TParticle> {
		public override double ForceConstant => Parameters.GRAVITATIONAL_CONSTANT;
		public override bool IsAttractionForce => true;

		public override double GetInteractedPhysicalParameter(TParticle particle) {
			return particle.Mass;
		}

		public override BaryonCenter GetInteractedPhysicalParameter(FarFieldQuadTree<TParticle> baryonTree) {
			return baryonTree.BaryCenter_Mass;
		}
	}
}