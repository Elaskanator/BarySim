using System;
using System.Collections.Generic;
using Generic.Extensions;
using Generic.Vectors;

namespace ParticleSimulator.Simulation.Gravity {
	public class MatterClump : AClassicalParticle<MatterClump> {
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
			set { this._mass = value;
				this._density = Parameters.GRAVITY_COMPRESSION_BIAS > 0d
					? 1d / (1d - (1d / (1d + (1d / Math.Log(1d + value, 1d + Parameters.GRAVITY_COMPRESSION_BIAS)))))
					: 1d;
				this._radius = Math.Pow(value, 1d / Parameters.DIM) / Parameters.GRAVITY_RADIAL_DENSITY / this._density;
				this._luminosity = Math.Pow(value * Parameters.MASS_LUMINOSITY_SCALAR, 3.5d); }}
		

		protected override void AfterUpdate() {
			if (this.Mass >= Parameters.GRAVITY_CRITICAL_MASS) {
				HashSet<MatterClump> memberParticles = this.DistinctRecursiveChildren(p => p.MergedParticles);
				this.MergedParticles.Clear();

				double totalMass = this.Mass;
				double excessMass = totalMass - Parameters.GRAVITY_CRITICAL_MASS;
				double intensityFraction = excessMass / Parameters.GRAVITY_CRITICAL_MASS;
				double velocity;
				if (Parameters.GRAVITY_EXPLOSION_SPEED_LOW_BIAS > 0)
					velocity = Parameters.GRAVITY_EXPLOSION_MIN_SPEED
						+ (Parameters.GRAVITY_EXPLOSION_MAX_SPEED - Parameters.GRAVITY_EXPLOSION_MIN_SPEED)
							* (1d - (1d / (1d + (1d / Math.Log(1d + intensityFraction, 1d + Parameters.GRAVITY_EXPLOSION_SPEED_LOW_BIAS + 1)))));
				else velocity = Parameters.GRAVITY_EXPLOSION_MAX_SPEED;
				double[] centerOfMass = (double[])this.LiveCoordinates.Clone();
				double avgMass = totalMass / memberParticles.Count;
				double[] avgMomentum = this.Momentum.Divide(memberParticles.Count);

				this.Mass = avgMass;
				double valenceRadius = this.Radius;
				int valenceNum = 0, valenceSize = 0, valenceCapacity = 1, valenceScaling = 1 << Parameters.DIM;
				double[] direction;
				foreach (MatterClump shrapnel in memberParticles) {
					direction = HyperspaceFunctions.RandomUnitVector_Spherical(Parameters.DIM, Program.Random);

					shrapnel.Mass = avgMass;
					shrapnel.LiveCoordinates = centerOfMass.Add(direction.Multiply(valenceNum * valenceRadius));
					shrapnel.Velocity = valenceNum == 0 && valenceCapacity == 1
						? new double[Parameters.DIM]
						: direction.Multiply(velocity);
					shrapnel.Momentum = shrapnel.Momentum.Add(avgMomentum);
					shrapnel.Enabled = true;

					if (++valenceSize >= valenceCapacity) {
						valenceNum++;
						valenceCapacity *= valenceScaling;
						valenceSize = 0;
					}
				}
			}
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