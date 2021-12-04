using System;
using System.Collections.Generic;
using System.Linq;
using Generic.Vectors;

namespace ParticleSimulator.Simulation {
	public abstract class AParticleGroup<P> : IEquatable<AParticleGroup<P>>, IEqualityComparer<AParticleGroup<P>>
	where P : AParticle {
		public AParticleGroup() {
			this.NumParticles = Parameters.PARTICLES_GROUP_MIN + (int)Math.Round(Math.Pow(Program.Random.NextDouble(), Parameters.PARTICLES_GROUP_SIZE_SKEW_POWER) * (Parameters.PARTICLES_GROUP_MAX - Parameters.PARTICLES_GROUP_MIN));
			if (Parameters.PARTICLES_GROUP_COUNT < 2)
				this.SpawnCenter = Parameters.DOMAIN_CENTER;
			else this.SpawnCenter = Parameters.DOMAIN_SIZE
				.Select(x =>
					x * (Program.Random.NextDouble() * (100d - Parameters.WORLD_PADDING_PCT) + 0.5d * Parameters.WORLD_PADDING_PCT) / 100d)
				.ToArray();
			this.InitialVelocity = this.NewInitialDirection(Parameters.DOMAIN_CENTER, this.SpawnCenter).Multiply(Parameters.PARTICLES_MAX_GROUP_STARTING_SPEED)
				.Add(HyperspaceFunctions.RandomUnitVector_Spherical(Parameters.DIM, Program.Random).Multiply(Parameters.PARTICLES_MAX_STARTING_SPEED));

			double particleVolume = HyperspaceFunctions.HypersphereVolume(this.InitialSeparationRadius * Math.Pow(2d, 1d / Parameters.DIM), Parameters.DIM);
			double radius = this.NumParticles > 1 ? HyperspaceFunctions.HypersphereRadius(particleVolume * this.NumParticles, Parameters.DIM) : 0d;
			this.Particles = Enumerable
				.Range(0, this.NumParticles)
				.Select(i => this.NewParticlePosition(this.SpawnCenter, radius))
				.Select(p => this.NewParticle(
					p,
					this.InitialVelocity
						.Add(this.NewInitialDirection(this.SpawnCenter, p).Multiply(Parameters.PARTICLES_MAX_GROUP_STARTING_SPEED))
						.Add(HyperspaceFunctions.RandomUnitVector_Spherical(Parameters.DIM, Program.Random).Multiply(Parameters.PARTICLES_MAX_STARTING_SPEED))))
				.ToArray();
		}

		private static int _globalID = 0;
		public readonly int ID = _globalID++;
		public readonly double[] SpawnCenter;
		public readonly double[] InitialVelocity;
		public readonly int NumParticles;
		public P[] Particles { get; private set; }
		protected abstract double InitialSeparationRadius { get; }

		protected abstract P NewParticle(double[] position, double[] velocity);

		protected virtual double[] NewParticlePosition(double[] center, double radius) {
			return center.Add(HyperspaceFunctions.RandomCoordinate_Spherical(radius, Parameters.DIM, Program.Random));
		}

		protected virtual double[] NewInitialDirection(double[] center, double[] position) {
			return HyperspaceFunctions.RandomUnitVector_Spherical(Parameters.DIM, Program.Random);
		}

		public bool Equals(AParticleGroup<P> other) { return !(other is null) && this.ID == other.ID; }
		public override bool Equals(object other) { return !(other is null) && (other is AParticleGroup<P>) && this.ID == (other as AParticleGroup<P>).ID; }
		public bool Equals(AParticleGroup<P> x, AParticleGroup<P> y) { return x.ID == y.ID; }
		public int GetHashCode(AParticleGroup<P> obj) { return obj.ID.GetHashCode(); }
		public override int GetHashCode() { return this.ID.GetHashCode(); }
		public override string ToString() { return string.Format("{0}[ID {1}]", nameof(AParticleGroup<P>), this.ID); }
	}
}