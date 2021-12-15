using Generic.Models;
using Generic.Vectors;

namespace ParticleSimulator.Simulation.Gravity {
	public class GravitySimulator : ABaryonParticleSimulator<MatterClump> {
		public GravitySimulator()
		: base(new GravitationalForce<MatterClump>(), new ElectrostaticForce<MatterClump>()) { }

		public override bool EnableCollisions => true;

		protected override AParticleGroup<MatterClump> NewParticleGroup() { return new Galaxy(); }
		protected override ATree<MatterClump> NewTree(double[] leftCorner, double[] rightCorner) { return new FarFieldQuadTree<MatterClump>(leftCorner, rightCorner); }

		protected override bool DoCombine(double distance, MatterClump smaller, MatterClump larger) {
			return Parameters.GRAVITY_COLLISION_COMBINE
				&& (distance <= Parameters.WORLD_EPSILON
					|| distance <= larger.Radius + smaller.Radius * (1d - Parameters.GRAVITY_COMBINE_OVERLAP_CUTOFF));
		}

		//TODO
		protected override double[] ComputeCollisionAcceleration(double distance, double[] toOther, MatterClump smaller, MatterClump larger) {
			if (Parameters.GRAVITY_COLLISION_DRAG_STRENGTH > 0d) {
				return new double[Parameters.DIM];
				//double overlapRange = this.Radius + other.Radius - larger.Radius;
				//double[] dragForce =
				//	other.Velocity
				//		.Subtract(this.Velocity)
				//		.Multiply(Parameters.GRAVITY_COLLISION_DRAG_STRENGTH * smaller.Radius * (distance - larger.Radius) / overlapRange);
				////do not include in result, apply directly
				//this.NetForce = this.NetForce.Add(dragForce);
				//other.NetForce = other.NetForce.Subtract(dragForce);
			} else return new double[Parameters.DIM];
		}
	}
}