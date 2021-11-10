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
		public IEnumerable<AParticle> AllParticles { get; }
		public SampleSMA[] DensityScale { get; }

		public ITree RebuildTree();
		public Tuple<double[], double>[] Simulate(ITree tree);
		public Tuple<char, double>[] RasterizeDensities(Tuple<double[], double>[] particles);
		public void AutoscaleUpdate(Tuple<char, double>[] sampling);
	}

	public abstract class AParticleSimulator<P, T> : IParticleSimulator
	where P : AParticle
	where T : ATree<P> {
		public AParticleSimulator(Random rand = null) {
			this._rand = rand ?? new Random();
			this.DensityScale = Enumerable
				.Range(1, Parameters.DENSITY_COLORS.Length - 1)
				.Select(x => new SampleSMA(Parameters.AUTOSCALING_SMA_ALPHA, x))
				.ToArray();
		}
		
		public abstract bool IsDiscrete { get; }
		public abstract IEnumerable<P> AllParticles { get; }
		IEnumerable<AParticle> IParticleSimulator.AllParticles => this.AllParticles;
		public SampleSMA[] DensityScale { get; private set; }
		protected readonly Random _rand;

		public abstract T NewTree { get; }

		public T RebuildTree() {
			if (Program.ENABLE_DEBUG_LOGGING) DebugExtensions.DebugWriteline("BuildTree - Start");

			T tree = this.NewTree;
			tree.AddRange(this.AllParticles);

			if (Program.ENABLE_DEBUG_LOGGING) DebugExtensions.DebugWriteline("BuildTree - End");
			return tree;
		}
		ITree IParticleSimulator.RebuildTree() { return this.RebuildTree(); }

		public Tuple<double[], double>[] Simulate(T tree) {
			if (Program.ENABLE_DEBUG_LOGGING) DebugExtensions.DebugWriteline("Simulate - Start");

			DateTime startUtc = DateTime.UtcNow;

			this.ComputeUpdate(tree);
			Parallel.ForEach(this.AllParticles, p => p.ApplyUpdate());

			Tuple<double[], double>[] result = this.AllParticles.Select(p => new Tuple<double[], double>(p.Coordinates, p.Mass)).ToArray();

			if (Parameters.PERF_ENABLE) PerfMon.AfterSimulate(startUtc);

			if (Program.ENABLE_DEBUG_LOGGING) DebugExtensions.DebugWriteline("Simulate - End");
			return result;
		}
		public Tuple<double[], double>[] Simulate(ITree tree) { return this.Simulate((T)tree); }

		protected abstract void ComputeUpdate(T tree);

		public Tuple<char, double>[] RasterizeDensities(Tuple<double[], double>[] particles) {
			if (Program.ENABLE_DEBUG_LOGGING) DebugExtensions.DebugWriteline("Rasterize - Start");
			DateTime startUtc = DateTime.UtcNow;

			Tuple<char, double>[] result = this.Resample(particles);
			
			if (Parameters.PERF_ENABLE) PerfMon.AfterRasterize(startUtc);
			if (Program.ENABLE_DEBUG_LOGGING) DebugExtensions.DebugWriteline("Rasterize - End");
			return result;
		}

		protected virtual Tuple<char, double>[] Resample(Tuple<double[], double>[] particles) {
			Tuple<char, double>[] results = new Tuple<char, double>[Parameters.WINDOW_WIDTH * Parameters.WINDOW_HEIGHT];

			int topCount, bottomCount;
			double colorCount;
			char pixelChar;
			foreach (IGrouping<int, double[]> xGroup
			in particles.Select(p => p.Item1).GroupBy(c => (int)(Renderer.RenderWidth * c[0] / Parameters.DOMAIN[0]))) {
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
			if (Program.ENABLE_DEBUG_LOGGING) DebugExtensions.DebugWriteline("Autoscale - Start");

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
						this.DensityScale[band - 1].Update(newValRounded, Program.Step_Rasterizer.IterationCount <= 1 ? 1d : null);
					} else if (band > 1 && newValRounded <= this.DensityScale[band - 2].Current) {
						this.DensityScale[band - 1].Update(this.DensityScale[band - 2].Current + 1, 1d);
					} else {
						this.DensityScale[band - 1].Update(newValRounded, 1d);
					}
				}
			}

			if (Program.ENABLE_DEBUG_LOGGING) DebugExtensions.DebugWriteline("Autoscale - End");
		}

		public IEnumerator<AParticle> GetEnumerator() { return this.AllParticles.GetEnumerator(); }
		IEnumerator IEnumerable.GetEnumerator() {return this.AllParticles.GetEnumerator(); }
	}
}