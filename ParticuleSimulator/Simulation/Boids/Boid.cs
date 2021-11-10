using System;
using System.Collections.Generic;
using System.Linq;
using Generic.Extensions;
using Generic.Models;

namespace ParticleSimulator.Simulation.Boids {
	public class Boid: AParticle {
		public readonly Flock Flock;

		public Boid(Flock flock, double[] position, double[] velocity, double mass = 1)
		: base(position, velocity, mass) {
			this.Flock = flock;
		}
		
		private double[] _velocity;
		public override double[] Velocity {
			get { return this._velocity; }
			set { this._velocity = VectorFunctions.Clamp(value, Parameters.MAX_SPEED); } }
		private double[] _acceleration;
		public double[] Acceleration {
			get { return this._acceleration; }
			set { this._acceleration = VectorFunctions.Clamp(value, Parameters.MAX_ACCELERATION); } }
		public override double Radius => 0f;

		public void Interact(IEnumerable<Boid> neighbors) {
			this.Acceleration = this.ComputeNeighborhoodBias(neighbors);
		}
		public override void Interact(AParticle neighbor) { throw new NotImplementedException(); }

		internal override void ApplyUpdate() {
			this.Velocity = this.Velocity.Multiply(Math.Exp(-Parameters.SPEED_DECAY)).Addition(this.Acceleration);
			this.Coordinates = this.Velocity.Addition(this.Coordinates);
		}

		//TODO rewrite this method entirely
		//seealso https://swharden.com/CsharpDataVis/boids/boids.md.html
		private double[] ComputeNeighborhoodBias(IEnumerable<Boid> neighbors) {
			double cohesionBiasWeight = 0d, alignmentBiasWeight = 0d, separationBiasWeight = 0d;
			double[]
				cohesionBias = new double[this.Dimensionality],
				separationBias = new double[this.Dimensionality],
				alignmentBias = new double[this.Dimensionality];

			double dist;
			double weight;
			double[] awayVector;

			int count = 0;
			foreach (Boid other in neighbors.Except(x => x.ID == this.ID)) {
				if (++count > Parameters.DESIRED_NEIGHBORS)
					break;
				
				dist = VectorFunctions.Distance(this.Coordinates, other.Coordinates);

				if (Parameters.ENABLE_COHESION && Parameters.COHESION_WEIGHT > 0d) {
					weight = this.GetCohesionWeight(other, dist);
					cohesionBiasWeight += weight;
					cohesionBias = cohesionBias.Addition(other.Coordinates.Multiply(weight));
				}

				if (Parameters.ENABLE_ALIGNMENT && Parameters.ALIGNMENT_WEIGHT > 0d) {
					weight = this.GetAlignmentWeight(other, dist);
					alignmentBiasWeight += weight;
					alignmentBias = alignmentBias.Addition(other.Velocity.Multiply(weight));
				}

				if (Parameters.ENABLE_SEPARATION && Parameters.SEPARATION_WEIGHT > 0d && dist < Parameters.SEPARATION) {
					weight = this.GetSeparationWeight(other, dist);
					separationBiasWeight += weight;
					awayVector = this.Coordinates.Subtract(other.Coordinates);
					separationBias = separationBias.Addition(awayVector.Multiply(weight));
				}
			}

			if (count > 0) {
				if (cohesionBiasWeight > 0d) cohesionBias = cohesionBias.Divide(cohesionBiasWeight);
				if (alignmentBiasWeight > 0d) alignmentBias = alignmentBias.Divide(alignmentBiasWeight);
				if (separationBiasWeight > 0d) separationBias = separationBias.Divide(separationBiasWeight);

				return
					((cohesionBias.Multiply(Parameters.COHESION_WEIGHT).Clamp(Parameters.MAX_IMPULSE_COHESION))
						.Addition(alignmentBias.Multiply(Parameters.ALIGNMENT_WEIGHT).Clamp(Parameters.MAX_IMPULSE_ALIGNMENT))
						.Addition(separationBias.Multiply(Parameters.SEPARATION_WEIGHT).Clamp(Parameters.MAX_IMPULSE_SEPARATION)))
					.Divide(this.Mass)
					.Clamp(Parameters.MAX_ACCELERATION);
			} else
				return cohesionBias;
		}

		public double GetCohesionWeight(Boid other, double dist) {
			return this.Flock.ID == other.Flock.ID ? dist > Parameters.SEPARATION ? 1d / (dist - Parameters.SEPARATION/2d) : Parameters.COHESION_WEIGHT : 0d;
		}

		public double GetAlignmentWeight(Boid other, double dist) {
			return this.Flock.ID == other.Flock.ID ? dist > Parameters.SEPARATION ? 1d / Math.Sqrt(dist) : Parameters.MAX_IMPULSE_ALIGNMENT : 0d;
		}

		public double GetSeparationWeight(Boid other, double dist) {
			return dist <= 0d ? Parameters.MAX_IMPULSE_SEPARATION : 1d / dist;
		}
	}
}