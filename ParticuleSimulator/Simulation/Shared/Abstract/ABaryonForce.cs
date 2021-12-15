using Generic.Vectors;

namespace ParticleSimulator.Simulation {
	public abstract class ABaryonForce<TParticle>
	where TParticle : ABaryonParticle<TParticle> {
		public abstract double ForceConstant { get; }
		public abstract bool IsAttractionForce { get; }

		public abstract double GetInteractedPhysicalParameter(TParticle particle);
		public abstract BaryonCenter GetInteractedPhysicalParameter(FarFieldQuadTree<TParticle> baryonTree);

		public double[] ComputeImpulse(double distance, double[] toOther, TParticle p1, TParticle p2, out bool collision) {
			collision = distance < p1.Radius + p2.Radius;
			return distance > Parameters.WORLD_EPSILON
				? this.ComputeImpulse(toOther, distance, this.GetInteractedPhysicalParameter(p1), this.GetInteractedPhysicalParameter(p2))
				: new double[Parameters.DIM];
		}
		public double[] ComputeAsymmetricImpulse(FarFieldQuadTree<TParticle> p1, FarFieldQuadTree<TParticle> p2) {
			BaryonCenter c1 = this.GetInteractedPhysicalParameter(p1),
				c2 = this.GetInteractedPhysicalParameter(p2);
			if (c2.TotalWeight > Parameters.WORLD_EPSILON) {
				double[] toOther = c2.Coordinates.Subtract(c1.Coordinates);
				double distance = toOther.Magnitude();
				return distance > Parameters.WORLD_EPSILON
					? this.ComputeImpulse(toOther, distance, 1d, c2.TotalWeight)
					: new double[Parameters.DIM];
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