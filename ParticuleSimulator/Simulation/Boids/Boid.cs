using System;
using System.Collections.Generic;
using System.Linq;
using Generic.Vectors;

namespace ParticleSimulator.Simulation.Boids {
	public class Boid : AParticle {
		public Boid(int groupID, double[] position, double[] velocity, double corruption)
		: base(groupID, position, velocity) {
			this.IsPredator = Program.Random.NextDouble() * Parameters.BOIDS_PREDATOR_CHANCE < corruption;
		}
		
		public bool IsPredator { get; private set; }

		public double MinSpeed => this.IsPredator ? Parameters.BOIDS_PREDATOR_MIN_SPEED : Parameters.BOIDS_BOID_MIN_SPEED;
		public double MaxSpeed => this.IsPredator ? Parameters.BOIDS_PREDATOR_MAX_SPEED : Parameters.BOIDS_BOID_MAX_SPEED;

		public double MinDist => this.IsPredator ? Parameters.BOIDS_PREDATOR_MIN_DIST : Parameters.BOIDS_BOID_MIN_DIST;
		public double CohesionDist => this.IsPredator ? Parameters.BOIDS_PREDATOR_COHESION_DIST : Parameters.BOIDS_BOID_COHESION_DIST;
		public double Vision => this.IsPredator ? Parameters.BOIDS_PREDATOR_VISION : Parameters.BOIDS_BOID_VISION;
		public double FoV => this.IsPredator ? Parameters.BOIDS_PREDATOR_FOV_RADIANS : Parameters.BOIDS_BOID_FOV_RADIANS;

		public double DisperseWeight => this.IsPredator ? Parameters.BOIDS_PREDATOR_DISPERSE_W : Parameters.BOIDS_BOID_DISPERSE_W;
		public double CohesionWeight => this.IsPredator ? Parameters.BOIDS_PREDATOR_COHESION_W : Parameters.BOIDS_BOID_COHESION_W;
		public double AlignmentWeight => this.IsPredator ? Parameters.BOIDS_PREDATOR_ALIGNMENT_W : Parameters.BOIDS_BOID_ALIGNMENT_W;

		public double FlockDispersalRadius => this.IsPredator ? Parameters.BOIDS_PREDATOR_GROUP_AVOID_DIST : Parameters.BOIDS_BOID_GROUP_AVOID_DIST;
		public double ForeignFlockDislike => this.IsPredator ? Parameters.BOIDS_PREDATOR_GROUP_AVOID_WE : Parameters.BOIDS_BOID_GROUP_AVOID_WE;

		protected override IEnumerable<AParticle> Filter(IEnumerable<AParticle> others) {
			if (this.FoV > 0)
				return others.Where(p => Math.Abs(this.Velocity.AngleTo(p.LiveCoordinates)) < this.FoV/2d);
			else return others;
		}
		public override double[] ComputeInteractionForce(AParticle other) {
			double[] vectorAway = this.LiveCoordinates.Subtract(other.LiveCoordinates);
			double dist = vectorAway.Magnitude();
			double[] result = new double[Parameters.DIM];

			if (dist < this.Vision) {
				double
					minDist = this.MinDist,
					cohesionDist = this.CohesionDist,
					dispersionWeight = 0,
					cohesionWeight = 0,
					alignmentWeight = 0;

				if (this.IsPredator == ((Boid)other).IsPredator) {
					if (this.GroupID == other.GroupID) {
						if (dist > minDist) {
							cohesionWeight = this.CohesionWeight;
							if (dist < cohesionDist)
								alignmentWeight = this.AlignmentWeight;
						} else dispersionWeight = this.DisperseWeight;
					} else {
						minDist = this.FlockDispersalRadius;
						dispersionWeight = this.DisperseWeight * this.ForeignFlockDislike;
					}
				} else if (this.IsPredator) {
					minDist = 1d;
					cohesionDist = 2d;
					cohesionWeight = this.CohesionWeight * Parameters.BOIDS_CHASE_WE;
				} else if (((Boid)other).IsPredator) {
					minDist = this.Vision;
					dispersionWeight = this.DisperseWeight * Parameters.BOIDS_FLEE_WE;
					cohesionWeight = this.CohesionWeight;
				}

				if (Parameters.BOIDS_ENABLE_ALIGNMENT && alignmentWeight != 0d)
					result = result.Add(other.Velocity.Subtract(this.Velocity).Multiply(alignmentWeight));
				
				if (Parameters.BOIDS_ENABLE_SEPARATION && dispersionWeight != 0d && dist > Parameters.WORLD_EPSILON)
					result = result.Add(vectorAway.Normalize(this.MaxSpeed * dispersionWeight * (minDist - dist)));

				if (Parameters.BOIDS_ENABLE_COHESION && cohesionWeight != 0d && dist > minDist) {
					if (dist < cohesionDist)
						result = result.Add(vectorAway.Normalize(-cohesionWeight * this.MaxSpeed * (dist - minDist) / (cohesionDist - minDist)));
					else result = result.Add(vectorAway.Normalize(-cohesionWeight * this.MaxSpeed));
				}
			}

			return result;
		}

		protected override void AfterUpdate() {
			double speed;
			while ((speed = this.Velocity.Magnitude()) == 0)
				this.Velocity = HyperspaceFunctions.RandomUnitVector_Spherical(Parameters.DIM, Program.Random).Multiply(Parameters.BOIDS_BOID_MAX_SPEED * Program.Random.NextDouble());

			this.Velocity = speed < this.MinSpeed
				? this.Velocity.Multiply(this.MinSpeed / speed)
				: speed > this.MaxSpeed
					? this.Velocity.Multiply(this.MaxSpeed / speed)
					: this.Velocity;
		}
	}
}