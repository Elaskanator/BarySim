using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Generic.Vectors;

namespace ParticleSimulator.Simulation {
	public interface IParticleGroup : IEquatable<IParticleGroup>, IEqualityComparer<IParticleGroup> {
		int ID { get; }
		IBaryonParticle[] InitialParticles { get; }
	}

	public abstract class AParticleGroup<TParticle> : IParticleGroup
	where TParticle : BaryonParticle {
		public AParticleGroup() {
			this.NumParticles = Parameters.PARTICLES_GROUP_MIN + (int)Math.Round(Math.Pow(Program.Random.NextDouble(), Parameters.PARTICLES_GROUP_SIZE_SKEW_POWER) * (Parameters.PARTICLES_GROUP_MAX - Parameters.PARTICLES_GROUP_MIN));

			if (Parameters.PARTICLES_GROUP_COUNT < 2)
				this.SpawnCenter = Parameters.DOMAIN_CENTER;
			else this.SpawnCenter = VectorFunctions.New(Enumerable
				.Range(0, Parameters.DIM)
				.Select(d => (float)(
					Parameters.DOMAIN_SIZE[d] * (Program.Random.NextDouble() * (100d - Parameters.WORLD_PADDING_PCT) + 0.5d * Parameters.WORLD_PADDING_PCT) / 100d)));

			this.InitialVelocity = VectorFunctions.New(VectorFunctions.RandomUnitVector_Spherical(Parameters.DIM, Program.Random).Select(x => (float)x * this.StartSpeedMax_Group_Rand))
				+ (this.StartSpeedMax_Group_Angular * this.NewInitialDirection(Parameters.DOMAIN_CENTER, this.SpawnCenter));

			this.InitialParticles = Enumerable
				.Repeat(this.SpawnCenter, this.NumParticles)
				.Select(position => this.NewParticle(
					position,
					this.InitialVelocity))
				.ToArray();

			float radius = this.ComputeInitialSeparationRadius(this.InitialParticles);
			for (int i = 0; i < this.NumParticles; i++) {
				this.InitialParticles[i].Position += this.NewParticleOffset(radius);
				this.InitialParticles[i].Velocity += VectorFunctions.New(VectorFunctions.RandomUnitVector_Spherical(Parameters.DIM, Program.Random).Select(x => (float)x * this.StartSpeedMax_Particle_Range))
					+ this.StartSpeedMax_Particle_Angular * this.NewInitialDirection(this.SpawnCenter, this.InitialParticles[i].Position);
			}
		}

		private static int _globalID = 0;
		private readonly int _id = _globalID++;
		public int ID => this._id;

		public readonly int NumParticles;
		public readonly Vector<float> SpawnCenter;
		public readonly Vector<float> InitialVelocity;

		public abstract float ComputeInitialSeparationRadius(IEnumerable<TParticle> particles);

		public TParticle[] InitialParticles { get; private set; }
		IBaryonParticle[] IParticleGroup.InitialParticles => this.InitialParticles;

		public abstract float StartSpeedMax_Group_Angular { get; }
		public abstract float StartSpeedMax_Group_Rand { get; }
		public abstract float StartSpeedMax_Particle_Angular { get; }
		public abstract float StartSpeedMax_Particle_Range { get; }

		protected abstract TParticle NewParticle(Vector<float> position, Vector<float> velocity);

		protected virtual Vector<float> NewParticleOffset(float radius) {
			return VectorFunctions.New(VectorFunctions.RandomCoordinate_Spherical(radius, Parameters.DIM, Program.Random).Select(x => (float)x));
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