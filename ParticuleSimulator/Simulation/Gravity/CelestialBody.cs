//using System;
//using System.Collections.Generic;
//using Generic.Extensions;
//using Generic.Models;

//namespace ParticleSimulator.Simulation.Gravity {
//	public class CelestialBody : AParticle {
//		public const double GRAVITATIONAL_CONSTANT = 0.66743d;

//		private readonly double _radius;
//		public override double Radius => this._radius;

//		public CelestialBody(int groupID, double[] position, double[] velocity, double mass) : base(groupID, position, velocity, mass) {
//			this._contributingBaryonsAcceleration = new double[this.DIMENSIONALITY];
//			this._radius = NumberExtensions.HypersphereRadius(mass, this.DIMENSIONALITY);
//		}

//		internal readonly Dictionary<int, double[]> _contributingAccelerations = new();
//		private double[] _contributingBaryonsAcceleration;
//		public override void InteractSubtree(ITree node) {
//			double[] toOther = this.TrueCoordinates.Subtract(((ParticleTree<CelestialBody>)node).Barycenter.Current);
//			double distance = VectorFunctions.Magnitude(toOther);

//			double[] toOtherNormalized;
//			if (distance > this.Radius) toOtherNormalized = VectorFunctions.Divide(toOther, distance);
//			else return;

//			this._contributingBaryonsAcceleration = VectorFunctions.Add(
//				this._contributingBaryonsAcceleration,
//				VectorFunctions.Multiply(toOtherNormalized, GRAVITATIONAL_CONSTANT * ((ParticleTree<CelestialBody>)node).TotalMass / distance / distance));
//		}
//		public void Interact(CelestialBody other) {
//			if (this._contributingAccelerations.ContainsKey(other.ID)) return;//handshake optimization
//			else {
//				double[] toOther = this.TrueCoordinates.Subtract(other.TrueCoordinates);
//				double distance = VectorFunctions.Magnitude(toOther);
				
//				double[] toOtherNormalized;
//				if (distance > this.Radius) toOtherNormalized = VectorFunctions.Divide(toOther, distance);
//				else return;

//				double[] force = VectorFunctions.Multiply(toOtherNormalized, GRAVITATIONAL_CONSTANT * this.Mass * other.Mass / distance / distance);

//				this._contributingAccelerations[other.ID] = VectorFunctions.Divide(force, this.Mass);
//				other._contributingAccelerations[this.ID] = VectorFunctions.Divide(force, -other.Mass);
//			}
//		}
//		public override void Interact(AParticle other) { this.Interact((CelestialBody)other); }
		
//		public override void InteractMany(ATree<AParticle> tree) { throw new NotImplementedException(); }
//	}
//}