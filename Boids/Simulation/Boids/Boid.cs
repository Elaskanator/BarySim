using System;
using System.Collections.Generic;
using System.Linq;
using Generic.Extensions;
using Generic.Models;

namespace ParticleSimulator.Simulation.Boids {
	public class Boid : AParticle {
		public readonly Flock Flock;

		private SimpleVector _velocity;
		public override SimpleVector Velocity {
			get { return this._velocity; }
			set { this._velocity = VectorFunctions.Clamp(value, Parameters.DEFAULT_MAX_SPEED); } }
		private SimpleVector _acceleration;
		public SimpleVector Acceleration {
			get { return this._acceleration; }
			set { this._acceleration = VectorFunctions.Clamp(value, Parameters.DEFAULT_MAX_ACCELERATION); } }
		public override double Radius => 0;

		public Boid(Flock flock, SimpleVector position, SimpleVector velocity, double mass = 1)
		: base(position, velocity, mass) {
			this.Flock = flock;
			this.Velocity = velocity;
			this.Acceleration = new double[this.Dimensionality];
		}

		public void UpdateDeltas(IEnumerable<Boid> neighbors) {
			this.Acceleration = this.ComputeNeighborhoodBias(neighbors);
		}

		internal override void ApplyUpdate() {
			this.Velocity = VectorFunctions.Add(
				VectorFunctions.Multiply(this.Velocity, this.Flock.SpeedDecay),
				this.Acceleration);

			this.Coordinates = VectorFunctions.Add(
				this.Coordinates,
				this.Velocity);
		}

		//TODO rewrite this method entirely
		//seealso https://swharden.com/CsharpDataVis/boids/boids.md.html
		private SimpleVector ComputeNeighborhoodBias(IEnumerable<Boid> neighbors) {
			double cohesionBiasWeight = 0d, alignmentBiasWeight = 0d, separationBiasWeight = 0d;
			SimpleVector cohesionBias, separationBias, alignmentBias;
			cohesionBias = separationBias = alignmentBias = Enumerable.Repeat(0d, Parameters.DOMAIN.Length).ToArray();

			double dist;
			double weight;
			SimpleVector vect;
			//double[] positionPrime;

			int count = 0;
			foreach (Boid other in neighbors.Except(x => x.ID == this.ID)) {
				if (++count > Parameters.DESIRED_NEIGHBORS)
					break;
				
				//positionPrime = b.GetNearestWrappedPosition(this);
				dist = VectorFunctions.Distance(this.Coordinates, other.Coordinates);

				if (Parameters.ENABLE_COHESION && Parameters.DEFAULT_COHESION_WEIGHT > 0d) {
					weight = this.GetCohesionWeight(other, dist);
					cohesionBiasWeight += weight;
					cohesionBias = VectorFunctions.Add(cohesionBias, VectorFunctions.Multiply(other.Coordinates, weight));
				}

				if (Parameters.ENABLE_ALIGNMENT && Parameters.DEFAULT_ALIGNMENT_WEIGHT > 0d) {
					weight = this.GetAlignmentWeight(other, dist);
					alignmentBiasWeight += weight;
					alignmentBias = VectorFunctions.Add(alignmentBias, VectorFunctions.Multiply(other.Velocity, weight));
				}

				if (Parameters.ENABLE_SEPARATION && Parameters.DEFAULT_SEPARATION_WEIGHT > 0d && dist < this.Flock.Separation) {
					weight = this.GetSeparationWeight(other, dist);
					separationBiasWeight += weight;
					vect = VectorFunctions.Subtract(this.Coordinates, other.Coordinates);
					separationBias = VectorFunctions.Add(separationBias, VectorFunctions.Multiply(vect, weight));
				}
			}

			if (count > 0) {
				if (cohesionBiasWeight > 0) cohesionBias = VectorFunctions.Multiply(cohesionBias, cohesionBiasWeight);
				if (alignmentBiasWeight > 0) alignmentBias = VectorFunctions.Multiply(alignmentBias, alignmentBiasWeight);
				if (separationBiasWeight > 0) separationBias = VectorFunctions.Multiply(separationBias, separationBiasWeight);

				return
					VectorFunctions.Clamp(
						(VectorFunctions.Multiply(cohesionBias, Parameters.DEFAULT_COHESION_WEIGHT)
						+ VectorFunctions.Multiply(alignmentBias, Parameters.DEFAULT_ALIGNMENT_WEIGHT)
						+ VectorFunctions.Multiply(separationBias, Parameters.DEFAULT_SEPARATION_WEIGHT)
						) / this.Mass,
						Parameters.MAX_ACCELERATION);
			} else return new double[Parameters.DOMAIN.Length];
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