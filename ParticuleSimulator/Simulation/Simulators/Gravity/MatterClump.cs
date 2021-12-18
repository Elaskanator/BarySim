using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Generic.Vectors;

namespace ParticleSimulator.Simulation.Gravity {
	public class MatterClump : ABaryonParticle<MatterClump> {
		public MatterClump(int groupID, Vector<float> position, Vector<float> velocity, float mass, float charge = 0f)
		: base(groupID, position, velocity, mass, charge) { }

		private float _radius = 0f;
		public override float Radius => this._radius;
		private float _luminosity = 0f;
		public override float Luminosity => this._luminosity;
		
		private float _mass = 0f;
		public override float Mass {
			get => this._mass;
			set {
				this._mass = value;
				this._density = ComputeDensity(value);
				this._radius = value / this._density / Parameters.GRAVITY_RADIAL_DENSITY;
				this._luminosity = MathF.Pow(value * Parameters.MASS_LUMINOSITY_SCALAR, 3.5f); }}

		private float _density = 1f / Parameters.GRAVITY_RADIAL_DENSITY;
		public override float Density => this._density;
		public static float ComputeDensity(float mass) => 1f + MathF.Pow(mass, (1f / Parameters.DIM) - 0.5f);
		
		public override Vector<float> CollisionAcceleration {
			get => this.CollisionImpulse * (1f / this.Mass);
			set { this.CollisionImpulse = value * this.Mass; } }

		public override bool CollideCombine(float distance, Vector<float> toOther, MatterClump other, ref float strength) {
			Vector<float> baryCenter =
				(this.Mass * this.Position
				+ other.Mass * other.Position)
				* (1f / (this.Mass + other.Mass));
			
			float distanceError = other.Position.Distance(baryCenter) / (this.Radius + other.Radius);
			if (distance <= this.Radius - other.Radius || distance <= other.Radius - this.Radius
			|| distanceError <= Parameters.GRAVITY_COMBINE_OVERLAP_CUTOFF_BARYON_ERROR) {
				this.Mass += other.Mass;
				this.Charge += other.Charge;
				this.Position = baryCenter;
				this.Momentum = this.Momentum + other.Momentum;
				this.NearfieldImpulse = this.NearfieldImpulse + other.NearfieldImpulse;
				this.FarfieldImpulse = this.FarfieldImpulse + other.FarfieldImpulse;
				this.CollisionImpulse = this.CollisionImpulse + other.CollisionImpulse;

				other.Momentum = Vector<float>.Zero;
				other.NearfieldImpulse = Vector<float>.Zero;
				other.FarfieldImpulse = Vector<float>.Zero;
				other.CollisionImpulse = Vector<float>.Zero;
				other.IsEnabled = false;
				return true;
			}

			return false;
		}

		protected override IEnumerable<MatterClump> AfterUpdate() {
			if (this.Mass >= Parameters.GRAVITY_CRITICAL_MASS) {//supernova!
				this.IsEnabled = false;

				float radiusRange = this.Radius * (1 << (1 + Parameters.DIM));
				float avgMass = this.Mass / Parameters.GRAVITY_EJECTA_NUM_PARTICLES;
				float avgCharge = this.Charge / Parameters.GRAVITY_EJECTA_NUM_PARTICLES;
				for (int i = 0; i < Parameters.GRAVITY_EJECTA_NUM_PARTICLES; i++) {
					yield return new MatterClump(
						this.GroupID,
						this.Position + VectorFunctions.New(VectorFunctions.RandomCoordinate_Spherical(radiusRange, Parameters.DIM, Program.Random).Select(x => (float)x)),
						VectorFunctions.New(VectorFunctions.RandomUnitVector_Spherical(Parameters.DIM, Program.Random).Select(x => (float)x * Parameters.GRAVITY_EJECTA_SPEED)),
						avgMass,
						avgCharge);
				}
			} else yield return this;
		}
		
		//public override float ComputeCollision(float distance, Vector<float> toOther, MatterClump other) {
		//	if (Parameters.GRAVITY_COLLISION_DRAG_STRENGTH > 0f && distance > Parameters.WORLD_EPSILON) {
		//		throw new NotImplementedException();
		//		Vector<float> velocityDelta = other.Velocity.Subtract(this.Velocity);
		//		double velocityDeltaSize = velocityDelta.Magnitude();
		//		if (velocityDeltaSize > Parameters.WORLD_EPSILON) {
		//			double alignedImpulse = velocityDelta.Divide(velocityDeltaSize).DotProduct(toOther.Divide(distance));
		//			Vector<float> impulse =
		//				toOther.Normalize()
		//					.Multiply(this.Momentum.Magnitude() * alignedImpulse * Parameters.GRAVITY_COLLISION_DRAG_STRENGTH);
		//			if (distance <= this.Radius + other.Radius) {
		//				this.CollisionImpulse = this.CollisionImpulse.Add(impulse);
		//				other.CollisionImpulse = other.CollisionImpulse.Subtract(impulse);
		//			}
		//			return impulse.Magnitude() / (this.Mass < other.Mass ? this.Mass : other.Mass) / distance;
		//		}
		//	}
		//	return 0f;
		//}
	}
}