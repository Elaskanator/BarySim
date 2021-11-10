using Generic.Extensions;
using Generic.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ParticleSimulator.Simulation {
	public abstract class AParticleGroup<N, T> : IEquatable<AParticleGroup<N, T>>, IEqualityComparer<AParticleGroup<N, T>>
	where N : IComparable<N>
	where T : AParticle<N> {
		private static int _id = 0;
		public readonly int ID = ++_id;

		public T[] Particles { get; private set; }

		public abstract T NewParticle(double[] position, double[] velocity, Random rand);

		public AParticleGroup(Random random) {
			random ??= new Random();

			double[] startingPosition = Enumerable
				.Range(0, Parameters.DOMAIN_DOUBLE.Length)
				.Select(d => random.NextDouble() * Parameters.DOMAIN_DOUBLE[d])
				.ToArray();

			double boidVolume = NumberExtensions.HypersphereVolume(4d * Parameters.DEFAULT_SEPARATION, Parameters.DOMAIN_DOUBLE.Length);
			double radius = NumberExtensions.HypersphereRadius(boidVolume * Parameters.NUM_PARTICLES_PER_GROUP, Parameters.DOMAIN_DOUBLE.Length);

			this.Particles = Enumerable
				.Range(0, Parameters.NUM_PARTICLES_PER_GROUP)
				.Select(d => this.NewParticle(
					startingPosition.Zip(
						NumberExtensions.Random_Spherical(radius, Parameters.DOMAIN_DOUBLE.Length, random),
						(a, b) => a + b).ToArray(),
					VectorFunctions.Multiply(
						VectorFunctions.Normalize(Enumerable
						.Range(0, Parameters.DOMAIN_DOUBLE.Length)
						.Select(d => (random.NextDouble() * 2d) - 1d).ToArray()//random between -1 and +1
						.ToArray()),
						random.NextDouble() * Parameters.MAX_STARTING_SPEED).ToArray(),
					random))
				.ToArray();
		}

		public bool Equals(AParticleGroup<N, T> other) { return !(other is null) && this.ID == other.ID; }
		public override bool Equals(object other) { return !(other is null) && (other is AParticleGroup<N, T>) && this.ID == (other as AParticleGroup<N, T>).ID; }
		public bool Equals(AParticleGroup<N, T> x, AParticleGroup<N, T> y) { return x.ID == y.ID; }
		public int GetHashCode(AParticleGroup<N, T> obj) { return obj.ID.GetHashCode(); }
		public override int GetHashCode() { return this.ID.GetHashCode(); }
		public override string ToString() { return string.Format("{0}[ID {1}]", nameof(AParticleGroup<N, T>), this.ID); }
	}
}