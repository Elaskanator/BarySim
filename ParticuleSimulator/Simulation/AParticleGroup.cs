using System;
using System.Collections.Generic;
using System.Linq;
using Generic.Extensions;
using Generic.Models.Vectors;

namespace ParticleSimulator.Simulation {
	public abstract class AParticleGroup<P> : IEquatable<AParticleGroup<P>>, IEqualityComparer<AParticleGroup<P>>
	where P : AParticle {
		private static int _globalID = 0;
		public int ID { get; }

		public P[] Particles { get; private set; }

		public abstract P NewParticle(double[] position, double[] groupVelocity);

		public AParticleGroup() {
			this.ID = _globalID++;
		}

		protected virtual double InitialSeparation => this.Particles.Average(p => p.Radius) * 2d;

		public void Init(double[] center) {
			int numMembers = 1 + (int)Math.Round(Program.Random.NextDouble() * (Parameters.PARTICLES_PER_GROUP_MAX-1));

			double[] groupVelocity = NumberExtensions.RandomUnitVector_Spherical(Parameters.DIM, Program.Random).Multiply(Parameters.MAX_GROUP_STARTING_SPEED);
			this.Particles = Enumerable
				.Range(0, numMembers)
				.Select(i => this.NewParticle(center, groupVelocity))
				.ToArray();
			
			if (numMembers > 1) {
				double particleVolume = NumberExtensions.HypersphereVolume(this.InitialSeparation, Parameters.DIM);
				double radius = numMembers > 1 ? NumberExtensions.HypersphereRadius(particleVolume * numMembers, Parameters.DIM) : 0d;
				for (int i = 0; i < numMembers; i++)
					this.Particles[i].Coordinates = this.Particles[i].Coordinates.Add(
						NumberExtensions.RandomCoordinate_Spherical(radius, Parameters.DIM, Program.Random));
			}
		}

		public bool Equals(AParticleGroup<P> other) { return !(other is null) && this.ID == other.ID; }
		public override bool Equals(object other) { return !(other is null) && (other is AParticleGroup<P>) && this.ID == (other as AParticleGroup<P>).ID; }
		public bool Equals(AParticleGroup<P> x, AParticleGroup<P> y) { return x.ID == y.ID; }
		public int GetHashCode(AParticleGroup<P> obj) { return obj.ID.GetHashCode(); }
		public override int GetHashCode() { return this.ID.GetHashCode(); }
		public override string ToString() { return string.Format("{0}[ID {1}]", nameof(AParticleGroup<P>), this.ID); }
	}
}