using Generic.Extensions;
using Generic.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ParticleSimulator.Simulation {
	public abstract class AParticleGroup<P> : IEquatable<AParticleGroup<P>>, IEqualityComparer<AParticleGroup<P>>
	where P : AParticle {
		private static int _id = 0;
		public readonly int ID = ++_id;

		public P[] Particles { get; private set; }

		public abstract P NewParticle(double[] position, double[] velocity, Random rand);

		public AParticleGroup(Random random) {
			random ??= new Random();

			double[] startingPosition = Enumerable
				.Range(0, Parameters.DOMAIN.Length)
				.Select(d => random.NextDouble() * Parameters.DOMAIN[d])
				.ToArray();

			double boidVolume = NumberExtensions.HypersphereVolume(4d * Parameters.SEPARATION, Parameters.DOMAIN.Length);
			double radius = NumberExtensions.HypersphereRadius(boidVolume * Parameters.NUM_PARTICLES_PER_GROUP, Parameters.DOMAIN.Length);

			this.Particles = Enumerable
				.Range(0, Parameters.NUM_PARTICLES_PER_GROUP)
				.Select(d => this.NewParticle(
					position:startingPosition.Zip(
						NumberExtensions.Random_Spherical(radius, Parameters.DOMAIN.Length, random),
						(a, b) => a + b).ToArray(),
					velocity:VectorFunctions.Multiply(
						VectorFunctions.Normalize(Enumerable
						.Range(0, Parameters.DOMAIN.Length)
						.Select(d => (random.NextDouble() * 2d) - 1d).ToArray()//random between -1 and +1
						.ToArray()),
						random.NextDouble() * Parameters.MAX_STARTING_SPEED).ToArray(),
					random))
				.ToArray();
		}

		public bool Equals(AParticleGroup<P> other) { return !(other is null) && this.ID == other.ID; }
		public override bool Equals(object other) { return !(other is null) && (other is AParticleGroup<P>) && this.ID == (other as AParticleGroup<P>).ID; }
		public bool Equals(AParticleGroup<P> x, AParticleGroup<P> y) { return x.ID == y.ID; }
		public int GetHashCode(AParticleGroup<P> obj) { return obj.ID.GetHashCode(); }
		public override int GetHashCode() { return this.ID.GetHashCode(); }
		public override string ToString() { return string.Format("{0}[ID {1}]", nameof(AParticleGroup<P>), this.ID); }
	}
}