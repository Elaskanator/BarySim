using Generic.Vectors;

namespace ParticleSimulator.Simulation {
	public abstract class AInverseSquareForce {
		public abstract double ForceConstant { get; }
		public abstract bool IsAttractionForce { get; }

		public abstract double GetInteractedPhysicalParameter(AClassicalParticle particle);
		public abstract BaryonCenter GetInteractedPhysicalParameter(FarFieldQuadTree baryonTree);

		public virtual double[] ClampImpulse(double[] impulse, double distance, double smallerMass) {
			double
				val = impulse.Magnitude(),
				max = Parameters.PARTICLE_MAX_ACCEL * Parameters.TIME_SCALE / smallerMass;
			return val > max ? impulse.Multiply(max / val) : impulse;
		}

		public double[] ComputeImpulse(AClassicalParticle p1, AClassicalParticle p2) {
			double[] toOther = p2.LiveCoordinates.Subtract(p1.LiveCoordinates);
			double distance = toOther.Magnitude();

			if (distance < p1.Radius + p2.Radius)
				p1.Collisions.Enqueue(p2);

			double[] impulse = this.ComputeImpulse(toOther, distance, this.GetInteractedPhysicalParameter(p1), this.GetInteractedPhysicalParameter(p2));
			return this.ClampImpulse(impulse, distance, p1.Mass < p2.Mass ? p1.Mass : p2.Mass);
		}
		public double[] ComputeImpulse(FarFieldQuadTree p1, FarFieldQuadTree p2) {
			BaryonCenter c1 = this.GetInteractedPhysicalParameter(p1),
				c2 = this.GetInteractedPhysicalParameter(p2);
			if (c1.TotalWeight > 0d && c2.TotalWeight > 0d) {
				double[] toOther = c2.Coordinates.Subtract(c1.Coordinates);
				double distance = toOther.Magnitude();
				double[] impulse = this.ComputeImpulse(toOther, distance, c1.TotalWeight, c2.TotalWeight);
				impulse = this.ClampImpulse(impulse, distance, p1.BaryCenter_Mass.TotalWeight < p2.BaryCenter_Mass.TotalWeight ? p1.BaryCenter_Mass.TotalWeight : p2.BaryCenter_Mass.TotalWeight);
				return impulse;
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