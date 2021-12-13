using System;
using Generic.Vectors;

namespace ParticleSimulator.Simulation.Gravity {
	public class MatterClump : AClassicalParticle {
		private double _radius = 0d;
		public override double Radius => this._radius;

		public MatterClump(int groupID, double[] position, double[] velocity, double mass, double charge = 0d)
		: base(groupID, position, velocity, mass, charge) {
			this._radius = RadiusOfMass(mass);
		}

		public static double RadiusOfMass(double mass) {
			return Math.Cbrt(mass) / Parameters.GRAVITY_DENSITY;
		}
		/*
		public double[] ComputeInteractionForce(Matter other) {
			double[] netForce = new double[Parameters.DIM];
			if (this.IsAlive && other.IsAlive) {
				//compute gravity
				
				Matter
					smaller = this.PhysicalAttributes[PhysicalAttribute.Mass] < other.PhysicalAttributes[PhysicalAttribute.Mass] ? this : other,
					larger = this.PhysicalAttributes[PhysicalAttribute.Mass] < other.PhysicalAttributes[PhysicalAttribute.Mass] ? other : this;
				bool tooClose = distance <= Parameters.WORLD_EPSILON,
					engulfed = distance <= larger.Radius - Parameters.GRAVITY_COMBINE_OVERLAP_CUTOFF * smaller.Radius,
					excessiveForce = netForce.Magnitude() / this.PhysicalAttributes[PhysicalAttribute.Mass] / distance / distance > Parameters.GRAVITY_MAX_ACCEL * Parameters.TIME_SCALE;//time step of resulting velocity
				if (tooClose || engulfed) {//into one larger particle
					if (Parameters.GRAVITY_COLLISION_COMBINE) {//momentum-preserving
					}
				}
				if (tooClose || engulfed) {//into one larger particle
					if (Parameters.GRAVITY_COLLISION_COMBINE) {//momentum-preserving
						netForce = new double[Parameters.DIM];//ignore gravity
						double newMass = this.Mass + other.Mass;
						double[]
							newCoordinates = this.LiveCoordinates.Multiply(this.Mass)
								.Add(other.LiveCoordinates.Multiply(other.Mass))
								.Divide(newMass),
							newVelocity = this.Velocity.Multiply(this.Mass)
								.Add(other.Velocity.Multiply(other.Mass))
								.Divide(this.Mass + other.Mass),
							newNetForce = this.NetForce.Add(other.NetForce);
						//remove smaller particle
						larger.LiveCoordinates = newCoordinates;
						larger.Velocity = newVelocity;
						larger.Mass = newMass;
						larger.NetForce = newNetForce;
						smaller.IsActive = false;
					} else if (tooClose)//treat as in the same spot
						netForce = new double[Parameters.DIM];//ignore gravity
					else if (excessiveForce)
						netForce = netForce.Normalize(
							Parameters.GRAVITY_MAX_ACCEL * Parameters.TIME_SCALE
							* distance * distance * this.Mass);
				} else if (Parameters.GRAVITY_COLLISION_DRAG_STRENGTH > 0d && distance < this.Radius + other.Radius) {//overlap - drag
					if (excessiveForce)
						netForce = netForce.Normalize(
							Parameters.GRAVITY_MAX_ACCEL * Parameters.TIME_SCALE
							* distance * distance * this.Mass);
					double overlapRange = this.Radius + other.Radius - larger.Radius;
					double[] dragForce =
						other.Velocity
							.Subtract(this.Velocity)
							.Multiply(Parameters.GRAVITY_COLLISION_DRAG_STRENGTH * smaller.Radius * (distance - larger.Radius) / overlapRange);
					//do not include in result, apply directly
					this.NetForce = this.NetForce.Add(dragForce);
					other.NetForce = other.NetForce.Subtract(dragForce);
				} else if (excessiveForce)
					netForce = netForce.Normalize(
						Parameters.GRAVITY_MAX_ACCEL * Parameters.TIME_SCALE
						* distance * distance * this.Mass);
			}

			return netForce;
		}
	*/
	}
}