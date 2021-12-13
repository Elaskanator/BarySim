using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Generic.Extensions;
using Generic.Models;
using Generic.Vectors;

namespace ParticleSimulator.Simulation {
	public interface IParticleSimulator {
		public AParticle[] AllParticles { get; }
		public Scaling Scaling { get; }

		public ITree RebuildTree();
		public ConsoleColor ChooseColor(Tuple<char, AParticle[], double> others);
		public AParticle[] RefreshSimulation(object[] parameters);
	}

	public abstract class AParticleSimulator<P, T, G> : IParticleSimulator
	where P : AParticle
	where T : AVectorQuadTree<P, T>
	where G : AParticleGroup<P> {
		public AParticleSimulator() {
			this.Scaling = new();

			this.ParticleGroups = Enumerable.Range(0, Parameters.PARTICLES_GROUP_COUNT)
				.Select(i => this.NewParticleGroup())
				.ToArray();
			this.AllParticles = this.ParticleGroups.SelectMany(g => g.Particles).ToArray();
			this.HandleBounds();
		}

		public G[] ParticleGroups { get; private set; }
		public P[] AllParticles { get; private set; }
		AParticle[] IParticleSimulator.AllParticles => this.AllParticles;
		public Scaling Scaling { get; private set; }
		public virtual double WorldBounceWeight => 0d;
		public virtual int InteractionLimit => int.MaxValue;

		protected abstract G NewParticleGroup();
		protected abstract T NewTree(double[] leftCorner, double[] rightCorner);
		protected abstract void InteractAll(T tree);

		public ITree RebuildTree() {
			double[]
				leftCorner = Enumerable.Repeat(double.PositiveInfinity, Parameters.DIM).ToArray(),
				rightCorner = Enumerable.Repeat(double.NegativeInfinity, Parameters.DIM).ToArray();
			P[] particles = (P[])this.AllParticles.Clone();
			foreach (AParticle p in particles) {
				p._coordinates = (double[])p.LiveCoordinates.Clone();
				for (int d = 0; d < Parameters.DIM; d++) {
					leftCorner[d] = leftCorner[d] < p.Coordinates[d] ? leftCorner[d] : p.Coordinates[d];
					rightCorner[d] = rightCorner[d] > p.Coordinates[d] ? rightCorner[d] : p.Coordinates[d];
				}
			}

			T result = this.NewTree(leftCorner, rightCorner.Select(x => x += Parameters.WORLD_EPSILON).ToArray());
			result.AddRange(particles);
			return result;
		}

		public AParticle[] RefreshSimulation(object[] parameters) {
			T tree = (T)parameters[0];
			if (this.AllParticles.Length == 0)
				Program.CancelAction(null, null);

			for (int i = 0; i < this.AllParticles.Length; i++)
				this.AllParticles[i].NetForce = new double[Parameters.DIM];

			this.InteractAll(tree);

			this.HandleBounds();
			this.AllParticles = this.AllParticles.Where(p => p.IsActive).ToArray();
			
			for (int i = 0; i < this.AllParticles.Length; i++)
				this.AllParticles[i].ApplyTimeStep();

			return this.AllParticles;
		}

		public ConsoleColor ChooseColor(Tuple<char, AParticle[], double> particleData) {
			int rank;
			switch (Parameters.COLOR_SCHEME) {
				case ParticleColoringMethod.Density:
					rank = this.Scaling.Values.Drop(1).TakeWhile(ds => ds < particleData.Item3).Count();
					return Parameters.COLOR_ARRAY[rank];
				case ParticleColoringMethod.Group:
					return this.ChooseGroupColor(particleData.Item2);
				case ParticleColoringMethod.Depth:
					if (Parameters.DIM > 2) {
						int numColors = Parameters.COLOR_ARRAY.Length;
						double depth = 1d - particleData.Item2.Min(p => GetDepthScalar(p.LiveCoordinates));
						rank = this.Scaling.Values.Take(numColors - 1).TakeWhile(a => a < depth).Count();
						return Parameters.COLOR_ARRAY[rank];
					} else return Parameters.COLOR_ARRAY[^1];
				default:
					throw new InvalidEnumArgumentException(nameof(Parameters.COLOR_SCHEME));
			}
		}

		public double GetDepthScalar(double[] v) {
			if (Parameters.DIM > 2)
				return 1d - (v.Skip(2).ToArray().Magnitude() / Parameters.DOMAIN_HIDDEN_DIMENSIONAL_HEIGHT);
			else return 1d;
		}

		public virtual ConsoleColor ChooseGroupColor(AParticle[] particles) {
			int dominantGroupID;
			if (Parameters.DIM > 2)
				dominantGroupID = particles.MinBy(p => this.GetDepthScalar(p.LiveCoordinates)).GroupID;
			else dominantGroupID  = particles.GroupBy(p => p.GroupID).MaxBy(g => g.Count()).Key;
			return Parameters.COLOR_ARRAY[dominantGroupID % Parameters.COLOR_ARRAY.Length];
		}

		private void HandleBounds() {
			Parallel.ForEach(
				this.AllParticles,
				Parameters.MulithreadedOptions,
				p => {
					if (Parameters.WORLD_WRAPPING)
						p.WrapPosition();
					else if (Parameters.WORLD_BOUNDING)
						p.BoundPosition();
					else if (p.LiveCoordinates.Any((c, d) => c < -Parameters.WORLD_DEATH_BOUND_CNT*Parameters.DOMAIN_SIZE[d] || c > Parameters.DOMAIN_SIZE[d] *(1d + Parameters.WORLD_DEATH_BOUND_CNT)))
						p.IsActive = false;

					if (this.WorldBounceWeight > 0d)
						p.BounceVelocity(this.WorldBounceWeight);
			});
		}
	}
}