﻿using System;
using Generic.Vectors;

namespace ParticleSimulator.Simulation.Gravity {
	public class MatterClump : AParticle {
		public MatterClump(int groupID, double[] position, double[] velocity, double mass)
		: base(groupID, position, velocity, mass) {}

		private double _mass;
		public override double Mass {
			get => this._mass;
			set { this._mass = value;
				this.Radius = RadiusOfMass(value);
		}}
		
		public double[] ComputeInteractionForce(MatterClump other) {
			double[] netForce = new double[Parameters.DIM];
			if (this.IsActive && other.IsActive) {
				MatterClump
					smaller = this.Mass < other.Mass ? this : other,
					larger = this.Mass < other.Mass ? other : this;
				double[] toOther = other.LiveCoordinates.Subtract(this.LiveCoordinates);
				double distance = toOther.Magnitude();

				netForce = toOther.Multiply(Parameters.GRAVITATIONAL_CONSTANT * this.Mass * other.Mass / distance / distance / distance);//gravity only
				bool doCombine = 
					distance <= Parameters.WORLD_EPSILON
					|| distance <= larger.Radius - smaller.Radius
					|| netForce.Magnitude() / this.Mass / distance / distance / Parameters.TIME_SCALE > Parameters.GRAVITY_MAX_ACCEL;
				if (doCombine) {
					netForce = new double[Parameters.DIM];

					double newMass = this.Mass + other.Mass;
					double[]
						newCoordinates = this.LiveCoordinates.Multiply(this.Mass)
							.Add(other.LiveCoordinates.Multiply(other.Mass))
							.Divide(newMass),
						newVelocity = this.Velocity.Multiply(this.Mass)
							.Add(other.Velocity.Multiply(other.Mass))
							.Divide(this.Mass + other.Mass);
					larger.LiveCoordinates = newCoordinates;
					larger.Velocity = newVelocity;
					larger.Mass = newMass;
					smaller.IsActive = false;
				} else if (distance < this.Radius + other.Radius) {//drag
					netForce = netForce.Add(
						other.Velocity
							.Subtract(this.Velocity)
							.Multiply(1d - distance / (this.Radius + other.Radius)));
				}
			}

			return netForce;
		}
		public override double[] ComputeInteractionForce(AParticle other) { return this.ComputeInteractionForce(other as MatterClump); }

		public static double RadiusOfMass(double mass) {
			return Math.Cbrt(mass) / Parameters.GRAVITY_DENSITY;
		}
	}
}