namespace ParticleSimulator.Simulation {
	public class ElectrostaticForce : AInverseSquareForce {
		public override double ForceConstant => Parameters.ELECTROSTATIC_CONSTANT;
		public override bool IsAttractionForce => false;

		public override double GetInteractedPhysicalParameter(AClassicalParticle particle) {
			return particle.Charge;
		}

		public override BaryonCenter GetInteractedPhysicalParameter(FarFieldQuadTree baryonTree) {
			return baryonTree.BaryCenter_Charge;
		}
	}
}