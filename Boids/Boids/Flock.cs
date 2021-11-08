using System;
using System.Linq;
using Generic;

namespace Simulation.Boids {
	public class Flock : IEquatable<Flock> {
		private static int _id = 0;
		public readonly int ID = ++_id;

		public double Separation = Parameters.DEFAULT_SEPARATION;
		public double SeparationWeight = Parameters.DEFAULT_SEPARATION_WEIGHT;
		public double AlignmentWeight = Parameters.DEFAULT_ALIGNMENT_WEIGHT;
		public double CohesionWeight = Parameters.DEFAULT_COHESION_WEIGHT;
		public double SpeedDecay = Math.Exp(Parameters.DEFAULT_SPEED_DECAY);

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
						.Multiply(random.NextDouble() * Parameters.DEFAULT_MAX_STARTING_SPEED)))//scale by a random speed
				.ToArray();
		}

		public bool Equals(Flock other) {
			return this.ID == other.ID;
		}
		public override string ToString() {
			return string.Format("{0}[ID {1}]", nameof(Flock), this.ID);
		}
	}
}