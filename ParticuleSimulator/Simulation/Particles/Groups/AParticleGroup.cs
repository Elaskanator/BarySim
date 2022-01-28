using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Generic.Vectors;

namespace ParticleSimulator.Simulation.Particles {
	public interface IParticleGroup : IEquatable<IParticleGroup>, IEqualityComparer<IParticleGroup> {
		int Id { get; }
		IParticle[] InitialParticles { get; }
	}

	public abstract class AParticleGroup<TParticle> : IParticleGroup
	where TParticle : AParticle<TParticle> {
		public AParticleGroup(Func<Vector<float>, Vector<float>, TParticle> initializer, float radius) {
			this.ParticleInitializer = initializer;
			this.Radius = radius;
			this.NumParticles = Parameters.PARTICLES_GROUP_MIN + (int)Math.Round(Math.Pow(Program.Random.NextDouble(), Parameters.PARTICLES_GROUP_SIZE_POW) * (Parameters.PARTICLES_GROUP_MAX - Parameters.PARTICLES_GROUP_MIN));

			if (Parameters.PARTICLES_GROUP_COUNT > 1)
				this.InitPositionVelocity();
			else this.Position = Vector<float>.Zero;

			this.InitialParticles = Enumerable
				.Repeat(this.Position, this.NumParticles)
				.Select(position => this.ParticleInitializer(position, this.Velocity))
				.ToArray();

			for (int i = 0; i < this.NumParticles; i++) {
				this.InitialParticles[i].GroupId = this.Id;

				if (this.NumParticles > 1)
					this.ParticleAddPositionVelocity(this.InitialParticles[i]);

				if (Parameters.WORLD_BOUNCING)
					if (Parameters.WORLD_WRAPPING)
						this.InitialParticles[i].WrapPosition();
					else this.InitialParticles[i].BoundPosition();
			}
		}

		public readonly Func<Vector<float>, Vector<float>, TParticle> ParticleInitializer;
		protected virtual void PrepareNewParticle(TParticle p) { }
		
		protected virtual void InitPositionVelocity() {
			this.Position = VectorFunctions.New(
				Enumerable.Range(0, Parameters.DIM)
					.Select(d => (float)(Parameters.WORLD_SIZE[d] * Program.Random.NextDouble() * (1d - Parameters.WORLD_PADDING_PCT/50d)
						+ Parameters.WORLD_LEFT[d]
						+ (Parameters.WORLD_SIZE[d] * Parameters.WORLD_PADDING_PCT/100d))));
			this.Velocity = Parameters.GALAXY_SPEED_RAND
				* VectorFunctions.New(VectorFunctions.RandomUnitVector_Spherical(Parameters.DIM, Program.Random));
		}

		protected abstract void ParticleAddPositionVelocity(TParticle particle);

		private static int _globalID = 0;
		private readonly int _id = _globalID++;
		public int Id => this._id;

		public readonly float Radius;
		public readonly int NumParticles;

		public Vector<float> Position { get; protected set; }
		public Vector<float> Velocity { get; protected set; }

		public TParticle[] InitialParticles { get; private set; }
		IParticle[] IParticleGroup.InitialParticles => this.InitialParticles;

		public bool Equals(IParticleGroup other) { return !(other is null) && this.Id == other.Id; }
		public override bool Equals(object other) { return !(other is null) && (other is AParticleGroup<TParticle>) && this.Id == (other as AParticleGroup<TParticle>).Id; }
		public bool Equals(IParticleGroup x, IParticleGroup y) { return x.Id == y.Id; }
		public int GetHashCode(IParticleGroup obj) { return obj.Id.GetHashCode(); }
		public override int GetHashCode() { return this.Id.GetHashCode(); }
		public override string ToString() { return string.Format("{0}[ID {1}]", nameof(AParticleGroup<TParticle>), this.Id); }
	}
}