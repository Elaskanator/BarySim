using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Generic.Extensions;
using Generic.Models;
using Generic.Vectors;

namespace ParticleSimulator.Simulation {
	public interface IParticleSimulator {
		public AParticle[] AllParticles { get; }
		public double[] DensityScale { get; }

		public ITree RebuildTree();
		public ConsoleColor ChooseColor(Tuple<char, AParticle[], double> others);
		public AParticle[] RefreshSimulation(object[] parameters);
		public Tuple<char, AParticle[], double>[] Resample(object[] parameters);
		public void AutoscaleUpdate(object[] parameters);
	}

	public abstract class AParticleSimulator<P, T, G> : IParticleSimulator
	where P : AParticle
	where T : AVectorQuadTree<P, T>
	where G : AParticleGroup<P> {
		public AParticleSimulator() {
			this.ParticleGroups = new G[Parameters.PARTICLES_GROUP_COUNT];
			for (int i = 0; i < Parameters.PARTICLES_GROUP_COUNT; i++) {
				this.ParticleGroups[i] = this.NewParticleGroup();
			}

			if (Parameters.COLOR_SCHEME == ParticleColoringMethod.Depth)
				this.DensityScale =
					Enumerable
						.Range(1, Parameters.COLOR_ARRAY.Length)
						.Select(x => (double)x / Parameters.COLOR_ARRAY.Length)
						.ToArray();
			else if (Parameters.COLOR_SCHEME == ParticleColoringMethod.Group)
				this.DensityScale =
					Enumerable
						.Range(1, Parameters.COLOR_ARRAY.Length)
						.Select(x => (double)x)
						.ToArray();
			else this.DensityScale = new double[0];

			this.AllParticles = this.ParticleGroups.SelectMany(g => g.Particles).ToArray();
			this.HandleBounds();
		}

		protected virtual int InteractionLimit => int.MaxValue;
		
		public G[] ParticleGroups { get; private set; }
		public P[] AllParticles { get; private set; }
		AParticle[] IParticleSimulator.AllParticles => this.AllParticles;
		public double[] DensityScale { get; private set; }

		protected abstract G NewParticleGroup();

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
		protected abstract T NewTree(double[] leftCorner, double[] rightCorner);

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

		protected abstract void InteractAll(T tree);

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

					if (Parameters.WORLD_BOUNCE_WEIGHT > 0d)
						p.BounceVelocity();
			});
		}

		public Tuple<char, AParticle[], double>[] Resample(object[] parameters) {
			P[] particleData = (P[])parameters[0];
			Tuple<char, AParticle[], double>[] results = new Tuple<char, AParticle[], double>[Parameters.WINDOW_WIDTH * Parameters.WINDOW_HEIGHT];

			char pixelChar;
			P[] topStuff, bottomStuff, distinct;
			foreach (IGrouping<int, Tuple<int, int, P>> bin in DiscreteParticleBin(particleData)) {
				topStuff = bin.Where(t => t.Item2 % 2 == 0).Select(t => t.Item3).ToArray();
				bottomStuff = bin.Where(t => t.Item2 % 2 == 1).Select(t => t.Item3).ToArray();
				distinct = bin.Select(b => b.Item3).Distinct().ToArray();

				if (topStuff.Length > 0 && bottomStuff.Length > 0)
					pixelChar = Parameters.CHAR_BOTH;
				else if (topStuff.Length > 0)
					pixelChar = Parameters.CHAR_TOP;
				else pixelChar = Parameters.CHAR_LOW;

				results[bin.Key] =
					new Tuple<char, AParticle[], double>(
						pixelChar,
						distinct,
						distinct.Sum(p => p.Mass));
			}
			return results;
		}
		private static IEnumerable<IGrouping<int, Tuple<int, int, P>>> DiscreteParticleBin(P[] particles) { 
			return particles
				.Where(p =>
					p.IsActive
					&& p.LiveCoordinates[0] + p.Radius >= 0 && p.LiveCoordinates[0] - p.Radius < Parameters.DOMAIN_SIZE[0]
					&& (Parameters.DIM < 2 || p.LiveCoordinates[1] > -p.Radius && p.LiveCoordinates[1] < p.Radius + Parameters.DOMAIN_SIZE[1]))
				.SelectMany(p => SpreadSample(p).Where(p => p.Item1 >= 0 && p.Item1 < Parameters.WINDOW_WIDTH && p.Item2 >= 0 && p.Item2 < 2*Parameters.WINDOW_HEIGHT))
				.GroupBy(pd => pd.Item1 + (Parameters.WINDOW_WIDTH * (pd.Item2 / 2)));
		}
		private static IEnumerable<Tuple<int, int, P>> SpreadSample(P p) {
			double
				pixelScalar = Renderer.RenderWidth / Parameters.DOMAIN_SIZE[0],
				scaledX = Renderer.RenderWidthOffset + p.LiveCoordinates[0] * pixelScalar,
				scaledY = Renderer.RenderHeightOffset + (Parameters.DIM < 2 ? 0d : p.LiveCoordinates[1] * pixelScalar);

			if (p.Radius == 0d)
				yield return new((int)scaledX, (int)scaledY, p);
			else {
				double
					radiusX = p.Radius * pixelScalar,
					minX = scaledX - radiusX,
					maxX = scaledX + radiusX,
					radiusY = Parameters.DIM < 2 ? 0d : radiusX,
					minY = scaledY - radiusY,
					maxY = scaledY + radiusY;
				maxX = maxX < Parameters.WINDOW_WIDTH ? maxX : Parameters.WINDOW_WIDTH - 1;
				maxY = maxY < 2*Parameters.WINDOW_HEIGHT ? maxY : 2*Parameters.WINDOW_HEIGHT - 1;

				int
					rangeX = 1 + (int)(maxX) - (int)(minX),
					rangeY = 1 + (int)(maxY) - (int)(minY);

				double testX, testY, dist;
				int roundedX, roundedY;
				for (int x2 = 0; x2 < rangeX; x2++) {
					roundedX = x2 + (int)minX;
					if (roundedX >= 0d && roundedX < Parameters.WINDOW_WIDTH) {
						for (int y2 = 0; y2 < rangeY; y2++) {
							roundedY = y2 + (int)minY;
							if (roundedY >= 0d && roundedY < Parameters.WINDOW_HEIGHT*2) {
								testX = roundedX == (int)scaledX//particle in current bin
									? p.LiveCoordinates[0] * pixelScalar//use exact value
									: roundedX + (roundedX < scaledX ? 1 : 0);//nearer edge
								if (Parameters.DIM == 1) {
									dist = Math.Abs(testX - p.LiveCoordinates[0]);
									if (dist <= p.Radius) {
										yield return new(roundedX, roundedY, p);
									}
								} else {
									testY = roundedY == (int)scaledY//particle in current bin
										? p.LiveCoordinates[1] * pixelScalar//use exact value
										: roundedY + (roundedY < scaledY ? 1 : 0);//nearer edge
									dist = new double[] { testX, testY }.Distance(p.LiveCoordinates.Take(2).Select(c => c * pixelScalar).ToArray());
									if (p.Radius * pixelScalar >= dist)
										yield return new(roundedX, roundedY, p);
		}}}}}}}

		public void AutoscaleUpdate(object[] parameters) {
			Tuple<char, AParticle[], double>[] sampling = ((Tuple<char, AParticle[], double>[])parameters[0]).Without(t => t is null).ToArray();
			double[] densities;
			densities = sampling.Select(t => t.Item3).ToArray();
			//densities = this.AllParticles.Where(p => p.IsActive).Select(p => p.Mass).ToArray();

			if (densities.Length > 0) {
				StatsInfo stats = new(densities);
				if (Parameters.DENSITY_AUTOSCALE_CUTOFF_PCT > 0) {
					stats.FilterData(Parameters.DENSITY_AUTOSCALE_CUTOFF_PCT);
					if (stats.Data_asc.Length == 0)
						return;
				}

				if (Parameters.DENSITY_AUTOSCALE_PERCENTILE) {
					List<double> results = new(Parameters.COLOR_ARRAY.Length);
					int position, diff,
						totalBands = (Parameters.COLOR_ARRAY.Length < stats.Data_asc.Length ? Parameters.COLOR_ARRAY.Length : stats.Data_asc.Length);
					double newValue;
					for (int bandIdx = 0; bandIdx < totalBands && stats.Data_asc.Length > 0; bandIdx++) {
						newValue = stats.GetPercentileValue(100d * bandIdx / (totalBands + 1d), false);
						if (results.Count == 0) {
							results.Add(newValue);
							stats.Data_asc = stats.Data_asc
								.SkipWhile(d => d == newValue)
								.ToArray();
						} else {
							position = 0;
							while (newValue <= results[^1]) {
								diff = stats.Data_asc.Skip(position).TakeWhile(x => x <= newValue).Count();
								position += diff;
								if (position < stats.Data_asc.Length) {
									if (diff > 0)
										newValue = stats.Data_asc[position];
								} else break;
							}
							if (position < stats.Data_asc.Length) {
								if (newValue <= results[^1]) {
									totalBands -= bandIdx;
									bandIdx = 0;
								}
								results.Add(newValue);
								stats.Data_asc = stats.Data_asc
									.Skip(position)
									.SkipWhile(d => d == newValue)
									.ToArray();
							}
						}
					}
					this.DensityScale = results.ToArray();
				} else {
					double
						min = stats.Data_asc[0],
						max = stats.Data_asc[^1],
						range = max - min;
					if (range > 0) {
						double step = range / (Parameters.COLOR_ARRAY.Length + 1);
						this.DensityScale = Enumerable.Range(1, Parameters.COLOR_ARRAY.Length).Select(i => min + step*i).ToArray();
					} else this.DensityScale = new double[] { min };
				}
			}
		}

		public ConsoleColor ChooseColor(Tuple<char, AParticle[], double> particleData) {
			int rank;
			switch (Parameters.COLOR_SCHEME) {
				case ParticleColoringMethod.Density:
					rank = Program.Simulator.DensityScale.Drop(1).TakeWhile(ds => ds < particleData.Item3).Count();
					return Parameters.COLOR_ARRAY[rank];
				case ParticleColoringMethod.Group:
					return this.ChooseGroupColor(particleData.Item2);
				case ParticleColoringMethod.Depth:
					if (Parameters.DIM > 2) {
						int numColors = Parameters.COLOR_ARRAY.Length;
						double depth = 1d - particleData.Item2.Min(p => GetDepthScalar(p.LiveCoordinates));
						rank = Program.Simulator.DensityScale.Take(numColors - 1).TakeWhile(a => a < depth).Count();
						return Parameters.COLOR_ARRAY[rank];
					} else return Parameters.COLOR_ARRAY[^1];
				default:
					throw new InvalidEnumArgumentException(nameof(Parameters.COLOR_SCHEME));
			}
		}

		public virtual ConsoleColor ChooseGroupColor(AParticle[] particles) {
			int dominantGroupID;
			if (Parameters.DIM > 2)
				dominantGroupID = particles.MinBy(p => this.GetDepthScalar(p.LiveCoordinates)).GroupID;
			else dominantGroupID  = particles.GroupBy(p => p.GroupID).MaxBy(g => g.Count()).Key;
			return Parameters.COLOR_ARRAY[dominantGroupID % Parameters.COLOR_ARRAY.Length];
		}

		public double GetDepthScalar(double[] v) {
			if (Parameters.DIM > 2)
				return 1d - (v.Skip(2).ToArray().Magnitude() / Parameters.DOMAIN_HIDDEN_DIMENSIONAL_HEIGHT);
			else return 1d;
		}
	}
}