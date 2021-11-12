using System;
using Generic.Extensions;
using System.Linq;
using Generic.Models;
using System.Collections.Generic;

namespace ParticleSimulator.Simulation.Boids {
	public class Boid: AParticle {
		private static double _speedDecay;
		private static double _predatorSpeedDecay;
		public Boid(int groupID, double[] position, double[] velocity)
		: base(groupID, position, velocity) {
			this.IsPredator = Program.Random.NextDouble() < Parameters.BOIDS_PREDATOR_CHANCE;
		}
		static Boid() {
			_speedDecay = Math.Exp(-Parameters.BOIDS_SPEED_DECAY);
			_predatorSpeedDecay = Math.Exp(-Parameters.BOIDS_PREDATOR_SPEED_DECAY);
		}
		
		public bool IsPredator { get; private set; }
		public double Vision => this.IsPredator ? Parameters.BOIDS_PREDATOR_VISION : Parameters.BOIDS_BOID_VISION;
		public double MaxSpeed => this.IsPredator ? Parameters.BOIDS_PREDATOR_MAX_SPEED : Parameters.BOIDS_MAX_SPEED;
		public override double SpeedDecay => this.IsPredator ? _predatorSpeedDecay : _speedDecay;

		public override void Interact(IEnumerable<AParticle> others) {
			if (Parameters.DESIRED_INTERACTION_NEIGHBORS == 0) return;

			AParticle target = this.IsPredator
				? others.Take(Parameters.DESIRED_INTERACTION_NEIGHBORS < 0 ? 1 : Parameters.DESIRED_INTERACTION_NEIGHBORS)
					.MinBy(p => this.TrueCoordinates.Distance(p.TrueCoordinates))
				: null;

			int count = 0;
			foreach (Boid other in others.Cast<Boid>()) {
				if (other.IsPredator || target is null || target.ID == other.ID) {
					if (Parameters.BOIDS_BOID_FOV_DEGREES < 0 || Parameters.BOIDS_BOID_FOV_DEGREES > Math.Abs(this.Velocity.AngleTo(other.TrueCoordinates))) {
						this.InteractInternal(other);
						if (++count >= Parameters.DESIRED_INTERACTION_NEIGHBORS && Parameters.DESIRED_INTERACTION_NEIGHBORS > 0)
							return;
					}
				}

				if (other.IsPredator && !this.IsPredator)
					return;
			}
		}
		private void InteractInternal(Boid other) {
			double[] vectorAway = this.TrueCoordinates.Subtract(other.TrueCoordinates);
			double dist = vectorAway.Magnitude();

			if (dist < this.Vision) {
				double[] result = new double[this.DIMENSIONALITY];
				double
					minDist = 0d,
					attractionWeight = 0d,
					separationWeight = 0d;

				if (this.IsPredator && (other).IsPredator) {
					minDist = Parameters.BOIDS_MIN_SEPARATION_DIST;
					separationWeight = Parameters.BOIDS_SEPARATION_WEIGHT;
				} else if (this.IsPredator) {
					attractionWeight = Parameters.BOIDS_PREDATOR_COHESION_WEIGHT;
				} else if ((other).IsPredator) {
					minDist = Parameters.BOIDS_BOID_VISION;
					separationWeight = Parameters.BOIDS_PREDATOR_SEPARATION_WEIGHT;
				} else {
					if (this.GroupID == other.GroupID) {
						minDist = Parameters.BOIDS_MIN_SEPARATION_DIST;
						separationWeight = Parameters.BOIDS_SEPARATION_WEIGHT;
					} else {
						minDist = Parameters.BOIDS_GROUP_AVOIDANCE_DIST;
						separationWeight = Parameters.BOIDS_GROUP_SEPARATION_WEIGHT;
					}

					if (this.GroupID == other.GroupID && dist > minDist) {
						separationWeight = 0d;
						if (dist > Parameters.BOIDS_MAX_COHESION_DIST)
							attractionWeight = Parameters.BOIDS_COHESION_WEIGHT;
						else if (Parameters.BOIDS_ENABLE_ALIGNMENT && Parameters.BOIDS_ALIGNMENT_WEIGHT > 0d)
							result = result.Add(
								other.Velocity.Subtract(this.Velocity)
								.Multiply(Parameters.BOIDS_ALIGNMENT_WEIGHT));
					}
				}

				if (dist < minDist)
					if (Parameters.BOIDS_ENABLE_SEPARATION && separationWeight > 0d && dist > Parameters.WORLD_EPSILON)
						result = result.Add(
							vectorAway.Normalize().Multiply(Parameters.BOIDS_MAX_SPEED * (minDist - dist))
							.Multiply(separationWeight));

				if (Parameters.BOIDS_ENABLE_COHESION && attractionWeight > 0d)
					result = result.Subtract(
						vectorAway.Multiply(attractionWeight));

				this.Acceleration = this.Acceleration.Add(result);
			}
		}

		public override void AfterInteract() {
			double speed;
			while ((speed = this.Velocity.Magnitude()) == 0)
				this.Velocity = NumberExtensions.RandomUnitVector_Spherical(Parameters.DOMAIN.Length, Program.Random).Multiply(Parameters.BOIDS_MAX_SPEED * Program.Random.NextDouble());

			this.Velocity = speed < Parameters.BOIDS_MIN_SPEED
				? this.Velocity.Multiply(Parameters.BOIDS_MIN_SPEED / speed)
				: speed > this.MaxSpeed
					? this.Velocity.Multiply(this.MaxSpeed / speed)
					: this.Velocity;
		}
	}
}