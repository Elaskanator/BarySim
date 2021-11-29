using System;
using Generic.Extensions;
using Generic.Models.Vectors;

namespace ParticleSimulator.Simulation.Gravity {
	public class CelestialBody : AParticle {
		public CelestialBody(int groupID, double[] position, double[] velocity, double mass) : base(groupID, position, velocity) {
			this.Mass = mass;
		}

		private double _mass;
		public double Mass {
			get => this._mass;
			set {
				this._mass = value;
				this.Radius = NumberExtensions.HypersphereRadius(
					this._mass
					* Math.Pow(1d / this._mass, 1d / Parameters.GRAVITY_COMPRESSION_SCALING_POW)
					/ Parameters.GRAVITY_DENSITY,
					Parameters.DIM);
		}}
		public double Radius { get; private set; }

		public static double[] ComputeInteraction(double[] coordinates, double mass, double[] otherCoordinates, double otherMass) {
			double[] toOther = otherCoordinates.Subtract(coordinates);
			double distance = VectorFunctions.Magnitude(toOther);
			return VectorFunctions.Multiply(
				toOther,//one division of distance is to normalize the direction vector
				Parameters.GRAVITATIONAL_CONSTANT * mass * otherMass / distance / distance / distance);
		}
		protected override void Interact(AParticle other) {
			throw new InvalidOperationException();
		}
	}
}