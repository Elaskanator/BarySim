using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Generic;
using Generic.Abstractions;

namespace Boids {
	public class Boid : IVector, IEquatable<Boid>, IEqualityComparer<Boid> {
		private static int _id = 0;
		public readonly int ID = ++_id;

		public readonly Flock Flock;

		private double[] _coordinates;
		public double[] Coordinates {
			get { return this._coordinates; }
			set { this._coordinates = this.BoundPosition(value).ToArray(); } }
		private double[] _velocity;
		public double[] Velocity {
			get { return this._velocity; }
			set { this._velocity = value.Clamp(this.Flock.MaxSpeed); } }
		private double[] _acceleration;
		public double[] Acceleration {
			get { return this._acceleration; }
			set { this._acceleration = value.Clamp(this.Flock.MaxAcceleration); } }

		private double _mass;
		public double Mass {
			get { return this._mass; }
			set { if (value <= 0) throw new ArgumentOutOfRangeException("Mass"); else this._mass = value; } }

		public double Vision { get; private set; }

		public Boid(Flock flock, double[] position, double[] velocity, double mass = 1) {
			this.Flock = flock;
			this.Coordinates = position;
			this.Velocity = velocity;
			this.Mass = mass;
			this.Vision = this.Flock.Separation;
		}

		public void UpdateDeltas(Boid[] neighbors) {
			double[] bias = this.ComputeNeighborhoodBias(neighbors);

			this.Acceleration = bias
				.Divide(this.Mass);

			this.Velocity = this.Velocity
				.Multiply(this.Flock.SpeedDecay)
				.Add(this.Acceleration);

			this.Coordinates = this.Coordinates.Add(this.Velocity);

			switch (neighbors.Length.CompareTo(Parameters.DESIRED_NEIGHBORS)) {
				case -1:
					this.Vision++;
					break;
				case 1:
					if (this.Vision > 2.0d) this.Vision -= 1.0d;
					else if (this.Vision > 1.5d) this.Vision -= 0.5d;
					else if (this.Vision > 1.0d) this.Vision -= 0.25d;
					else if (this.Vision > 0.5d) this.Vision -= 0.125d;
					else this.Vision -= 0.1;
					break;
			}
		}

		private IEnumerable<double> BoundPosition(double[] position) {
			for (int i = 0; i < Parameters.Domain.Length; i++)
				if (position[i] < 0 || position[i] >= Parameters.Domain[i])
					yield return position[i].ModuloAbsolute(Parameters.Domain[i]);//wrap around
				else
					yield return position[i];
		}

		//TODO rewrite this method entirely
		//seealso https://swharden.com/CsharpDataVis/boids/boids.md.html
		private double[] ComputeNeighborhoodBias(Boid[] neighbors) {
			bool anyNeighbors = false;
			double cohesionBiasWeight = 0d, alignmentBiasWeight = 0d, separationBiasWeight = 0d;
			double[] cohesionBias, separationBias, alignmentBias;
			cohesionBias = separationBias = alignmentBias = Enumerable.Repeat(0d, Parameters.Domain.Length).ToArray();

			double dist;
			double weight;
			double[] vect;
			//double[] positionPrime;
			foreach (Boid other in neighbors) {
				anyNeighbors = true;
				
				//positionPrime = b.GetNearestWrappedPosition(this);
				dist = this.Coordinates.Distance(other.Coordinates);

				if (Parameters.ENABLE_COHESION && this.Flock.CohesionWeight > 0d) {
					weight = this.GetCohesionWeight(other, dist);
					cohesionBiasWeight += weight;
					cohesionBias = cohesionBias.Add(other.Coordinates.Multiply(weight));
				}

				if (Parameters.ENABLE_ALIGNMENT && this.Flock.AlignmentWeight > 0d) {
					weight = this.GetAlignmentWeight(other, dist);
					alignmentBiasWeight += weight;
					alignmentBias = alignmentBias.Add(other.Velocity.Multiply(weight));
				}

				if (Parameters.ENABLE_SEPARATION && this.Flock.SeparationWeight > 0d && dist < this.Flock.Separation) {
					weight = this.GetSeparationWeight(other, dist);
					separationBiasWeight += weight;
					vect = this.Coordinates.Subtract(other.Coordinates);
					separationBias = separationBias.Add(vect.Multiply(weight));
				}
			}

			if (anyNeighbors) {
				if (cohesionBiasWeight > 0) cohesionBias = cohesionBias.Divide(cohesionBiasWeight);
				if (alignmentBiasWeight > 0) alignmentBias = alignmentBias.Divide(alignmentBiasWeight);
				if (separationBiasWeight > 0) separationBias = separationBias.Divide(separationBiasWeight);
				
				//separationBias = separationBias.Subtract(this.Velocity);

				return
					Enumerable.Repeat(0d, Parameters.Domain.Length).ToArray()//for testing
					//.Add(Enumerable.Repeat(0.005d, Parameters.Domain.Length).ToArray())//for testing
					.Add(cohesionBias.Normalize().Multiply(this.Flock.CohesionWeight))
					.Add(alignmentBias)
					.Add(separationBias)
					.Clamp(this.Flock.MaxForce);
			} else {
				return Enumerable.Repeat(0d, Parameters.Domain.Length).ToArray();
			}
		}

		//TODO better curve
		public double GetCohesionWeight(Boid other, double dist) {
			if (this.Flock != other.Flock) return 0;

			double result;
			if (dist <= 0d
				//|| dist < this.Flock.Separation
			)
				result = 0d;
			else
				result = this.Flock.CohesionWeight / dist;

			if (result > this.Flock.MaxImpulse_Cohesion)
				return this.Flock.MaxImpulse_Cohesion;
			else
				return result;
		}

		public double GetAlignmentWeight(Boid other, double dist) {
			if (this.Flock != other.Flock) return 0;

			if (dist == 0d)
				return this.Flock.MaxImpulse_Alignment;
			else {
				double result = this.Flock.AlignmentWeight / Math.Sqrt(dist);
				if (result > this.Flock.MaxImpulse_Alignment)
					return this.Flock.MaxImpulse_Alignment;
				else
					return result;
			}
		}

		public double GetSeparationWeight(Boid other, double dist) {
			if (dist <= 0d)
				return this.Flock.MaxImpulse_Separation;
			else {
				double result = this.Flock.SeparationWeight / dist / dist;
				if (result > this.Flock.MaxImpulse_Separation)
					return this.Flock.MaxImpulse_Separation;
				else
					return result;
			}
		}

		public bool Equals(Boid other) {
			return this.ID == other.ID;
		}

		public override string ToString() {
			return string.Format("Boid<{0}><{1}",
				string.Join(", ", this.Coordinates.Select(i => i.ToString("G5"))),
				string.Join(", ", this.Velocity.Select(i => i.ToString("G5"))));
		}

		public bool Equals([AllowNull] Boid x, [AllowNull] Boid y) {
			return !(x is null) && !(y is null) && x.ID == y.ID;
		}

		public int GetHashCode([DisallowNull] Boid obj) {
			return obj.ID.GetHashCode();
		}
	}
}