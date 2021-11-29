using System;
using System.Collections.Generic;
using System.Linq;
using Generic.Extensions;
using Generic.Models.Vectors;

namespace ParticleSimulator.Simulation.Boids {
	public class Boid : AParticle {
		private static double _speedDecay;
		private static double _predatorSpeedDecay;
		public Boid(int groupID, double[] position, double[] velocity, double corruption)
		: base(groupID, position, velocity) {
			this.IsPredator = Program.Random.NextDouble() < corruption;
			this._accumDisperse = new double[Parameters.DIM];
			this._accumCohere = new double[Parameters.DIM];
			this._cohesionInteractions = 0;
			this._accumAlign = new double[Parameters.DIM];
			this._alignInteractions = 0;
		}
		static Boid() {
			_speedDecay = Math.Exp(-Parameters.BOIDS_BOID_SPEED_DECAY);
			_predatorSpeedDecay = Math.Exp(-Parameters.BOIDS_PREDATOR_SPEED_DECAY);
		}
		
		public bool IsPredator { get; private set; }

		public override double SpeedDecay => this.IsPredator ? _predatorSpeedDecay : _speedDecay;
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

		private double[] _accumDisperse;
		private double[] _accumCohere;
		private int _cohesionInteractions;
		private double[] _accumAlign;
		private int _alignInteractions;
		public override double[] Acceleration {
			get { return
				this._accumDisperse.Multiply(this.DisperseWeight)
				.Add(this._accumCohere.Multiply(this.CohesionWeight / (this._cohesionInteractions > 0 ? this._cohesionInteractions : 1)))
				.Add(this._accumAlign.Multiply(this.AlignmentWeight / (this._alignInteractions > 0 ? this._alignInteractions : 1))); }
			set {
				this._accumDisperse = new double[Parameters.DIM];
				this._accumCohere = new double[Parameters.DIM];
				this._cohesionInteractions = 0;
				this._accumAlign = new double[Parameters.DIM];
				this._alignInteractions = 0;
		}}

		protected override IEnumerable<AParticle> Filter(IEnumerable<AParticle> others) {
			if (this.FoV > 0)
				return others.Where(p => Math.Abs(this.Velocity.AngleTo(p.LiveCoordinates)) < this.FoV/2d);
			else return others;
		}
		protected override void Interact(AParticle other) {
			double[] vectorAway = this.LiveCoordinates.Subtract(other.LiveCoordinates);
			double dist = vectorAway.Magnitude();

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
							cohesionWeight = 1d;
							if (dist < cohesionDist)
								alignmentWeight = 1d;
						} else dispersionWeight = 1d;
					} else {
						minDist = this.FlockDispersalRadius;
						dispersionWeight = this.ForeignFlockDislike;
					}
				} else if (this.IsPredator) {
					minDist = 1d;
					cohesionDist = 2d;
					cohesionWeight = Parameters.BOIDS_CHASE_WE;
				} else if (((Boid)other).IsPredator) {
					minDist = this.Vision;
					dispersionWeight = Parameters.BOIDS_FLEE_WE;
					cohesionWeight = 1d;
					alignmentWeight = -1d;
				}

				if (Parameters.BOIDS_ENABLE_ALIGNMENT && alignmentWeight != 0d) {
					this._accumAlign = this._accumAlign.Add(other.Velocity.Subtract(this.Velocity).Multiply(alignmentWeight));
					this._alignInteractions++;
				}
				
				if (Parameters.BOIDS_ENABLE_SEPARATION && dispersionWeight != 0d && dist > Parameters.WORLD_EPSILON)
					this._accumDisperse = this._accumDisperse.Add(
						vectorAway.Normalize().Multiply(this.MaxSpeed * dispersionWeight * (minDist - dist)));

				if (Parameters.BOIDS_ENABLE_COHESION && cohesionWeight != 0d && dist > minDist) {
					if (dist < cohesionDist)
						this._accumCohere = this._accumCohere.Add(
							vectorAway.Normalize()
								.Multiply(-cohesionWeight * this.MaxSpeed * (dist - minDist) / (cohesionDist - minDist)));
					else this._accumCohere = this._accumCohere.Add(
							vectorAway.Normalize()
								.Multiply(-cohesionWeight * this.MaxSpeed));

					this._cohesionInteractions++;
				}
			}
		}

		protected override void AfterInteract() {
			double speed;
			while ((speed = this.Velocity.Magnitude()) == 0)
				this.Velocity = NumberExtensions.RandomUnitVector_Spherical(Parameters.DIM, Program.Random).Multiply(Parameters.BOIDS_BOID_MAX_SPEED * Program.Random.NextDouble());

			this.Velocity = speed < this.MinSpeed
				? this.Velocity.Multiply(this.MinSpeed / speed)
				: speed > this.MaxSpeed
					? this.Velocity.Multiply(this.MaxSpeed / speed)
					: this.Velocity;
		}
	}
}