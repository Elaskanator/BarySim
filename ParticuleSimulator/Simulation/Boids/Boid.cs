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

		public double FlockSeparation => this.IsPredator ? Parameters.BOIDS_PREDATOR_GROUP_AVOID_DIST : Parameters.BOIDS_BOID_GROUP_AVOID_DIST;
		public double SeparationDist => this.IsPredator ? Parameters.BOIDS_PREDATOR_MIN_DIST : Parameters.BOIDS_BOID_MIN_DIST;
		public double CohesionDist => this.IsPredator ? Parameters.BOIDS_PREDATOR_COHESION_DIST : Parameters.BOIDS_BOID_COHESION_DIST;
		public double Vision => this.IsPredator ? Parameters.BOIDS_PREDATOR_VISION : Parameters.BOIDS_BOID_VISION;
		public double FoV => this.IsPredator ? Parameters.BOIDS_PREDATOR_FOV_RADIANS : Parameters.BOIDS_BOID_FOV_RADIANS;

		public double RepulsionWeight => this.IsPredator ? Parameters.BOIDS_PREDATOR_DISPERSE_W : Parameters.BOIDS_BOID_DISPERSE_W;
		public double CohesionWeight => this.IsPredator ? Parameters.BOIDS_PREDATOR_COHESION_W : Parameters.BOIDS_BOID_COHESION_W;
		public double AlignmentWeight => this.IsPredator ? Parameters.BOIDS_PREDATOR_ALIGNMENT_W : Parameters.BOIDS_BOID_ALIGNMENT_W;

		protected override void AfterUpdate() {
			double speed;
			if ((speed = this.Velocity.Magnitude()) <= Parameters.WORLD_EPSILON)
				this.Velocity = HyperspaceFunctions.RandomUnitVector_Spherical(Parameters.DIM, Program.Random).Multiply(Parameters.BOIDS_BOID_MAX_SPEED * Program.Random.NextDouble());

			this.Velocity = this.MinSpeed >= 0 && speed < this.MinSpeed
				? speed < Parameters.WORLD_EPSILON
					? HyperspaceFunctions.RandomUnitVector_Spherical(Parameters.DIM, Program.Random).Multiply(this.MaxSpeed)
					: this.Velocity.Multiply(this.MinSpeed / speed)
				: this.MaxSpeed >= 0 && speed > this.MaxSpeed
					? this.Velocity.Multiply(this.MaxSpeed / speed)
					: this.Velocity;
		}
	}
}