using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Generic.Vectors;

namespace ParticleSimulator.Simulation {
	public interface IParticleGroup : IEquatable<IParticleGroup>, IEqualityComparer<IParticleGroup> {
		int ID { get; }
		IParticle[] InitialParticles { get; }
	}

	public abstract class AParticleGroup<TParticle> : IParticleGroup
	where TParticle : Particle {
		public AParticleGroup() {
			this.NumParticles = Parameters.PARTICLES_GROUP_MIN + (int)Math.Round(Math.Pow(Program.Engine.Random.NextDouble(), Parameters.PARTICLES_GROUP_SIZE_SKEW_POWER) * (Parameters.PARTICLES_GROUP_MAX - Parameters.PARTICLES_GROUP_MIN));

			if (Parameters.PARTICLES_GROUP_COUNT < 2)
				this.SpawnCenter = Vector<float>.Zero;
			else this.SpawnCenter = VectorFunctions.New(
					Enumerable.Range(0, Parameters.DIM)
						.Select(d => (float)(Parameters.WORLD_SIZE[d] * Program.Engine.Random.NextDouble() * (1d - Parameters.WORLD_PADDING_PCT/50d)
							+ Parameters.WORLD_LEFT[d]
							+ (Parameters.WORLD_SIZE[d] * Parameters.WORLD_PADDING_PCT/100d))));

			this.InitialVelocity = VectorFunctions.New(VectorFunctions.RandomUnitVector_Spherical(Parameters.DIM, Program.Engine.Random).Select(x => (float)x * this.StartSpeedMax_Group_Rand))
				+ (this.StartSpeedMax_Group_Angular * this.NewInitialDirection(Vector<float>.Zero, this.SpawnCenter));

			this.InitialParticles = Enumerable
				.Repeat(this.SpawnCenter, this.NumParticles)
				.Select(position => this.NewParticle(
					position,
					this.InitialVelocity))
				.ToArray();

			float radius = this.ComputeInitialSeparationRadius(this.InitialParticles);
			Vector<float> min = new Vector<float>(this.StartSpeedMax_Particle_Min);
			Vector<float> max = new Vector<float>(this.StartSpeedMax_Particle_Max);
			Vector<float> range = max - min;
			for (int i = 0; i < this.NumParticles; i++) {
				if (this.NumParticles > 1)
					this.InitialParticles[i].Position += this.NewParticleOffset(radius);
				this.InitialParticles[i].Velocity += (min + (range * (float)Program.Engine.Random.NextDouble())) * VectorFunctions.New(VectorFunctions.RandomUnitVector_Spherical(Parameters.DIM, Program.Engine.Random).Select(x => (float)x));
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
		IParticle[] IParticleGroup.InitialParticles => this.InitialParticles;

		public abstract float StartSpeedMax_Group_Angular { get; }
		public abstract float StartSpeedMax_Group_Rand { get; }
		public abstract float StartSpeedMax_Particle_Angular { get; }
		public abstract float StartSpeedMax_Particle_Min { get; }
		public abstract float StartSpeedMax_Particle_Max { get; }

		protected abstract TParticle NewParticle(Vector<float> position, Vector<float> velocity);

		protected virtual Vector<float> NewParticleOffset(float radius) {
			return VectorFunctions.New(VectorFunctions.RandomCoordinate_Spherical(radius, Parameters.DIM, Program.Engine.Random).Select(x => (float)x));
		}

		protected virtual Vector<float> NewInitialDirection(Vector<float> center, Vector<float> position) {
			return VectorFunctions.New(VectorFunctions.RandomUnitVector_Spherical(Parameters.DIM, Program.Engine.Random).Select(x => (float)x));
		}

		public bool Equals(IParticleGroup other) { return !(other is null) && this.ID == other.ID; }
		public override bool Equals(object other) { return !(other is null) && (other is AParticleGroup<TParticle>) && this.ID == (other as AParticleGroup<TParticle>).ID; }
		public bool Equals(IParticleGroup x, IParticleGroup y) { return x.ID == y.ID; }
		public int GetHashCode(IParticleGroup obj) { return obj.ID.GetHashCode(); }
		public override int GetHashCode() { return this.ID.GetHashCode(); }
		public override string ToString() { return string.Format("{0}[ID {1}]", nameof(AParticleGroup<TParticle>), this.ID); }
	}
}