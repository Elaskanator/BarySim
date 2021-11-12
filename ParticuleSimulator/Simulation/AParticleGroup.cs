using Generic.Extensions;
using Generic.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ParticleSimulator.Simulation {
	public abstract class AParticleGroup<P> : IEquatable<AParticleGroup<P>>, IEqualityComparer<AParticleGroup<P>>
	where P : AParticle {
		private static int _globalID = 0;
		public int ID { get; }

		public P[] Particles { get; private set; }

		public abstract P NewParticle(double[] position, double[] velocity, Random rand);

		public AParticleGroup(Random rand) {
			this.ID = ++_globalID;

			rand ??= new Random();

			double[] startingPosition = Enumerable
				.Range(0, Parameters.DOMAIN.Length)
				.Select(d => rand.NextDouble() * Parameters.DOMAIN[d])
				.ToArray();

			double boidVolume = NumberExtensions.HypersphereVolume(4d * Parameters.INITIAL_SEPARATION, Parameters.DOMAIN.Length);
			double radius = NumberExtensions.HypersphereRadius(boidVolume * Parameters.NUM_PARTICLES_PER_GROUP, Parameters.DOMAIN.Length);

			double startingSpeedRange = Parameters.MAX_STARTING_SPEED < 0
				? 0d
				: Parameters.MAX_STARTING_SPEED;
			this.Particles = Enumerable
				.Range(0, Parameters.NUM_PARTICLES_PER_GROUP)
				.Select(d => {
					double direction = startingSpeedRange < 0 ? 0 : 2d * Math.PI * rand.NextDouble();
					return this.NewParticle(
						position:startingPosition.Zip(
							NumberExtensions.RandomCoordinate_Spherical(radius, Parameters.DOMAIN.Length, rand),
							(a, b) => a + b).ToArray(),
						velocity:startingSpeedRange <= 0
							? new double[Parameters.DOMAIN.Length]
							: NumberExtensions.RandomUnitVector_Spherical(Parameters.DOMAIN.Length, rand).Multiply(startingSpeedRange * rand.NextDouble()),
						rand);
					})
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