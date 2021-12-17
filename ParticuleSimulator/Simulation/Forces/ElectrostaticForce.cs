namespace ParticleSimulator.Simulation {
	public class ElectrostaticForce<TParticle> : ABaryonForce<TParticle>
	where TParticle : ABaryonParticle<TParticle> {
		public override float ForceConstant => Parameters.ELECTROSTATIC_CONSTANT;
		public override bool IsAttractionForce => false;

		public override float GetInteractedPhysicalParameter(TParticle particle) {
			return particle.Charge;
		}

		public override BaryonCenter GetInteractedPhysicalParameter(MagicTree<TParticle> baryonTree) {
			return baryonTree.BaryCenter_Charge;
		}
	}
}