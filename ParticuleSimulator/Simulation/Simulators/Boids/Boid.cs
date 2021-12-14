using Generic.Vectors;

namespace ParticleSimulator.Simulation.Boids {
	public class Boid : AClassicalParticle {
		public Boid(int groupID, double[] position, double[] velocity, double corruption)
		: base(groupID, position, velocity) {
			this.IsPredator = Program.Random.NextDouble() * Parameters.BOIDS_PREDATOR_CHANCE < corruption;
		}

		public bool IsPredator { get; private set; }

		public double Vision => this.IsPredator ? Parameters.BOIDS_PREDATOR_VISION : Parameters.BOIDS_BOID_VISION;
		public double FoV => this.IsPredator ? Parameters.BOIDS_PREDATOR_FOV_RADIANS : Parameters.BOIDS_BOID_FOV_RADIANS;

		public double MinSpeed => this.IsPredator ? Parameters.BOIDS_PREDATOR_MIN_SPEED : Parameters.BOIDS_BOID_MIN_SPEED;
		public double MaxSpeed => this.IsPredator ? Parameters.BOIDS_PREDATOR_MAX_SPEED : Parameters.BOIDS_BOID_MAX_SPEED;

		public double FlockDist => this.IsPredator ? Parameters.BOIDS_PREDATOR_GROUP_REPULSION_DIST : Parameters.BOIDS_BOID_GROUP_REPULSION_DIST;
		public double NeighborDist => this.IsPredator ? Parameters.BOIDS_PREDATOR_REPULSION_DIST : Parameters.BOIDS_BOID_REPULSION_DIST;
		public double CohesionDist => this.IsPredator ? Parameters.BOIDS_PREDATOR_COHESION_DIST : Parameters.BOIDS_BOID_COHESION_DIST;

		public double NeighborRepulsionWeight => this.IsPredator ? Parameters.BOIDS_PREDATOR_REPULSION_W : Parameters.BOIDS_BOID_REPULSION_W;
		public double GroupRepulsionWeight => this.IsPredator ? Parameters.BOIDS_PREDATOR_GROUP_REPULSION_W : Parameters.BOIDS_BOID_GROUP_REPULSION_W;
		public double CohesionWeight => this.IsPredator ? Parameters.BOIDS_PREDATOR_COHESION_W : Parameters.BOIDS_BOID_COHESION_W;
		public double AlignmentWeight => this.IsPredator ? Parameters.BOIDS_PREDATOR_ALIGNMENT_W : Parameters.BOIDS_BOID_ALIGNMENT_W;

		public double[] ComputeInteractionForce(Boid[] others) {
			VectorIncrementalAverage centerAvg = new(), directionAvg = new();
			double[] awayVector, repulsion = new double[Parameters.DIM];
			double dist, repulsionDist, repulsionWeight, cohesionDist;
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
						cohesionDist = double.PositiveInfinity;
					}
				} else if (this.IsPredator) {//chase
					repulsionDist = 0d;
					repulsionWeight = 0d;
					cohesionDist = double.PositiveInfinity;
				} else {//flee
					repulsionDist = double.PositiveInfinity;
					repulsionWeight = Parameters.BOIDS_BOID_FLEE_REPULSION_W;
					cohesionDist = double.PositiveInfinity;
				}

				awayVector = this.LiveCoordinates.Subtract(other.LiveCoordinates);
				dist = awayVector.Magnitude();

				if ((this.Vision < 0d || this.Vision <= dist))
					if (dist < Parameters.WORLD_EPSILON || this.FoV < 0d || this.FoV >= this.LiveCoordinates.AngleTo_FullRange(other.LiveCoordinates))
						if (dist < cohesionDist)
							if (dist < repulsionDist)
								repulsion = repulsion.Add(awayVector.Multiply(repulsionWeight * (1d - dist/repulsionDist)));
							else directionAvg.Update(other.Velocity);
						else centerAvg.Update(other.LiveCoordinates);
			}
						
			return (Parameters.BOIDS_REPULSION_ENABLE ? repulsion : new double[Parameters.DIM])
				.Add(Parameters.BOIDS_COHERE_ENABLE && centerAvg.NumUpdates > 0 ? centerAvg.Current.Subtract(this.LiveCoordinates).Normalize().Multiply(this.CohesionWeight) : new double[Parameters.DIM])
				.Add(Parameters.BOIDS_ALIGN_ENABLE && directionAvg.NumUpdates > 0 && directionAvg.Current.Magnitude() > Parameters.WORLD_EPSILON ? directionAvg.Current.Subtract(this.Velocity).Multiply(this.AlignmentWeight) : new double[Parameters.DIM]);
		}

		protected override void AfterUpdate() {
			double speed;
			if ((speed = this.Velocity.Magnitude()) <= Parameters.WORLD_EPSILON)
				this.Velocity = HyperspaceFunctions.RandomUnitVector_Spherical(Parameters.DIM, Program.Random).Multiply(Parameters.BOIDS_BOID_MAX_SPEED * Program.Random.NextDouble());

			this.Velocity = this.MinSpeed >= 0d && speed < this.MinSpeed
				? speed < Parameters.WORLD_EPSILON
					? HyperspaceFunctions.RandomUnitVector_Spherical(Parameters.DIM, Program.Random).Multiply(this.MaxSpeed)
					: this.Velocity.Multiply(this.MinSpeed / speed)
				: this.MaxSpeed >= 0d && speed > this.MaxSpeed
					? this.Velocity.Multiply(this.MaxSpeed / speed)
					: this.Velocity;
		}
	}
}