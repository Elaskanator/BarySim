using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Generic.Extensions;
using Generic.Models;
using ParticleSimulator.Rendering;

namespace ParticleSimulator.Simulation {
	public interface IParticleSimulator : IEnumerable<AParticle> {
		public AParticle[] AllParticles { get; }
		public double[] DensityScale { get; }
		public int InteractionLimit { get; }
		public int? NeighborhoodFilteringDepth { get; }

		public ITree RebuildTree();
		public ConsoleColor ChooseColor(AParticle[] others);
		public Tuple<double[], AParticle>[] RefreshSimulation(object[] parameters);
		public Tuple<char, AParticle[]>[] Resample(object[] parameters);
		public void AutoscaleUpdate(object[] parameters);
	}

	public abstract class AParticleSimulator<P, G> : IParticleSimulator
	where P : AParticle
	where G : AParticleGroup<P> {
		public AParticleSimulator() {
			double[] spawnCenter;
			this.ParticleGroups = new G[Parameters.NUM_PARTICLE_GROUPS];
			for (int i = 0; i < Parameters.NUM_PARTICLE_GROUPS; i++) {
				double test = Parameters.DOMAIN.Magnitude();
				spawnCenter = Parameters.DOMAIN.Divide(2).Add(
					NumberExtensions.RandomCoordinate_Spherical(
						Parameters.DOMAIN.Max() / 2d, Parameters.DOMAIN.Length,
						Program.Random));
				this.ParticleGroups[i] = this.NewParticleGroup();
				this.ParticleGroups[i].Init(spawnCenter);
			}
			this.AllParticles = this.ParticleGroups.SelectMany(g => g.Particles).ToArray();
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
		}
		public virtual int InteractionLimit => Parameters.DESIRED_INTERACTION_NEIGHBORS;
		public virtual int? NeighborhoodFilteringDepth => null;
		
		public G[] ParticleGroups { get; private set; }
		public P[] AllParticles { get; private set; }
		AParticle[] IParticleSimulator.AllParticles => this.AllParticles;
		public double[] DensityScale { get; private set; }
		protected readonly Random _rand;

		public abstract G NewParticleGroup();

		public ParticleTree<P> RebuildTree() {
			foreach (P p in this.AllParticles)
				p.Coordinates = (double[])p.TrueCoordinates.Clone();//tree reuse means we don't care about race conditions with dirty access
			ParticleTree<P> result = new ParticleTree<P>();
			result.AddRange(this.AllParticles.Where(p => p.Coordinates.Select((x, d) => x >= 0 && x < Parameters.DOMAIN[d]).All()));
			return result;
		}
		ITree IParticleSimulator.RebuildTree() { return this.RebuildTree(); }

		public Tuple<double[], AParticle>[] RefreshSimulation(ParticleTree<P> tree) {
			if (Parameters.DESIRED_INTERACTION_NEIGHBORS != 0)
				this.InteractTree(tree);

			foreach (P p in this.AllParticles)
				p.ApplyTimeStep();

			Tuple<double[], AParticle>[] result = this.AllParticles.Select(p => new Tuple<double[], AParticle>(p.TrueCoordinates, p)).ToArray();

			return result;
		}
		public Tuple<double[], AParticle>[] RefreshSimulation(object[] parameters) { return this.RefreshSimulation((ParticleTree<P>)parameters[0]); }

		protected virtual void InteractTree(ParticleTree<P> tree) {
			Parallel.ForEach(
				tree.Leaves,
				Parameters.MulithreadedOptions,
				leaf => {
					foreach (P p in leaf.NodeElements) {
						p.AccumulatedImpulse = new double[p.DIMENSIONALITY];
						p.Interact(leaf.GetNeighbors().Except(p2 => p2.ID == p.ID));
			}});
		}

		public virtual Tuple<char, AParticle[]>[] Resample(object[] parameters) {
			Tuple<double[], AParticle>[] particleData = (Tuple<double[], AParticle>[])parameters[0];
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

		public IEnumerable<IGrouping<int, dynamic>> DiscreteParticleBin(Tuple<double[], AParticle>[] particleData) { 
			return particleData//Parameters.RENDER_3D_PHI
				.Where(pd => pd.Item1.Select((x, d) => x >= 0 && x < Parameters.DOMAIN[d]).All())
				.Select(pd => new {
					X = pd.Item1[0] * Renderer.RenderWidth / Parameters.DOMAIN[0],
					Y = pd.Item1.Length < 2 ? 0d : pd.Item1[1] * Renderer.RenderHeight / Parameters.DOMAIN[1] / 2d,
					Particle = pd.Item2})
				.GroupBy(pd => (int)pd.X + Renderer.RenderWidthOffset + Parameters.WINDOW_WIDTH*((int)pd.Y + Renderer.RenderHeightOffset));
		}

		public void AutoscaleUpdate(Tuple<char, AParticle[]>[] sampling) {
			double[] orderedDensities = sampling.Except(t => t is null).Select(t => this.GetDensity(t.Item2)).Order().ToArray();
			bool isDiscrete = Parameters.DIMENSIONALITY < 3;
			if (orderedDensities.Any()) {
				int totalBands = Parameters.COLOR_ARRAY.Length;

				double curVal = 0d, newVal = 0d, lastVal = 0d;
				int percentileIdx;
				for (int bandIdx = 0; bandIdx < this.DensityScale.Length; bandIdx++) {//skip last band
					percentileIdx = (int)((double)orderedDensities.Length * (bandIdx + 1d) / totalBands);

					if (percentileIdx < orderedDensities.Length) {
						curVal = this.DensityScale[bandIdx];
						newVal = orderedDensities[percentileIdx];
					}

					if (isDiscrete && newVal - lastVal < 1d)
						newVal = lastVal + 1d;

					this.DensityScale[bandIdx] = newVal;
					lastVal = newVal;
				}
			}
		}
		public void AutoscaleUpdate(object[] parameters) { this.AutoscaleUpdate((Tuple<char, AParticle[]>[])parameters[0]); }

		public ConsoleColor ChooseColor(P[] particles) {
			int rank;
			switch (Parameters.COLOR_SCHEME) {
				case ParticleColoringMethod.Density:
					double density = this.GetDensity(particles);
					rank = Program.Simulator.DensityScale.TakeWhile(a => a < density).Count();
					rank = rank < Parameters.COLOR_ARRAY.Length ? rank : Parameters.COLOR_ARRAY.Length - 1;
					return Parameters.COLOR_ARRAY[rank];
				case ParticleColoringMethod.Group:
					return this.ChooseGroupColor(particles);
				case ParticleColoringMethod.Depth:
					if (Parameters.DIMENSIONALITY > 2) {
						int numColors = Parameters.COLOR_ARRAY.Length;
						double depth = 1d - particles.Min(p => GetDepthScalar(p.Coordinates));
						rank = Program.Simulator.DensityScale.Take(numColors - 1).TakeWhile(a => a < depth).Count();
						return Parameters.COLOR_ARRAY[rank];
					} else return Parameters.COLOR_ARRAY[^1];
				default:
					throw new InvalidEnumArgumentException(nameof(Parameters.COLOR_SCHEME));
			}
		}
		public ConsoleColor ChooseColor(AParticle[] particles) { return this.ChooseColor(particles.Cast<P>().ToArray()); }

		public virtual ConsoleColor ChooseGroupColor(P[] particles) {
			int dominantGroupID;
			if (Parameters.DIMENSIONALITY > 2)
				dominantGroupID = particles.MinBy(p => this.GetDepthScalar(p.TrueCoordinates)).GroupID;
			else dominantGroupID  = particles.GroupBy(p => p.GroupID).MaxBy(g => g.Count()).Key;
			return Parameters.COLOR_ARRAY[dominantGroupID % Parameters.COLOR_ARRAY.Length];
			//return (ConsoleColor)(1 + (dominantGroupID % 15));
		}

		public virtual double GetDensity(AParticle[] particles) {
			if (Parameters.DIMENSIONALITY > 2)
				return particles.Sum(p => this.GetDepthScalar(p.TrueCoordinates));
			else return particles.Length;
		}

		public double GetDepthScalar(double[] v) {
			return 1d - (v.Skip(2).ToArray().Magnitude() / Parameters.DOMAIN_HIDDEN_DIMENSIONAL_HEIGHT);
		}

		public IEnumerator<AParticle> GetEnumerator() { return this.AllParticles.AsEnumerable().GetEnumerator(); }
		IEnumerator IEnumerable.GetEnumerator() {return this.AllParticles.GetEnumerator(); }
	}
}