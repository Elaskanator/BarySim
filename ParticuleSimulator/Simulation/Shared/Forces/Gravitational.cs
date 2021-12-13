using Generic.Vectors;

namespace ParticleSimulator.Simulation {
	public class GravitationalForce : AForce {
		public override PhysicalAttribute InteractedPhysicalAttribute => PhysicalAttribute.Mass;

		public double[] ComputeImpulse(double[] position1, double mass1, double[] position2, double mass2) {
			double[] toOther = position2.Subtract(position1);
			double distance = toOther.Magnitude();
			if (distance > Parameters.WORLD_EPSILON)//third division by distance to normalize direction vector
				return toOther.Multiply(Parameters.GRAVITATIONAL_CONSTANT * mass1 * mass2 / distance / distance / distance);
			else return new double[Parameters.DIM];
		}

		public override double[] ComputeImpulse(AClassicalParticle p1, AClassicalParticle p2) {
			return this.ComputeImpulse(
				p1.LiveCoordinates,
				p1.Mass,
				p2.LiveCoordinates,
				p2.Mass);
		}

		public override double[] ComputeImpulse(FarFieldQuadTree n1, FarFieldQuadTree n2) {
			return this.ComputeImpulse(
				n1.BaryCenter[PhysicalAttribute.Mass].Current,
				n1.BaryCenter[PhysicalAttribute.Mass].TotalWeight,
				n2.BaryCenter[PhysicalAttribute.Mass].Current,
				n2.BaryCenter[PhysicalAttribute.Mass].TotalWeight);
		}
	}
}