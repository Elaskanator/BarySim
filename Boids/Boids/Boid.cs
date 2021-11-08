using System;
using System.Collections.Generic;
using System.Linq;

using Generic;

namespace Simulation.Boids {
	public class Boid : AParticle {
		public readonly Flock Flock;

		private double[] _coordinates;
		public override double[] Coordinates {
			get { return this._coordinates; }
			set { this._coordinates = this.BoundPosition(value).ToArray(); } }
		private double[] _velocity;
		public override double[] Velocity {
			get { return this._velocity; }
			set { this._velocity = value.Clamp(Parameters.DEFAULT_MAX_SPEED); } }
		private double[] _acceleration;
		public override double[] Acceleration {
			get { return this._acceleration; }
			set { this._acceleration = value.Clamp(Parameters.DEFAULT_MAX_ACCELERATION); } }

		private double _mass;
		public override double Mass {
			get { return this._mass; }
			set { if (value <= 0) throw new ArgumentOutOfRangeException("Mass"); else this._mass = value; } }

		public Boid(Flock flock, double[] position, double[] velocity, double mass = 1)
		: base(position, velocity, null, mass) {
			this.Flock = flock;
		}

		public void UpdateDeltas(IEnumerable<Boid> neighbors) {
			double[] bias = this.ComputeNeighborhoodBias(neighbors);

			this.Acceleration = bias
				.Divide(this.Mass);

			this.Velocity = this.Velocity
				.Multiply(this.Flock.SpeedDecay)
				.Add(this.Acceleration);

			this.Coordinates = this.Coordinates.Add(this.Velocity);
		}

		private IEnumerable<double> BoundPosition(double[] position) {
			for (int i = 0; i < Parameters.DOMAIN.Length; i++)
				if (position[i] < 0 || position[i] >= Parameters.DOMAIN[i])
					yield return position[i].ModuloAbsolute(Parameters.DOMAIN[i]);//wrap around
				else yield return position[i];
		}

		//TODO rewrite this method entirely
		//seealso https://swharden.com/CsharpDataVis/boids/boids.md.html
		private double[] ComputeNeighborhoodBias(IEnumerable<Boid> neighbors) {
			bool anyNeighbors = false;
			double cohesionBiasWeight = 0d, alignmentBiasWeight = 0d, separationBiasWeight = 0d;
			double[] cohesionBias, separationBias, alignmentBias;
			cohesionBias = separationBias = alignmentBias = Enumerable.Repeat(0d, Parameters.DOMAIN.Length).ToArray();

			double dist;
			double weight;
			double[] vect;
			//double[] positionPrime;

			int count = 0;
			foreach (Boid other in neighbors) {
				if (count++ > Parameters.DESIRED_NEIGHBORS)
					break;
				
				//positionPrime = b.GetNearestWrappedPosition(this);
				dist = this.Coordinates.Distance(other.Coordinates);

				if (Parameters.ENABLE_COHESION && Parameters.DEFAULT_COHESION_WEIGHT > 0d) {
					weight = this.GetCohesionWeight(other, dist);
					cohesionBiasWeight += weight;
					cohesionBias = cohesionBias.Add(other.Coordinates.Multiply(weight));
				}

				if (Parameters.ENABLE_ALIGNMENT && Parameters.DEFAULT_ALIGNMENT_WEIGHT > 0d) {
					weight = this.GetAlignmentWeight(other, dist);
					alignmentBiasWeight += weight;
					alignmentBias = alignmentBias.Add(other.Velocity.Multiply(weight));
				}

				if (Parameters.ENABLE_SEPARATION && Parameters.DEFAULT_SEPARATION_WEIGHT > 0d && dist < this.Flock.Separation) {
					weight = this.GetSeparationWeight(other, dist);
					separationBiasWeight += weight;
					vect = this.Coordinates.Subtract(other.Coordinates);
					separationBias = separationBias.Add(vect.Multiply(weight));
				}
			}

			if (count > 0) {
				if (cohesionBiasWeight > 0) cohesionBias = cohesionBias.Divide(cohesionBiasWeight);
				if (alignmentBiasWeight > 0) alignmentBias = alignmentBias.Divide(alignmentBiasWeight);
				if (separationBiasWeight > 0) separationBias = separationBias.Divide(separationBiasWeight);
				
				//separationBias = separationBias.Subtract(this.Velocity);

				return
					Enumerable.Repeat(0d, Parameters.DOMAIN.Length).ToArray()//for testing
					//.Add(Enumerable.Repeat(0.005d, Parameters.Domain.Length).ToArray())//for testing
					.Add(cohesionBias.Normalize().Multiply(Parameters.DEFAULT_COHESION_WEIGHT))
					.Add(alignmentBias)
					.Add(separationBias)
					.Clamp(Parameters.DEFAULT_MAX_FORCE);
			} else {
				return Enumerable.Repeat(0d, Parameters.DOMAIN.Length).ToArray();
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
				result = Parameters.DEFAULT_COHESION_WEIGHT / dist;

			if (result > Parameters.DEFAULT_MAX_IMPULSE_COHESION)
				return Parameters.DEFAULT_MAX_IMPULSE_COHESION;
			else
				return result;
		}

		public double GetAlignmentWeight(Boid other, double dist) {
			if (this.Flock != other.Flock) return 0;

			if (dist == 0d)
				return Parameters.DEFAULT_MAX_IMPULSE_ALIGNMENT;
			else {
				double result = Parameters.DEFAULT_ALIGNMENT_WEIGHT / Math.Sqrt(dist);
				if (result > Parameters.DEFAULT_MAX_IMPULSE_ALIGNMENT)
					return Parameters.DEFAULT_MAX_IMPULSE_ALIGNMENT;
				else
					return result;
			}
		}

		public double GetSeparationWeight(Boid other, double dist) {
			if (dist <= 0d)
				return Parameters.DEFAULT_MAX_IMPULSE_SEPARATION;
			else {
				double result = Parameters.DEFAULT_SEPARATION_WEIGHT / dist / dist;
				if (result > Parameters.DEFAULT_MAX_IMPULSE_SEPARATION)
					return Parameters.DEFAULT_MAX_IMPULSE_SEPARATION;
				else
					return result;
			}
		}
	}
}