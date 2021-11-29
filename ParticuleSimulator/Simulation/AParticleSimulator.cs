using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Generic.Extensions;
using Generic.Models;
using Generic.Models.Vectors;

namespace ParticleSimulator.Simulation {
	public interface IParticleSimulator : IEnumerable<AParticle> {
		public AParticle[] AllParticles { get; }
		public double[] DensityScale { get; }

		public ITree RebuildTree();
		public ConsoleColor ChooseColor(AParticle[] others);
		public AParticle[] RefreshSimulation(object[] parameters);
		public Tuple<char, AParticle[]>[] Resample(object[] parameters);
		public void AutoscaleUpdate(object[] parameters);
	}

	public abstract class AParticleSimulator<P, T, G> : IParticleSimulator
	where P : AParticle
	where T : AVectorQuadTree<P, T>
	where G : AParticleGroup<P> {
		public AParticleSimulator() {
			double[] spawnCenter;
			this.ParticleGroups = new G[Parameters.PARTICLE_GROUPS_NUM];
			for (int i = 0; i < Parameters.PARTICLE_GROUPS_NUM; i++) {
				spawnCenter = Parameters.DOMAIN.Divide(2d).Add(
					NumberExtensions.RandomCoordinate_Spherical(
						Parameters.DOMAIN.Max() / 2d, Parameters.DIM,
						Program.Random));
				this.ParticleGroups[i] = this.NewParticleGroup();
				this.ParticleGroups[i].Init(spawnCenter);
			}
			int numDensityValues = Parameters.COLOR_ARRAY.Length - 1;
			if (Parameters.COLOR_SCHEME == ParticleColoringMethod.Depth)
				this.DensityScale =
					Enumerable
						.Range(1, numDensityValues + 1)
						.Select(x => x / (1d + numDensityValues))
						.ToArray();
			else this.DensityScale =
					Enumerable
						.Range(1, numDensityValues + 1)
						.Select(x => (double)x)
						.ToArray();

			this.AllParticles = this.ParticleGroups.SelectMany(g => g.Particles).ToArray();
			this.HandleBounds();
		}
		protected virtual int InteractionLimit => int.MaxValue;
		protected virtual bool UseMaxDensity => false;
		
		public G[] ParticleGroups { get; private set; }
		public P[] AllParticles { get; private set; }
		AParticle[] IParticleSimulator.AllParticles => this.AllParticles;
		public double[] DensityScale { get; private set; }
		protected readonly Random _rand;

		protected abstract G NewParticleGroup();

		public T RebuildTree() {
			double[]
				leftCorner = Enumerable.Repeat(double.PositiveInfinity, Parameters.DIM).ToArray(),
				rightCorner = Enumerable.Repeat(double.NegativeInfinity, Parameters.DIM).ToArray();
			P[] particles = this.ParticleGroups.SelectMany(g => g.Particles).Where(p => p.IsActive).ToArray();
			foreach (AParticle p in particles) {
				p.Coordinates = (double[])p.LiveCoordinates.Clone();
				for (int d = 0; d < Parameters.DIM; d++) {
					leftCorner[d] = leftCorner[d] < p.Coordinates[d] ? leftCorner[d] : p.Coordinates[d];
					rightCorner[d] = rightCorner[d] > p.Coordinates[d] ? rightCorner[d] : p.Coordinates[d];
				}
			}

			T result = this.NewTree(leftCorner, rightCorner.Select(x => x += Parameters.WORLD_EPSILON).ToArray());
			result.AddRange(particles);
			return result;
		}
		ITree IParticleSimulator.RebuildTree() { return this.RebuildTree(); }
		protected abstract T NewTree(double[] leftCorner, double[] rightCorner);

		public AParticle[] RefreshSimulation(object[] parameters) {
			T tree = (T)parameters[0];
			if (this.AllParticles.None())
				Program.CancelAction(null, null);

			foreach (AParticle p in this.AllParticles)
				p.Acceleration = new double[Parameters.DIM];

			this.InteractAll(tree);

			foreach (P p in this.AllParticles)
				p.ApplyTimeStep();

			this.HandleBounds();
			this.AllParticles = this.ParticleGroups.SelectMany(g => g.Particles).Where(p => p.IsActive).ToArray();
			return this.AllParticles;
		}

		protected virtual void InteractAll(T tree) {
			Parallel.ForEach(
				tree.Leaves,
				Parameters.MulithreadedOptions,
				leaf => {
					foreach (P p in leaf.NodeElements)
						p.Interact(
							leaf.GetNeighbors()
								.Without(p2 => p2.ID == p.ID)
								.Take(this.InteractionLimit));
			});
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

					if (Parameters.WORLD_BOUNCE_WEIGHT > 0d)
						p.BounceVelocity();
			});
		}

		public Tuple<char, AParticle[]>[] Resample(object[] parameters) {
			P[] particleData = (P[])parameters[0];
			Tuple<char, AParticle[]>[] results = new Tuple<char, AParticle[]>[Parameters.WINDOW_WIDTH * Parameters.WINDOW_HEIGHT];

			bool top, bottom;
			char pixelChar;
			foreach (IGrouping<int, dynamic> bin in this.DiscreteParticleBin(particleData)) {
				top = bin.Any(t => t.Y % 1d < 0.5d);
				bottom = bin.Any(t => t.Y % 1d >= 0.5d);

				if (top && bottom)
					pixelChar = Parameters.CHAR_BOTH;
				else if (top)
					pixelChar = Parameters.CHAR_TOP;
				else pixelChar = Parameters.CHAR_LOW;

				results[bin.Key] =
					new Tuple<char, AParticle[]>(
						pixelChar,
						bin.Select(b => (AParticle)b.Particle).ToArray());
			}
			return results;
		}

		protected virtual IEnumerable<IGrouping<int, dynamic>> DiscreteParticleBin(P[] particles) { 
			return particles
				.Where(pd => pd.LiveCoordinates.All((x, d) => x >= 0 && x < Parameters.DOMAIN[d]))
				.Select(pd => new {
					X = pd.LiveCoordinates[0] * Renderer.RenderWidth / Parameters.DOMAIN[0],
					Y = Parameters.DIM < 2 ? 0d : pd.LiveCoordinates[1] * Renderer.RenderHeight / Parameters.DOMAIN[1] / 2d,
					Particle = pd})
				.GroupBy(pd => (int)pd.X + Renderer.RenderWidthOffset + Parameters.WINDOW_WIDTH*((int)pd.Y + Renderer.RenderHeightOffset));
		}

		public void AutoscaleUpdate(object[] parameters) {
			Tuple<char, AParticle[]>[] sampling = (Tuple<char, AParticle[]>[])parameters[0];
			double[] densities = sampling.Without(t => t is null).Select(t => this.GetDensity(t.Item2.Cast<P>().ToArray())).ToArray();
			if (densities.Length > 0) {
				StatsInfo stats = new(densities);
				if (Parameters.DENSITY_AUTOSCALE_PERCENTILE) {
					if (stats.Data_asc.Any()) {
						int totalBands = Parameters.COLOR_ARRAY.Length < stats.Data_asc.Length ? Parameters.COLOR_ARRAY.Length : stats.Data_asc.Length;
						for (int bandIdx = 0; bandIdx < this.DensityScale.Length; bandIdx++)//skip last band
							this.DensityScale[bandIdx] = stats.GetPercentileValue(100d * (bandIdx + 1d) / totalBands);
					}
				} else {
					double
						lowerThreshold = stats.GetPercentileValue(100d / this.DensityScale.Length),
						upperThreshold = stats.GetPercentileValue(99.9d),
						diff = upperThreshold - lowerThreshold;
					for (int bandIdx = 1; bandIdx < this.DensityScale.Length - 1; bandIdx++)
						if (diff > 0d)
							this.DensityScale[bandIdx] = lowerThreshold + ((bandIdx + 1) * diff / this.DensityScale.Length);
						else if (bandIdx == 0)
							this.DensityScale[bandIdx] = stats.Data_asc[0];
						else this.DensityScale[bandIdx] = this.DensityScale[bandIdx - 1] + 1d;
					this.DensityScale[0] = lowerThreshold;
					this.DensityScale[^1] = stats.Data_asc[^1];
				}
			}
		}

		public ConsoleColor ChooseColor(P[] particleData) {
			int rank;
			switch (Parameters.COLOR_SCHEME) {
				case ParticleColoringMethod.Density:
					double density = this.GetDensity(particleData);
					rank = Program.Simulator.DensityScale.TakeWhile(a => a < density).Count();
					rank = rank < Parameters.COLOR_ARRAY.Length ? rank : Parameters.COLOR_ARRAY.Length - 1;
					return Parameters.COLOR_ARRAY[rank];
				case ParticleColoringMethod.Group:
					return this.ChooseGroupColor(particleData);
				case ParticleColoringMethod.Depth:
					if (Parameters.DIM > 2) {
						int numColors = Parameters.COLOR_ARRAY.Length;
						double depth = 1d - particleData.Min(p => GetDepthScalar(p.LiveCoordinates));
						rank = Program.Simulator.DensityScale.Take(numColors - 1).TakeWhile(a => a < depth).Count();
						return Parameters.COLOR_ARRAY[rank];
					} else return Parameters.COLOR_ARRAY[^1];
				default:
					throw new InvalidEnumArgumentException(nameof(Parameters.COLOR_SCHEME));
			}
		}
		ConsoleColor IParticleSimulator.ChooseColor(AParticle[] particleData) { return this.ChooseColor(particleData.Cast<P>().ToArray()); }

		public virtual ConsoleColor ChooseGroupColor(AParticle[] particles) {
			int dominantGroupID;
			if (Parameters.DIM > 2)
				dominantGroupID = particles.MinBy(p => this.GetDepthScalar(p.LiveCoordinates)).GroupID;
			else dominantGroupID  = particles.GroupBy(p => p.GroupID).MaxBy(g => g.Count()).Key;
			return Parameters.COLOR_ARRAY[dominantGroupID % Parameters.COLOR_ARRAY.Length];
		}

		private double GetDensity(P[] particles) {
			if (this.UseMaxDensity)
				return particles.Max(p =>
					this.GetParticleWeight(p) * this.GetDepthScalar(p.LiveCoordinates));
			else return particles.Sum(p =>
					this.GetParticleWeight(p) * this.GetDepthScalar(p.LiveCoordinates));
		}
		protected abstract double GetParticleWeight(P particle);

		public double GetDepthScalar(double[] v) {
			if (Parameters.DIM > 2)
				return 1d - (v.Skip(2).ToArray().Magnitude() / Parameters.DOMAIN_HIDDEN_DIMENSIONAL_HEIGHT);
			else return 1d;
		}

		public IEnumerator<AParticle> GetEnumerator() { return this.AllParticles.AsEnumerable().GetEnumerator(); }
		IEnumerator IEnumerable.GetEnumerator() {return this.AllParticles.GetEnumerator(); }
	}
}