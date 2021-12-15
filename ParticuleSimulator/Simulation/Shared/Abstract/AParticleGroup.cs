using System;
using System.Collections.Generic;
using System.Linq;
using Generic.Vectors;

namespace ParticleSimulator.Simulation {
	public interface IParticleGroup : IEquatable<IParticleGroup>, IEqualityComparer<IParticleGroup> {
		public int ID { get; }
		public IParticle[] MemberParticles { get; }
	}

	public abstract class AParticleGroup<TParticle> : IParticleGroup
	where TParticle : AParticle<TParticle> {
		public AParticleGroup() {
			this.NumParticles = Parameters.PARTICLES_GROUP_MIN + (int)Math.Round(Math.Pow(Program.Random.NextDouble(), Parameters.PARTICLES_GROUP_SIZE_SKEW_POWER) * (Parameters.PARTICLES_GROUP_MAX - Parameters.PARTICLES_GROUP_MIN));

			if (Parameters.PARTICLES_GROUP_COUNT < 2)
				this.SpawnCenter = Parameters.DOMAIN_CENTER;
			else this.SpawnCenter = Parameters.DOMAIN_SIZE
				.Select(x => x * (Program.Random.NextDouble() * (100d - Parameters.WORLD_PADDING_PCT) + 0.5d * Parameters.WORLD_PADDING_PCT) / 100d)
				.ToArray();

			this.InitialVelocity = this.NewInitialDirection(Parameters.DOMAIN_CENTER, this.SpawnCenter).Multiply(this.StartSpeedMax_Group_Angular)
				.Add(HyperspaceFunctions.RandomUnitVector_Spherical(Parameters.DIM, Program.Random).Multiply(this.StartSpeedMax_Group_Rand));

			double particleVolume = HyperspaceFunctions.HypersphereVolume(this.InitialSeparationRadius, Parameters.DIM);
			double radius = this.NumParticles > 1 ? HyperspaceFunctions.HypersphereRadius(particleVolume * this.NumParticles, Parameters.DIM) : 0d;
			this.MemberParticles = Enumerable
				.Range(0, this.NumParticles)
				.Select(i => this.NewParticlePosition(this.SpawnCenter, radius))
				.Select(p => this.NewParticle(
					p,
					this.InitialVelocity
						.Add(this.NewInitialDirection(this.SpawnCenter, p).Multiply(this.StartSpeedMax_Particle_Angular))
						.Add(HyperspaceFunctions.RandomUnitVector_Spherical(Parameters.DIM, Program.Random).Multiply(this.StartSpeedMax_Particle_Range))))
				.ToArray();
		}

		private static int _globalID = 0;
		private readonly int _id = _globalID++;
		public int ID => this._id;

		public readonly int NumParticles;
		public readonly double[] SpawnCenter;
		public readonly double[] InitialVelocity;

		public abstract double InitialSeparationRadius { get; }

		public TParticle[] MemberParticles { get; private set; }
		IParticle[] IParticleGroup.MemberParticles => this.MemberParticles;

		public abstract double StartSpeedMax_Group_Angular { get; }
		public abstract double StartSpeedMax_Group_Rand { get; }
		public abstract double StartSpeedMax_Particle_Angular { get; }
		public abstract double StartSpeedMax_Particle_Range { get; }

		protected abstract TParticle NewParticle(double[] position, double[] velocity);

		protected virtual double[] NewParticlePosition(double[] center, double radius) {
			return center.Add(
				HyperspaceFunctions.RandomCoordinate_Spherical(radius, Parameters.DIM, Program.Random));
		}

		protected virtual double[] NewInitialDirection(double[] center, double[] position) {
			return HyperspaceFunctions
				.RandomUnitVector_Spherical(Parameters.DIM, Program.Random)
				.Multiply(Math.Log(center.Distance(position) + 1d));
		}

		public bool Equals(IParticleGroup other) { return !(other is null) && this.ID == other.ID; }
		public override bool Equals(object other) { return !(other is null) && (other is AParticleGroup<TParticle>) && this.ID == (other as AParticleGroup<TParticle>).ID; }
		public bool Equals(IParticleGroup x, IParticleGroup y) { return x.ID == y.ID; }
		public int GetHashCode(IParticleGroup obj) { return obj.ID.GetHashCode(); }
		public override int GetHashCode() { return this.ID.GetHashCode(); }
		public override string ToString() { return string.Format("{0}[ID {1}]", nameof(AParticleGroup<TParticle>), this.ID); }
	}
}