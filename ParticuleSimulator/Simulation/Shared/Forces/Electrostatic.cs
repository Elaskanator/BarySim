using Generic.Vectors;

namespace ParticleSimulator.Simulation {
	public class ElectrostaticForce : AForce {
		public override PhysicalAttribute InteractedPhysicalAttribute => PhysicalAttribute.Charge;

		public double[] ComputeImpulse(double[] position1, double charge1, double[] position2, double charge2) {
			double[] away = position1.Subtract(position2);
			double distance = away.Magnitude();
			if (distance > Parameters.WORLD_EPSILON)//third division by distance to normalize direction vector
				return away.Multiply(Parameters.ELECTROSTATIC_CONSTANT * charge1 * charge2 / distance / distance / distance);
			else return new double[Parameters.DIM];
		}

		public override double[] ComputeImpulse(AClassicalParticle p1, AClassicalParticle p2) {
			return this.ComputeImpulse(
				p1.LiveCoordinates,
				p1.Charge,
				p2.LiveCoordinates,
				p2.Charge);
		}

		public override double[] ComputeImpulse(FarFieldQuadTree n1, FarFieldQuadTree n2) {
			return this.ComputeImpulse(
				n1.BaryCenter[PhysicalAttribute.Charge].Current,
				n1.BaryCenter[PhysicalAttribute.Charge].TotalWeight,
				n2.BaryCenter[PhysicalAttribute.Charge].Current,
				n2.BaryCenter[PhysicalAttribute.Charge].TotalWeight);
		}
	}
}