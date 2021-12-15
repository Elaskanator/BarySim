using System;
using System.Collections.Generic;
using Generic.Extensions;
using Generic.Vectors;

namespace ParticleSimulator.Simulation.Gravity {
	public class MatterClump : ABaryonParticle<MatterClump> {
		public MatterClump(int groupID, double[] position, double[] velocity, double mass, double charge = 0d)
		: base(groupID, position, velocity, mass, charge) { }

		private double _density = 1d;
		public override double Density => this._density;
		private double _radius = 0d;
		public override double Radius => this._radius;
		private double _luminosity = 0d;
		public override double Luminosity => this._luminosity;
		private double _mass = 0d;
		public override double Mass {
			get => this._mass;
			set {
				this._mass = value;
				this._radius = Math.Pow(value, 1d / Parameters.DIM) / Parameters.GRAVITY_RADIAL_DENSITY;
				this._density = Math.Pow(this._radius, 1d / Parameters.DIM);
				this._radius /= this._density;
				this._luminosity = Math.Pow(value * Parameters.MASS_LUMINOSITY_SCALAR, 3.5d); }}
		public override double[] CollisionAcceleration {
			get => this.CollisionImpulse.Divide(this.Mass);
			set { this.CollisionImpulse = value.Multiply(this.Mass); } }

		public override bool Absorb(double distance, double[] toOther, MatterClump other) {
			double[] baryCenter =
				this.LiveCoordinates.Multiply(this.Mass)
				.Add(other.LiveCoordinates.Multiply(other.Mass))
				.Divide(this.Mass + other.Mass);
			
			double distanceError =
				(other.LiveCoordinates.Distance(baryCenter) / (this.Radius + other.Radius))
				//* (other.Momentum.Distance(this.Momentum) / (this.Momentum.Magnitude() + other.Momentum.Magnitude()))
				;
			if (distance <= this.Radius - other.Radius || distance <= other.Radius - this.Radius
			|| distanceError <= Parameters.GRAVITY_COMBINE_OVERLAP_CUTOFF_BARYON_ERROR) {
				//this._density =
				//	((this._density * this._mass) + (other._density * other._mass))
				//	/ (this.Mass + other.Mass);
				this.Mass += other.Mass;
				this.Charge += other.Charge;
				this.LiveCoordinates = baryCenter;
				this.Momentum = this.Momentum.Add(other.Momentum);
				this.NearfieldImpulse = this.NearfieldImpulse.Add(other.NearfieldImpulse);
				this.FarfieldImpulse = this.FarfieldImpulse.Add(other.FarfieldImpulse);
				this.CollisionImpulse = this.CollisionImpulse.Add(other.CollisionImpulse);

				other.Momentum = new double[Parameters.DIM];
				other.NearfieldImpulse = new double[Parameters.DIM];
				other.FarfieldImpulse = new double[Parameters.DIM];
				other.CollisionImpulse = new double[Parameters.DIM];
				other.Enabled = false;
				return true;
			}

			return false;
		}
		
		public override double ComputeCollision(double distance, double[] toOther, MatterClump other) {
			//if (Parameters.GRAVITY_COLLISION_DRAG_STRENGTH > 0d && distance > Parameters.WORLD_EPSILON) {
			//	throw new NotImplementedException();
			//	double[] velocityDelta = other.Velocity.Subtract(this.Velocity);
			//	double velocityDeltaSize = velocityDelta.Magnitude();
			//	if (velocityDeltaSize > Parameters.WORLD_EPSILON) {
			//		double alignedImpulse = velocityDelta.Divide(velocityDeltaSize).DotProduct(toOther.Divide(distance));
			//		double[] impulse =
			//			toOther.Normalize()
			//				.Multiply(this.Momentum.Magnitude() * alignedImpulse * Parameters.GRAVITY_COLLISION_DRAG_STRENGTH);
			//		if (distance <= this.Radius + other.Radius) {
			//			this.CollisionImpulse = this.CollisionImpulse.Add(impulse);
			//			other.CollisionImpulse = other.CollisionImpulse.Subtract(impulse);
			//		}
			//		return impulse.Magnitude() / (this.Mass < other.Mass ? this.Mass : other.Mass) / distance;
			//	}
			//}
			return 0d;
		}

		protected override void AfterUpdate() {
			if (this.Mass >= Parameters.GRAVITY_CRITICAL_MASS) {//supernova!
				HashSet<MatterClump> memberParticles = this.DistinctRecursiveChildren(p => p.MergedParticles);

				double totalMass = this.Mass;
				double totalCharge = this.Charge;
				double density = this.Density;
				double excessMass = totalMass - Parameters.GRAVITY_CRITICAL_MASS;
				double intensityFraction = excessMass / Parameters.GRAVITY_CRITICAL_MASS;
				double velocity;
				if (Parameters.GRAVITY_EXPLOSION_SPEED_LOW_BIAS > 0)
					velocity = Parameters.GRAVITY_EXPLOSION_MIN_SPEED
						+ (Parameters.GRAVITY_EXPLOSION_MAX_SPEED - Parameters.GRAVITY_EXPLOSION_MIN_SPEED)
							* (1d - (1d / (1d + (1d / Math.Log(1d + intensityFraction, 1d + Parameters.GRAVITY_EXPLOSION_SPEED_LOW_BIAS + 1)))));//don't ask...
				else velocity = Parameters.GRAVITY_EXPLOSION_MAX_SPEED;
				double[] centerOfMass = (double[])this.LiveCoordinates.Clone();
				double avgMass = totalMass / memberParticles.Count;
				double avgCharge = totalCharge / memberParticles.Count;
				double[] avgMomentum = this.Momentum.Divide(memberParticles.Count);

				this.Mass = avgMass;
				double valenceRadius = 3d * this.Radius;
				int valenceNum = 0, valenceSize = 0, valenceCapacity = 1;
				double[] direction;
				foreach (MatterClump shrapnel in memberParticles) {
					shrapnel.MergedParticles.Clear();
					shrapnel.NodeCollisions.Clear();
					direction = HyperspaceFunctions.RandomUnitVector_Spherical(Parameters.DIM, Program.Random);

					shrapnel.Mass = avgMass;
					//shrapnel._density = density;
					shrapnel.Charge = avgCharge;
					shrapnel.LiveCoordinates = centerOfMass.Add(direction.Multiply(valenceNum * valenceRadius));
					shrapnel.Velocity = valenceNum == 0 && valenceCapacity == 1
						? new double[Parameters.DIM]
						: direction.Multiply(velocity * Math.Pow(valenceNum, 1.1d));
					shrapnel.Momentum = shrapnel.Momentum.Add(avgMomentum);
					shrapnel.Enabled = true;

					if (++valenceSize >= valenceCapacity) {
						if (valenceNum++ == 0)
							valenceCapacity = 8;
						else valenceCapacity <<= 1;
						valenceSize = 0;
					}
				}
			}
		}
	}
}