using System;
using Generic.Extensions;
using Generic.Models.Vectors;

namespace ParticleSimulator.Simulation.Gravity {
	public class CelestialBody : AParticle {
		public CelestialBody(int groupID, double[] position, double[] velocity, double mass)
		: base(groupID, position, velocity, mass) {}

		private double _mass;
		public override double Mass {
			get => this._mass;
			set { this._mass = value;
				this.Radius = RadiusOfMass(this._mass);
		}}
		public override double Radius { get; protected set; }
		
		public double[] ComputeInteractionForce(CelestialBody other) {
			double[] netForce = new double[Parameters.DIM];
			if (this.IsActive && other.IsActive) {
				double[] toOther = other.LiveCoordinates.Subtract(this.LiveCoordinates);
				double distance = toOther.Magnitude();
				
				if (distance < this.Radius + other.Radius) {
					if (distance <= Parameters.WORLD_EPSILON || distance <= (this.Radius > other.Radius ? other.Radius : this.Radius)) {//combine
						double newMass = this.Mass + other.Mass;
						double[]
							newCoordinates = this.LiveCoordinates.Multiply(this.Mass)
								.Add(other.LiveCoordinates.Multiply(other.Mass))
								.Divide(newMass),
							newVelocity = this.Velocity.Multiply(this.Mass)
								.Add(other.Velocity.Multiply(other.Mass))
								.Divide(this.Mass + other.Mass);
						if (this.Mass > other.Mass) {
							this.LiveCoordinates = newCoordinates;
							this.Velocity = newVelocity;
							other.IsActive = false;
						} else {
							other.LiveCoordinates = newCoordinates;
							other.Velocity = newVelocity;
							this.IsActive = false;
						}
					} else {//drag
						netForce = other
							.Velocity
							.Subtract(this.Velocity)
							.Multiply(1d - distance / (this.Radius + other.Radius))
							.Add(toOther.Multiply(Parameters.GRAVITATIONAL_CONSTANT * this.Mass * other.Mass / distance / distance / distance));
					}
				} else netForce = toOther.Multiply(Parameters.GRAVITATIONAL_CONSTANT * this.Mass * other.Mass / distance / distance / distance);
			}

			return netForce;
		}
		public override double[] ComputeInteractionForce(AParticle other) { return this.ComputeInteractionForce(other as CelestialBody); }

		public static double RadiusOfMass(double mass) {
			return NumberExtensions.HypersphereRadius(
				mass / Parameters.GRAVITY_DENSITY
					/ Math.Pow(mass, 1d / Parameters.DIM),
				Parameters.DIM);
		}
	}
}