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

		public AParticleGroup() {
			this.ID = _globalID++;
		}

		public void Init(double[] center) {
			double particleVolume = NumberExtensions.HypersphereVolume(Parameters.INITIAL_SEPARATION * 2d, Parameters.DOMAIN.Length);
			double radius = NumberExtensions.HypersphereRadius(particleVolume * Parameters.NUM_PARTICLES_PER_GROUP, Parameters.DOMAIN.Length);

			double startingSpeedRange = Parameters.MAX_STARTING_SPEED < 0
				? 0d
				: Parameters.MAX_STARTING_SPEED;
			this.Particles = Enumerable
				.Range(0, Parameters.NUM_PARTICLES_PER_GROUP)
				.Select(d => {
					double direction = startingSpeedRange < 0 ? 0 : 2d * Math.PI * Program.Random.NextDouble();
					return this.NewParticle(
						position:center.Zip(
							NumberExtensions.RandomCoordinate_Spherical(radius, Parameters.DOMAIN.Length, Program.Random),
							(a, b) => a + b).ToArray(),
						velocity:startingSpeedRange <= 0
							? new double[Parameters.DOMAIN.Length]
							: NumberExtensions.RandomUnitVector_Spherical(Parameters.DOMAIN.Length, Program.Random).Multiply(startingSpeedRange * Program.Random.NextDouble()),
						Program.Random);
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