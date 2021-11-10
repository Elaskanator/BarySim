using System.Collections.Generic;
using System.Linq;
using Generic.Extensions;
using Generic.Models;

namespace ParticleSimulator.Simulation.Gravity {
	public class CelestialBody : AParticle {
		public const double GRAVITATIONAL_CONSTANT = 0.66743d;

		private readonly double _radius;
		public override double Radius => this._radius;

		public CelestialBody(double[] position, double[] velocity, double mass) : base(position, velocity, mass) {
			this._contributingBaryonsAcceleration = new double[this.Dimensionality];

			this._radius = NumberExtensions.HypersphereRadius(mass, this.Dimensionality);
		}

		internal readonly Dictionary<int, double[]> _contributingAccelerations = new();
		private double[] _contributingBaryonsAcceleration;
		public void Interact(BaryonQuadTree baryonNode) {
			double[] toOther = this.Coordinates.Subtract(baryonNode.Barycenter.Current);
			double distance = VectorFunctions.Magnitude(toOther);

			double[] toOtherNormalized;
			if (distance > this.Radius) toOtherNormalized = VectorFunctions.Divide(toOther, distance);
			else return;

			this._contributingBaryonsAcceleration = VectorFunctions.Addition(
				this._contributingBaryonsAcceleration,
				VectorFunctions.Multiply(toOtherNormalized, GRAVITATIONAL_CONSTANT * baryonNode.TotalMass / distance / distance));
		}
		public void Interact(CelestialBody other) {
			if (this._contributingAccelerations.ContainsKey(other.ID)) return;//handshake optimization
			else {
				double[] toOther = this.Coordinates.Subtract(other.Coordinates);
				double distance = VectorFunctions.Magnitude(toOther);
				
				double[] toOtherNormalized;
				if (distance > this.Radius) toOtherNormalized = VectorFunctions.Divide(toOther, distance);
				else return;

				double[] force = VectorFunctions.Multiply(toOtherNormalized, GRAVITATIONAL_CONSTANT * this.Mass * other.Mass / distance / distance);

				this._contributingAccelerations[other.ID] = VectorFunctions.Divide(force, this.Mass);
				other._contributingAccelerations[this.ID] = VectorFunctions.Divide(force, -other.Mass);
			}
		}

		internal override void ApplyUpdate() {
			this.Coordinates = VectorFunctions.Addition(
				this.Coordinates,
				VectorFunctions.Addition(
					_contributingBaryonsAcceleration, 
					this._contributingAccelerations.Values.Aggregate(new double[this.Dimensionality],
						(agg, x) => VectorFunctions.Addition(agg, x))));
			this._contributingAccelerations.Clear();
			this._contributingBaryonsAcceleration = new double[this.Dimensionality];
		}
	}
}