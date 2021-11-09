using System.Collections.Generic;
using System.Linq;

using Generic.Models;

namespace Simulation.Gravity {
	public class CelestialBody : AParticle {
		public const double GRAVITATIONAL_CONSTANT = 0.000000000066743d;

		public CelestialBody(double[] position, double mass) : base(position, mass) {
			this._contributingBaryonsAcceleration = new double[this.Dimensionality];
		}

		internal readonly Dictionary<int, double[]> _contributingAccelerations = new();
		private double[] _contributingBaryonsAcceleration;
		public void Interact(BaryonQuadTree<AParticle> baryonNode) {
			double[] toOther = VectorFunctions.Subtract(this.Coordinates, baryonNode.Barycenter.Current);
			double distance = VectorFunctions.Magnitude(toOther);
			double[] toOtherNormalized = VectorFunctions.Divide(toOther, distance);
			this._contributingBaryonsAcceleration = VectorFunctions.Add(
				this._contributingBaryonsAcceleration,
				VectorFunctions.Multiply(toOtherNormalized, GRAVITATIONAL_CONSTANT * baryonNode.TotalMass / distance / distance));
		}
		public void Interact(CelestialBody other) {
			if (this._contributingAccelerations.ContainsKey(other.ID)) return;//handshake optimization
			else {
				double[] toOther = VectorFunctions.Subtract(this.Coordinates, other.Coordinates);
				double distance = VectorFunctions.Magnitude(toOther);
				double[] toOtherNormalized = VectorFunctions.Divide(toOther, distance);
				double[] force = VectorFunctions.Multiply(toOtherNormalized, GRAVITATIONAL_CONSTANT * this.Mass * other.Mass / distance / distance);

				this._contributingAccelerations[other.ID] = VectorFunctions.Divide(force, this.Mass);
				other._contributingAccelerations[this.ID] = VectorFunctions.Divide(force, -other.Mass);
			}
		}
		internal void ApplyUpdate() {
			this.Coordinates = (Vector)VectorFunctions.Add(
				this.Coordinates,
				VectorFunctions.Add(
					_contributingBaryonsAcceleration, 
					this._contributingAccelerations.Values.Aggregate(new double[this.Dimensionality],
						(agg, x) => VectorFunctions.Add(agg, x))));
			this._contributingAccelerations.Clear();
			this._contributingBaryonsAcceleration = new double[this.Dimensionality];
		}
	}
}