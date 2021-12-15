namespace ParticleSimulator.Simulation {
	public class ElectrostaticForce<TParticle> : ABaryonForce<TParticle>
	where TParticle : ABaryonParticle<TParticle> {
		public override double ForceConstant => Parameters.ELECTROSTATIC_CONSTANT;
		public override bool IsAttractionForce => false;

		public override double GetInteractedPhysicalParameter(TParticle particle) {
			return particle.Charge;
		}

		public override BaryonCenter GetInteractedPhysicalParameter(FarFieldQuadTree<TParticle> baryonTree) {
			return baryonTree.BaryCenter_Charge;
		}
	}
}