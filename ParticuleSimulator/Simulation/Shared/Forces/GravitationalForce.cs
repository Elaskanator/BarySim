namespace ParticleSimulator.Simulation {
	public class GravitationalForce : AForce {
		public override double ForceConstant => Parameters.GRAVITATIONAL_CONSTANT;
		public override bool IsAttractionForce => true;

		public override double GetInteractedPhysicalParameter(AClassicalParticle particle) {
			return particle.Mass;
		}

		public override BaryonCenter GetInteractedPhysicalParameter(FarFieldQuadTree baryonTree) {
			return baryonTree.BaryCenter_Mass;
		}
	}
}