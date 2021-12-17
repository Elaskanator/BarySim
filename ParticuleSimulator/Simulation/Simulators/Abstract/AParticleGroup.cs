using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Generic.Vectors;

namespace ParticleSimulator.Simulation {
	public interface IParticleGroup : IEquatable<IParticleGroup>, IEqualityComparer<IParticleGroup> {
		public int ID { get; }
		public ISimulationParticle[] MemberParticles { get; }
	}

	public abstract class AParticleGroup<TParticle> : IParticleGroup
	where TParticle : ASimulationParticle<TParticle> {
		public AParticleGroup() {
			this.NumParticles = Parameters.PARTICLES_GROUP_MIN + (int)Math.Round(Math.Pow(Program.Random.NextDouble(), Parameters.PARTICLES_GROUP_SIZE_SKEW_POWER) * (Parameters.PARTICLES_GROUP_MAX - Parameters.PARTICLES_GROUP_MIN));

			if (Parameters.PARTICLES_GROUP_COUNT < 2)
				this.SpawnCenter = Parameters.DOMAIN_CENTER;
			else this.SpawnCenter = VectorFunctions.New(Enumerable
				.Range(0, Parameters.DIM)
				.Select(d => (float)(
					Parameters.DOMAIN_SIZE[d] * (Program.Random.NextDouble() * (100d - Parameters.WORLD_PADDING_PCT) + 0.5d * Parameters.WORLD_PADDING_PCT) / 100d)));

			this.InitialVelocity = (this.StartSpeedMax_Group_Angular * this.NewInitialDirection(Parameters.DOMAIN_CENTER, this.SpawnCenter))
				+ VectorFunctions.New(VectorFunctions.RandomUnitVector_Spherical(Parameters.DIM, Program.Random).Select(x => (float)x * this.StartSpeedMax_Group_Rand));

			double particleVolume = VectorFunctions.HypersphereVolume(this.InitialSeparationRadius, Parameters.DIM);
			float radius = this.NumParticles > 1 ? (float)VectorFunctions.HypersphereRadius(particleVolume * this.NumParticles, Parameters.DIM) : 0f;
			this.MemberParticles = Enumerable
				.Range(0, this.NumParticles)
				.Select(i => this.NewParticlePosition(this.SpawnCenter, radius))
				.Select(p => this.NewParticle(
					p,
					this.InitialVelocity
						+ this.StartSpeedMax_Particle_Angular * this.NewInitialDirection(this.SpawnCenter, p)
						+ VectorFunctions.New(VectorFunctions.RandomUnitVector_Spherical(Parameters.DIM, Program.Random).Select(x => (float)x * this.StartSpeedMax_Particle_Range))))
				.ToArray();
		}

		private static int _globalID = 0;
		private readonly int _id = _globalID++;
		public int ID => this._id;

		public readonly int NumParticles;
		public readonly Vector<float> SpawnCenter;
		public readonly Vector<float> InitialVelocity;

		public abstract float InitialSeparationRadius { get; }

		public TParticle[] MemberParticles { get; private set; }
		ISimulationParticle[] IParticleGroup.MemberParticles => this.MemberParticles;

		public abstract float StartSpeedMax_Group_Angular { get; }
		public abstract float StartSpeedMax_Group_Rand { get; }
		public abstract float StartSpeedMax_Particle_Angular { get; }
		public abstract float StartSpeedMax_Particle_Range { get; }

		protected abstract TParticle NewParticle(Vector<float> position, Vector<float> velocity);

		protected virtual Vector<float> NewParticlePosition(Vector<float> center, float radius) {
			return center + VectorFunctions.New(VectorFunctions.RandomCoordinate_Spherical(radius, Parameters.DIM, Program.Random).Select(x => (float)x));
		}

		protected virtual Vector<float> NewInitialDirection(Vector<float> center, Vector<float> position) {
			return VectorFunctions.New(VectorFunctions.RandomUnitVector_Spherical(Parameters.DIM, Program.Random).Select(x => (float)x));
		}

		public bool Equals(IParticleGroup other) { return !(other is null) && this.ID == other.ID; }
		public override bool Equals(object other) { return !(other is null) && (other is AParticleGroup<TParticle>) && this.ID == (other as AParticleGroup<TParticle>).ID; }
		public bool Equals(IParticleGroup x, IParticleGroup y) { return x.ID == y.ID; }
		public int GetHashCode(IParticleGroup obj) { return obj.ID.GetHashCode(); }
		public override int GetHashCode() { return this.ID.GetHashCode(); }
		public override string ToString() { return string.Format("{0}[ID {1}]", nameof(AParticleGroup<TParticle>), this.ID); }
	}
}