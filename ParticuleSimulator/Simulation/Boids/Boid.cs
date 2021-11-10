using System;
using System.Collections.Generic;
using System.Linq;
using Generic.Extensions;
using Generic.Models;

namespace ParticleSimulator.Simulation.Boids {
	public class Boid: AParticleDouble {
		public readonly Flock Flock;

		public Boid(Flock flock, VectorDouble position, VectorDouble velocity, double mass = 1)
		: base(position, velocity, mass) {
			this.Flock = flock;
			this.Velocity = new VectorDouble(new double[position.Dimensionality]);
		}
		
		private IVector<double> _velocity;
		public override IVector<double> Velocity {
			get { return this._velocity; }
			set { this._velocity = new VectorDouble(VectorFunctions.Clamp(value.Coordinates, Parameters.MAX_SPEED)); } }
		private IVector<double> _acceleration;
		public IVector<double> Acceleration {
			get { return this._acceleration; }
			set { this._acceleration = new VectorDouble(VectorFunctions.Clamp(value.Coordinates, Parameters.MAX_ACCELERATION)); } }
		public override double Radius => 0f;

		public void UpdateDeltas(IEnumerable<Boid> neighbors) {
			this.Acceleration = this.ComputeNeighborhoodBias(neighbors);
		}

		internal override void ApplyUpdate() {
			this.Velocity = new VectorDouble(VectorFunctions.Addition(VectorFunctions.Multiply(this.Velocity, Parameters.SPEED_DECAY), this.Acceleration.Coordinates));

			this.Coordinates = VectorFunctions.Addition(
				this,
				this.Velocity);
		}

		//TODO rewrite this method entirely
		//seealso https://swharden.com/CsharpDataVis/boids/boids.md.html
		private VectorDouble ComputeNeighborhoodBias(IEnumerable<Boid> neighbors) {
			double cohesionBiasWeight = 0d, alignmentBiasWeight = 0d, separationBiasWeight = 0d;
			VectorDouble cohesionBias, separationBias, alignmentBias;
			cohesionBias = separationBias = alignmentBias = Enumerable.Repeat(0d, Parameters.DOMAIN_DOUBLE.Length).ToArray();

			double dist;
			double weight;
			VectorDouble awayVector;

			int count = 0;
			foreach (Boid other in neighbors.Except(x => x.ID == this.ID)) {
				if (++count > Parameters.DESIRED_NEIGHBORS)
					break;
				
				dist = VectorFunctions.Distance(this.Coordinates, other.Coordinates);

				if (Parameters.ENABLE_COHESION && Parameters.COHESION_WEIGHT > 0d) {
					weight = this.GetCohesionWeight(other, dist);
					cohesionBiasWeight += weight;
					cohesionBias += other.Coordinates.Multiply(weight);
				}

				if (Parameters.ENABLE_ALIGNMENT && Parameters.ALIGNMENT_WEIGHT > 0d) {
					weight = this.GetAlignmentWeight(other, dist);
					alignmentBiasWeight += weight;
					alignmentBias += other.Velocity.Multiply(weight);
				}

				if (Parameters.ENABLE_SEPARATION && Parameters.SEPARATION_WEIGHT > 0d && dist < Parameters.DEFAULT_SEPARATION) {
					weight = this.GetSeparationWeight(other, dist);
					separationBiasWeight += weight;
					awayVector = this.Coordinates.Subtract(other.Coordinates);
					separationBias += awayVector * weight;
				}
			}

			if (count > 0) {
				if (cohesionBiasWeight > 0d) cohesionBias /= cohesionBiasWeight;
				if (alignmentBiasWeight > 0d) alignmentBias /= alignmentBiasWeight;
				if (separationBiasWeight > 0d) separationBias /= separationBiasWeight;

				return
					(cohesionBias * Parameters.COHESION_WEIGHT).Coordinates.Clamp(Parameters.MAX_IMPULSE_COHESION)
					.Addition(alignmentBias * Parameters.ALIGNMENT_WEIGHT).Clamp(Parameters.MAX_IMPULSE_ALIGNMENT)
					.Addition(separationBias* Parameters.SEPARATION_WEIGHT).Clamp(Parameters.MAX_IMPULSE_SEPARATION)
					.Divide(this.Mass)
					.Clamp(Parameters.MAX_ACCELERATION);
			} else
				return new double[Parameters.DOMAIN_DOUBLE.Length];
		}

		public double GetCohesionWeight(Boid other, double dist) {
			return this.Flock.ID == other.Flock.ID ? dist > Parameters.DEFAULT_SEPARATION ? 1d / (dist - Parameters.DEFAULT_SEPARATION/2d) : Parameters.COHESION_WEIGHT : 0d;
		}

		public double GetAlignmentWeight(Boid other, double dist) {
			return this.Flock.ID == other.Flock.ID ? dist > Parameters.DEFAULT_SEPARATION ? 1d / Math.Sqrt(dist) : Parameters.MAX_IMPULSE_ALIGNMENT : 0d;
		}

		public double GetSeparationWeight(Boid other, double dist) {
			return dist <= 0d ? Parameters.MAX_IMPULSE_SEPARATION : 1d / dist;
		}
	}
}