using System.Collections.Generic;
using System.Numerics;
using System.Linq;
using Generic.Vectors;

namespace ParticleSimulator.Simulation.Boids {
	public class Boid : ASimulationParticle<Boid> {
		public Boid(int groupID, Vector<float> position, Vector<float> velocity, float corruption)
		: base(groupID, position, velocity) {
			this.IsPredator = (float)Program.Random.NextDouble() * Parameters.BOIDS_PREDATOR_CHANCE < corruption;
			this.Acceleration = Vector<float>.Zero;
		}

		public Vector<float> Acceleration { get; set; }

		public bool IsPredator { get; private set; }

		public float Vision => this.IsPredator ? Parameters.BOIDS_PREDATOR_VISION : Parameters.BOIDS_BOID_VISION;
		public float FoV => this.IsPredator ? Parameters.BOIDS_PREDATOR_FOV_RADIANS : Parameters.BOIDS_BOID_FOV_RADIANS;

		public float MinSpeed => this.IsPredator ? Parameters.BOIDS_PREDATOR_MIN_SPEED : Parameters.BOIDS_BOID_MIN_SPEED;
		public float MaxSpeed => this.IsPredator ? Parameters.BOIDS_PREDATOR_MAX_SPEED : Parameters.BOIDS_BOID_MAX_SPEED;

		public float FlockDist => this.IsPredator ? Parameters.BOIDS_PREDATOR_GROUP_REPULSION_DIST : Parameters.BOIDS_BOID_GROUP_REPULSION_DIST;
		public float NeighborDist => this.IsPredator ? Parameters.BOIDS_PREDATOR_REPULSION_DIST : Parameters.BOIDS_BOID_REPULSION_DIST;
		public float CohesionDist => this.IsPredator ? Parameters.BOIDS_PREDATOR_COHESION_DIST : Parameters.BOIDS_BOID_COHESION_DIST;

		public float NeighborRepulsionWeight => this.IsPredator ? Parameters.BOIDS_PREDATOR_REPULSION_W : Parameters.BOIDS_BOID_REPULSION_W;
		public float GroupRepulsionWeight => this.IsPredator ? Parameters.BOIDS_PREDATOR_GROUP_REPULSION_W : Parameters.BOIDS_BOID_GROUP_REPULSION_W;
		public float CohesionWeight => this.IsPredator ? Parameters.BOIDS_PREDATOR_COHESION_W : Parameters.BOIDS_BOID_COHESION_W;
		public float AlignmentWeight => this.IsPredator ? Parameters.BOIDS_PREDATOR_ALIGNMENT_W : Parameters.BOIDS_BOID_ALIGNMENT_W;

		public Vector<float> ComputeAcceleration(Boid[] others) {
			VectorIncrementalWeightedAverage centerAvg = new(), directionAvg = new();
			Vector<float> awayVector, repulsion = Vector<float>.Zero;
			float dist, repulsionDist, repulsionWeight, cohesionDist;
			Boid other;
			for (int i = 0; i < others.Length; i++) {
				other = others[i];
				if (this.IsPredator == other.IsPredator) {
					if (this.GroupID == other.GroupID) {
						repulsionDist = this.NeighborDist;
						repulsionWeight = this.NeighborRepulsionWeight;
						cohesionDist = this.CohesionDist;
					} else {
						repulsionDist = this.FlockDist;
						repulsionWeight = this.GroupRepulsionWeight;
						cohesionDist = float.PositiveInfinity;
					}
				} else if (this.IsPredator) {//chase
					repulsionDist = 0f;
					repulsionWeight = 0f;
					cohesionDist = float.PositiveInfinity;
				} else {//flee
					repulsionDist = float.PositiveInfinity;
					repulsionWeight = Parameters.BOIDS_BOID_FLEE_REPULSION_W;
					cohesionDist = float.PositiveInfinity;
				}

				awayVector = this.Position - other.Position;
				dist = awayVector.Magnitude(Parameters.DIM);

				if ((this.Vision < 0f || this.Vision <= dist))
					if (dist < Parameters.WORLD_EPSILON || this.FoV < 0f || this.FoV >= this.Position.AngleTo_FullRange(other.Position, Parameters.DIM))
						if (dist < cohesionDist)
							if (dist < repulsionDist)
								repulsion = repulsion + (awayVector * (repulsionWeight * (1f - dist/repulsionDist)));
							else directionAvg.Update(other.Velocity);
						else centerAvg.Update(other.Position);
			}
						
			return (Parameters.BOIDS_REPULSION_ENABLE ? repulsion : Vector<float>.Zero)
				+ (Parameters.BOIDS_COHERE_ENABLE && centerAvg.NumUpdates > 0
					? (centerAvg.Current - this.Position).Normalize(Parameters.DIM, this.CohesionWeight)
					: Vector<float>.Zero)
				+ (Parameters.BOIDS_ALIGN_ENABLE && directionAvg.NumUpdates > 0 && directionAvg.Current.Magnitude(Parameters.DIM) > Parameters.WORLD_EPSILON
					? (directionAvg.Current - this.Velocity) * this.AlignmentWeight
					: Vector<float>.Zero);
		}

		protected override IEnumerable<Boid> AfterUpdate() {
			float speed;
			if ((speed = this.Velocity.Magnitude(Parameters.DIM)) <= Parameters.WORLD_EPSILON)
				this.Velocity = VectorFunctions.New(VectorFunctions.RandomUnitVector_Spherical(Parameters.DIM, Program.Random).Select(x => (float)x * Parameters.BOIDS_BOID_MAX_SPEED * (float)Program.Random.NextDouble()));

			this.Velocity = this.MinSpeed >= 0f && speed < this.MinSpeed
				? speed < Parameters.WORLD_EPSILON
					? VectorFunctions.New(VectorFunctions.RandomUnitVector_Spherical(Parameters.DIM, Program.Random).Select(x => (float)x * this.MaxSpeed))
					: this.Velocity * (this.MinSpeed / speed)
				: this.MaxSpeed >= 0f && speed > this.MaxSpeed
					? this.Velocity * (this.MaxSpeed / speed)
					: this.Velocity;

			return new Boid[] { this };
		}
	}
}