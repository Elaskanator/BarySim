using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Generic.Vectors;

namespace ParticleSimulator.Simulation.Particles {
	public interface IParticleGroup : IEquatable<IParticleGroup>, IEqualityComparer<IParticleGroup> {
		int ID { get; }
		IParticle[] InitialParticles { get; }
	}

	public abstract class AParticleGroup<TParticle> : IParticleGroup
	where TParticle : AParticle<TParticle> {
		public AParticleGroup(float r) {
			this.Radius = r;
			this.NumParticles = Parameters.PARTICLES_GROUP_MIN + (int)Math.Round(Math.Pow(Program.Engine.Random.NextDouble(), Parameters.PARTICLES_GROUP_SIZE_SKEW_POWER) * (Parameters.PARTICLES_GROUP_MAX - Parameters.PARTICLES_GROUP_MIN));
		}

		public void Init() {
			this.InitPositionVelocity();

			this.InitialParticles = Enumerable
				.Repeat(this.Position, this.NumParticles)
				.Select(position => this.NewParticle(
					position,
					this.Velocity))
				.ToArray();

			//float radius = this.ComputeInitialSeparationRadius(this.InitialParticles);
			Vector<float> min = new Vector<float>(this.StartSpeedMax_Particle_Min);
			Vector<float> max = new Vector<float>(this.StartSpeedMax_Particle_Max);
			Vector<float> range = max - min;
			for (int i = 0; i < this.NumParticles; i++) {
				if (this.NumParticles > 1)
					this.ParticleAddPositionVelocity(this.InitialParticles[i]);

				this.InitialParticles[i].Velocity +=
					(min + (range * (float)Program.Engine.Random.NextDouble()))
					* VectorFunctions.New(VectorFunctions.RandomUnitVector_Spherical(Parameters.DIM, Program.Engine.Random).Select(x => (float)x));

				if (Parameters.WORLD_BOUNCING)
					if (Parameters.WORLD_WRAPPING)
						this.InitialParticles[i].WrapPosition();
					else this.InitialParticles[i].BoundPosition();
			}
		}

		protected virtual void InitPositionVelocity() {
			if (Parameters.PARTICLES_GROUP_COUNT < 2)
				this.Position = Vector<float>.Zero;
			else this.Position = VectorFunctions.New(
					Enumerable.Range(0, Parameters.DIM)
						.Select(d => (float)(Parameters.WORLD_SIZE[d] * Program.Engine.Random.NextDouble() * (1d - Parameters.WORLD_PADDING_PCT/50d)
							+ Parameters.WORLD_LEFT[d]
							+ (Parameters.WORLD_SIZE[d] * Parameters.WORLD_PADDING_PCT/100d))));

			this.Velocity = VectorFunctions.New(VectorFunctions.RandomUnitVector_Spherical(Parameters.DIM, Program.Engine.Random).Select(x => (float)x * this.StartSpeedMax_Group_Rand))
				+ (this.StartSpeedMax_Group_Angular
					* VectorFunctions.New(VectorFunctions.RandomUnitVector_Spherical(Parameters.DIM, Program.Engine.Random).Select(x => (float)x)));
		}

		protected virtual void ParticleAddPositionVelocity(TParticle particle) {
			particle.Position +=
				((float)Program.Engine.Random.NextDouble()) *
				VectorFunctions.New(VectorFunctions.RandomCoordinate_Spherical(this.Radius, Parameters.DIM, Program.Engine.Random).Select(x => (float)x));;
			particle.Velocity += this.StartSpeedMax_Group_Angular
				* VectorFunctions.New(VectorFunctions.RandomUnitVector_Spherical(Parameters.DIM, Program.Engine.Random).Select(x => (float)x));
		}

		private static int _globalID = 0;
		private readonly int _id = _globalID++;
		public int ID => this._id;

		public readonly float Radius;
		public readonly int NumParticles;

		public Vector<float> Position { get; protected set; }
		public Vector<float> Velocity { get; protected set; }

		public TParticle[] InitialParticles { get; private set; }
		IParticle[] IParticleGroup.InitialParticles => this.InitialParticles;

		public virtual float StartSpeedMax_Group_Angular => 0f;
		public virtual float StartSpeedMax_Group_Rand => 0f;
		public virtual float StartSpeedMax_Particle_Min => 0f;
		public virtual float StartSpeedMax_Particle_Max => 0f;

		protected abstract TParticle NewParticle(Vector<float> position, Vector<float> velocity);

		public bool Equals(IParticleGroup other) { return !(other is null) && this.ID == other.ID; }
		public override bool Equals(object other) { return !(other is null) && (other is AParticleGroup<TParticle>) && this.ID == (other as AParticleGroup<TParticle>).ID; }
		public bool Equals(IParticleGroup x, IParticleGroup y) { return x.ID == y.ID; }
		public int GetHashCode(IParticleGroup obj) { return obj.ID.GetHashCode(); }
		public override int GetHashCode() { return this.ID.GetHashCode(); }
		public override string ToString() { return string.Format("{0}[ID {1}]", nameof(AParticleGroup<TParticle>), this.ID); }
	}
}