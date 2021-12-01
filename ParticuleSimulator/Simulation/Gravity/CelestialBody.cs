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
			set {
				this._mass = value;
				this.Radius = RadiusOfMass(this._mass);
		}}
		public override double Radius { get; protected set; }

		public static double RadiusOfMass(double mass) {
			return NumberExtensions.HypersphereRadius(
				mass
				/ Math.Pow(mass + 1d, 1d / Parameters.DIM)
				/ Parameters.GRAVITY_DENSITY,
				Parameters.DIM);
		}
		
		public double[] ComputeInteractionForce(CelestialBody other) {
			if (other.IsActive) {
				double distance;
				double[] toOther;

				toOther = other.LiveCoordinates.Subtract(this.LiveCoordinates);
				distance = toOther.Magnitude();
				if (distance < Parameters.WORLD_EPSILON || distance < this.Radius + other.Radius) {//collision
					if (Parameters.GRAVITY_DO_COMBINE) {
						other.IsActive = false;
						this.LiveCoordinates =
							this.LiveCoordinates.Multiply(this.Mass)
							.Add(other.LiveCoordinates.Multiply(other.Mass))
							.Divide(this.Mass + other.Mass);
						this.Velocity =
							this.Velocity.Multiply(this.Mass)
							.Add(other.Velocity.Multiply(other.Mass))
							.Divide(this.Mass + other.Mass);
						this.Mass += other.Mass;
					} else throw new NotImplementedException();
				} else return
					toOther.Multiply(Parameters.GRAVITATIONAL_CONSTANT * this.Mass * other.Mass / distance / distance / distance);
			}
			return new double[Parameters.DIM];
		}
		public override double[] ComputeInteractionForce(AParticle other) { return this.ComputeInteractionForce(other as CelestialBody); }
	}
}