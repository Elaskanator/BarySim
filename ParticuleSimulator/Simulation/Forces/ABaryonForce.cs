using System.Numerics;
using Generic.Vectors;

namespace ParticleSimulator.Simulation {
	public abstract class ABaryonForce<TParticle>
	where TParticle : ABaryonParticle<TParticle> {
		public abstract float ForceConstant { get; }
		public abstract bool IsAttractionForce { get; }

		public abstract float GetInteractedPhysicalParameter(TParticle particle);
		public abstract BaryonCenter GetInteractedPhysicalParameter(MagicTree<TParticle> baryonTree);

		public Vector<float> ComputeImpulse(float distance, Vector<float> toOther, TParticle p1, TParticle p2, out bool collision) {
			collision = distance < p1.Radius + p2.Radius;
			return distance > Parameters.WORLD_EPSILON
				? this.ComputeImpulse(toOther, distance, this.GetInteractedPhysicalParameter(p1), this.GetInteractedPhysicalParameter(p2))
				: Vector<float>.Zero;
		}
		public Vector<float> ComputeAsymmetricImpulse(MagicTree<TParticle> p1, MagicTree<TParticle> p2) {
			BaryonCenter c1 = this.GetInteractedPhysicalParameter(p1),
				c2 = this.GetInteractedPhysicalParameter(p2);
			if (c2.TotalWeight > Parameters.WORLD_EPSILON) {
				Vector<float> toOther = c2.Center.Current - c1.Center.Current;
				float distance = toOther.Magnitude();
				return distance > Parameters.WORLD_EPSILON
					? this.ComputeImpulse(toOther, distance, 1f, c2.TotalWeight)
					: Vector<float>.Zero;
			} else return Vector<float>.Zero;
		}
		
		private Vector<float> ComputeImpulse(Vector<float> toOther, float distance, float p1, float p2) {
			return toOther * (
				(IsAttractionForce ? 1f : -1f)
				* this.ForceConstant
				* p1 * p2
				/ distance / distance / distance);//third division normalizes direction vector
		}
	}
}