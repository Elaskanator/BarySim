using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Generic.Extensions;
using Generic.Models;
using ParticleSimulator.Rendering;

namespace ParticleSimulator.Simulation {
	public interface IParticleSimulator : IEnumerable<AParticle> {
		public bool IsDiscrete { get; }
		public AParticle[] AllParticles { get; }
		public SampleSMA[] DensityScale { get; }
		public int? InteractionLimit { get; }
		public int? NeighborhoodFilteringDepth { get; }

		public ITree RebuildTree();
		public Tuple<double[], object>[] RefreshSimulation(object[] parameters);
		public Tuple<char, double>[] ResampleDensities(object[] parameters);
		public void AutoscaleUpdate(object[] parameters);
	}

	public abstract class AParticleSimulator<P, G, T> : IParticleSimulator
	where P : AParticle
	where G : AParticleGroup<P>
	where T : AQuadTree<P, T> {
		public AParticleSimulator(Random rand = null) {
			this._rand = rand ?? new Random();
			this.ParticleGroups = Enumerable.Range(0, Parameters.NUM_PARTICLE_GROUPS).Select(i => this.NewParticleGroup(rand)).ToArray();
			this.AllParticles = this.ParticleGroups.SelectMany(g => g.Particles).ToArray();
			this.DensityScale = Enumerable
				.Range(1, Parameters.DENSITY_COLORS.Length - 1)
				.Select(x => new SampleSMA(Parameters.AUTOSCALING_SMA_ALPHA, x))
				.ToArray();
		}
		public virtual int? InteractionLimit => Parameters.DESIRED_NEIGHBORS;
		public virtual int? NeighborhoodFilteringDepth => null;
		
		public abstract bool IsDiscrete { get; }
		public G[] ParticleGroups { get; private set; }
		public P[] AllParticles { get; private set; }
		AParticle[] IParticleSimulator.AllParticles => this.AllParticles;
		public SampleSMA[] DensityScale { get; private set; }
		protected readonly Random _rand;

		public abstract T NewTree { get; }
		public abstract G NewParticleGroup(Random rand);

		public T RebuildTree() {
			Parallel.ForEach(this.AllParticles, p => p.Coordinates = p.TrueCoordinates);//make sure to update with true coordinates before recalcuating tree (to avoid race condition)

			T tree = this.NewTree;
			tree.AddRange(this.AllParticles, this._rand);

			return tree;
		}
		ITree IParticleSimulator.RebuildTree() { return this.RebuildTree(); }

		public Tuple<double[], object>[] RefreshSimulation(T tree) {
			DateTime startUtc = DateTime.UtcNow;

			this.InteractTree(tree);
			Parallel.ForEach(this.AllParticles, p => p.ApplyUpdate());

			Tuple<double[], object>[] result = this.AllParticles.Select(p => new Tuple<double[], object>(p.TrueCoordinates, null)).ToArray();

			return result;
		}
		public Tuple<double[], object>[] RefreshSimulation(object[] parameters) { return this.RefreshSimulation((T)parameters[0]); }

		protected virtual void InteractTree(T tree) {
			Parallel.ForEach(tree.Leaves, leaf => {
				P[] neighbors;
				if (this.InteractionLimit.HasValue)
					neighbors = leaf.GetNeighbors().Take(this.InteractionLimit.Value + 1).ToArray();
				else neighbors = leaf.GetNeighbors().ToArray();
				foreach (P b in leaf.NodeElements)
					b.InteractMany(neighbors);
			});
		}

		public Tuple<char, double>[] ResampleDensities(object[] parameters) { return this.ResampleDensities((Tuple<double[], object>[])parameters[0]); }

		protected virtual Tuple<char, double>[] ResampleDensities(Tuple<double[], object>[] particleData) {
			Tuple<char, double>[] results = new Tuple<char, double>[Parameters.WINDOW_WIDTH * Parameters.WINDOW_HEIGHT];

			int topCount, bottomCount;
			double colorCount;
			char pixelChar;
			foreach (IGrouping<int, double[]> xGroup
			in particleData.Select(p => p.Item1).GroupBy(c => (int)(Renderer.RenderWidth * c[0] / Parameters.DOMAIN[0]))) {
				foreach (IGrouping<int, double> yGroup
				in xGroup//subdivide each pixel into two vertical components
					.Select(c => Parameters.DOMAIN.Length < 2 ? 0 : Renderer.RenderHeight * c[1] / Parameters.DOMAIN[1] / 2d)
					.GroupBy(y => (int)y))//preserve floating point value of normalized Y for subdivision
				{
					topCount = yGroup.Count(y => y % 1d < 0.5d);
					bottomCount = yGroup.Count() - topCount;

					if (topCount > 0 && bottomCount > 0) {
						pixelChar = Parameters.CHAR_BOTH;
						colorCount = ((double)topCount + bottomCount) / 2d;
					} else if (topCount > 0) {
						pixelChar = Parameters.CHAR_TOP;
						colorCount = topCount;
					} else {
						pixelChar = Parameters.CHAR_BOTTOM;
						colorCount = bottomCount;
					}

					results[xGroup.Key + Renderer.RenderWidthOffset + Parameters.WINDOW_WIDTH*(yGroup.Key + Renderer.RenderHeightOffset)] =
						new Tuple<char, double>(
							pixelChar,
							colorCount);
				}
			}

			return results;
		}

		public void AutoscaleUpdate(Tuple<char, double>[] sampling) {
			double[] orderedCounts = sampling.Except(c => c is null).Select(c => c.Item2).Order().ToArray();//TODO use selection sort?
			if (orderedCounts.Length > 0) {
				int totalBands = Parameters.DENSITY_COLORS.Length - 1;

				double curVal = 0d, curValRounded = 0d;
				double newVal = 0d, newValRounded = 0d;
				int percentilIdx;
				for (int band = 1; band <= totalBands; band++) {
					percentilIdx = (int)(((double)orderedCounts.Length * band / (totalBands + 1d)) - 1d);

					if (orderedCounts.Length > percentilIdx) {
						curVal = curValRounded = this.DensityScale[band - 1].Current;
						newVal = newValRounded = orderedCounts[percentilIdx];
						if (Program.Simulator.IsDiscrete) {
							curValRounded = Math.Floor(curVal);
							newValRounded = Math.Ceiling(newVal);
						}
					} else {
						newVal = newValRounded = curVal = ++curValRounded;
					}

					if (newValRounded > curVal) {
						this.DensityScale[band - 1].Update(newValRounded, Program.StepEval_Resample.NumCompleted <= 1 ? 1d : null);
					} else if (band > 1 && newValRounded <= this.DensityScale[band - 2].Current) {
						this.DensityScale[band - 1].Update(this.DensityScale[band - 2].Current + 1, 1d);
					} else {
						this.DensityScale[band - 1].Update(newValRounded, 1d);
					}
				}
			}
		}
		public void AutoscaleUpdate(object[] parameters) { this.AutoscaleUpdate((Tuple<char, double>[])parameters[0]); }

		public IEnumerator<AParticle> GetEnumerator() { return this.AllParticles.AsEnumerable().GetEnumerator(); }
		IEnumerator IEnumerable.GetEnumerator() {return this.AllParticles.GetEnumerator(); }
	}

	public abstract class ASymmetricalInteractionParticleSimulator<P, G, T> : AParticleSimulator<P, G, T>
	where P : ASymmetricParticle
	where G : AParticleGroup<P>
	where T : AQuadTree<P, T> {
		public ASymmetricalInteractionParticleSimulator(Random rand = null)
		: base(rand) { }

		protected override void InteractTree(T tree) {
			throw new NotImplementedException();
		}
	}
}