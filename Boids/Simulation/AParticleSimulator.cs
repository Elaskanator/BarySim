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
		public AParticleSimulator() {
			this.DensityScale = Enumerable
				.Range(1, Parameters.DENSITY_COLORS.Length - 1)
				.Select(x => new SampleSMA(Parameters.AUTOSCALING_SMA_ALPHA, x))
				.ToArray();
		}
		
		public abstract bool IsDiscrete { get; }
		public abstract IEnumerable<P> AllParticles { get; }
		IEnumerable<AParticle> IParticleSimulator.AllParticles { get { return this.AllParticles; } }
		public SampleSMA[] DensityScale { get; private set; }
		protected readonly Random _rand = new();

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

			if (Parameters.DEBUG_ENABLE) PerfMon.AfterSimulate(startUtc);

			if (Program.ENABLE_DEBUG_LOGGING) DebugExtensions.DebugWriteline("Simulate - End");
			return result;
		}
		public Tuple<double[], double>[] Simulate(ITree tree) { return this.Simulate((T)tree); }
		protected abstract void ComputeUpdate(T tree);

		public Tuple<char, double>[] RasterizeDensities(Tuple<double[], double>[] particles) {
			if (Program.ENABLE_DEBUG_LOGGING) DebugExtensions.DebugWriteline("Rasterize - Start");
			DateTime startUtc = DateTime.UtcNow;

			Tuple<char, double>[] result = this.Resample(particles, Renderer.RenderWidth, Renderer.RenderHeight);
			
			if (Parameters.DEBUG_ENABLE) PerfMon.AfterRasterize(startUtc);
			if (Program.ENABLE_DEBUG_LOGGING) DebugExtensions.DebugWriteline("Rasterize - End");
			return result;
		}
		protected abstract Tuple<char, double>[] Resample(Tuple<double[], double>[] particles, double width, double height);

		public void AutoscaleUpdate(Tuple<char, double>[] sampling) {
			if (Program.ENABLE_DEBUG_LOGGING) DebugExtensions.DebugWriteline("Autoscale - Start");

			double[] orderedCounts = sampling.Except(c => c is null).Select(c => c.Item2).Order().ToArray();//TODO use selection sort?
			if (orderedCounts.Length > 0) {
				int totalBands = Parameters.DENSITY_COLORS.Length - 1;

				int lastBand = 0;
				int idx;
				int bandValue;
				for (int band = 1; band <= totalBands; band++) {
					idx = (int)(((double)orderedCounts.Length * band / (totalBands + 1d)) - 1d);

					if (orderedCounts.Length > idx) {
						bandValue = (int)orderedCounts[idx];
						if (bandValue > lastBand) {
							this.DensityScale[band - 1].Update(bandValue, Program.Step_Rasterizer.IterationCount <= 1 || bandValue == 1 ? 1d : null);
							lastBand = (int)this.DensityScale[band - 1].Current;
						} else {
							this.DensityScale[band - 1].Update(++lastBand);
						}
					} else {
						this.DensityScale[band - 1].Update(++lastBand);
					}
				}
			}
			if (Program.ENABLE_DEBUG_LOGGING) DebugExtensions.DebugWriteline("Autoscale - End");
		}

		public IEnumerator<AParticle> GetEnumerator() { return this.AllParticles.GetEnumerator(); }
		IEnumerator IEnumerable.GetEnumerator() {return this.AllParticles.GetEnumerator(); }
	}
}