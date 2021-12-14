using Generic.Vectors;

namespace ParticleSimulator.Simulation {
	public abstract class AForce {
		public abstract double ForceConstant { get; }
		public abstract bool IsAttractionForce { get; }

		public abstract double GetInteractedPhysicalParameter(AClassicalParticle particle);
		public abstract BaryonCenter GetInteractedPhysicalParameter(FarFieldQuadTree baryonTree);

		public double[] ComputeImpulse(double distance, double[] toOther, AClassicalParticle p1, AClassicalParticle p2, out bool collision) {
			collision = distance < p1.Radius + p2.Radius;
			return distance > Parameters.WORLD_EPSILON
				? this.ComputeImpulse(toOther, distance, this.GetInteractedPhysicalParameter(p1), this.GetInteractedPhysicalParameter(p2))
				:  new double[Parameters.DIM];
		}
		public double[] ComputeImpulse(FarFieldQuadTree p1, FarFieldQuadTree p2) {
			BaryonCenter c1 = this.GetInteractedPhysicalParameter(p1),
				c2 = this.GetInteractedPhysicalParameter(p2);
			if (c1.TotalWeight > 0d && c2.TotalWeight > 0d) {
				double[] toOther = c2.Coordinates.Subtract(c1.Coordinates);
				double distance = toOther.Magnitude();
				return this.ComputeImpulse(toOther, distance, c1.TotalWeight, c2.TotalWeight);
			} else return new double[Parameters.DIM];
		}
		
		private double[] ComputeImpulse(double[] toOther, double distance, double p1, double p2) {
			return toOther.Multiply(
				(IsAttractionForce ? 1d : -1d)
				* this.ForceConstant
				* p1 * p2
				/ distance / distance / distance);//third division normalizes direction vector
		}
	}
}