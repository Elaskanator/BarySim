using System;
using System.Linq;
using Generic;

namespace Boids {
	public class Flock : IEquatable<Flock> {
		private static int _id = 0;
		public readonly int ID = ++_id;

		public Boid[] Boids { get; private set; }

		public Flock(int size, Random random = null) {
			random ??= new Random();

			double[] startingPosition = Enumerable
				.Range(0, Parameters.DOMAIN.Length)
				.Select(d => random.NextDouble() * Parameters.DOMAIN[d])
				.ToArray();

			double boidVolume = NumberExtensions.HypersphereVolume(this.Separation * 2, Parameters.DOMAIN.Length);
			double randomRadius = NumberExtensions.HypersphereRadius(boidVolume * size, Parameters.DOMAIN.Length);

			this.Boids = Enumerable
				.Range(0, size)
				.Select(d => new Boid(
					flock: this,
					startingPosition.Zip(
						NumberExtensions.Random_Spherical(randomRadius, Parameters.DOMAIN.Length, random),
						(a, b) => a + b).ToArray(),
					velocity: Enumerable
						.Range(0, Parameters.DOMAIN.Length)
						.Select(d => (random.NextDouble() * 2d) - 1d).ToArray()//random between -1 and +1
						.Normalize()//unit vector in radom direction (is this uniformly distributed?)
						.Multiply(random.NextDouble() * this.MaxStartingSpeed)))//scale by a random speed
				.ToArray();
		}

		public double Separation = Parameters.DEFAULT_SEPARATION;

		public double SeparationWeight = 2;
		public double AlignmentWeight = 0.5;
		public double CohesionWeight = 0.5;

		public double MaxForce = 0.1;
		public double MaxAcceleration = 0.05;
		public double MaxSpeed = 0.4;
		public double MaxStartingSpeed = 1;

		public double SpeedDecay = Math.Exp(Parameters.DEFAULT_SPEED_DECAY);

		public double MaxImpulse_Cohesion = 1;
		public double MaxImpulse_Alignment = 0.5;
		public double MaxImpulse_Separation = 2;

		public bool Equals(Flock other) {
			return this.ID == other.ID;
		}
	}
}