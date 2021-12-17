namespace ParticleSimulator.Simulation {
	public class GravitationalForce<TParticle> : ABaryonForce<TParticle>
	where TParticle : ABaryonParticle<TParticle> {
		public override float ForceConstant => Parameters.GRAVITATIONAL_CONSTANT;
		public override bool IsAttractionForce => true;

		public override float GetInteractedPhysicalParameter(TParticle particle) {
			return particle.Mass;
		}

		public override BaryonCenter GetInteractedPhysicalParameter(MagicTree<TParticle> baryonTree) {
			return baryonTree.BaryCenter_Mass;
		}
	}
}