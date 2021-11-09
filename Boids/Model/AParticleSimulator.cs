using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using Generic.Extensions;
using Generic.Models;

namespace Simulation {
	public interface IParticleSimulator : IEnumerable<AParticle> {
		public abstract IEnumerable<AParticle> AllParticles { get; }
		public SampleSMA[] DensityScale { get; }

		public abstract ITree BuildTree(IEnumerable<AParticle> particles);

		public abstract double[][] Simulate(ITree tree);

		public void AutoscaleUpdate(Tuple<char, double>[] sampling);

		public void DrawLegend(ConsoleExtensions.CharInfo[] buffer);
	}

	public abstract class AParticleSimulator<T> : IParticleSimulator
	where T : AParticle {
		public AParticleSimulator() {
			this.DensityScale = Enumerable
				.Range(1, Parameters.DENSITY_COLORS.Length - 1)
				.Select(x => new SampleSMA(Parameters.AUTOSCALING_SMA_ALPHA, x))
				.ToArray();
		}

		public abstract IEnumerable<T> AllParticles { get; }
		IEnumerable<AParticle> IParticleSimulator.AllParticles { get { return this.AllParticles; } }
		public SampleSMA[] DensityScale { get; private set; }
		protected readonly Random _rand = new();

		public abstract ATree<T> BuildTree(IEnumerable<T> particles);
		ITree IParticleSimulator.BuildTree(IEnumerable<AParticle> particles) { return this.BuildTree(particles.Cast<T>()); }

		public abstract double[][] Simulate(ATree<T> tree);
		public double[][] Simulate(ITree tree) { return this.Simulate((ATree<T>)tree); }

		public virtual void DrawLegend(ConsoleExtensions.CharInfo[] buffer) {
			int pixelIdx = Parameters.WINDOW_WIDTH * (Parameters.WINDOW_HEIGHT - Parameters.DENSITY_COLORS.Length);
			string strData;
			for (int cIdx = 0; cIdx < Parameters.DENSITY_COLORS.Length; cIdx++) {
				buffer[pixelIdx] = new ConsoleExtensions.CharInfo(
					Rasterizer.CHAR_BOTH,
					Parameters.DENSITY_COLORS[cIdx]);

				if (cIdx == 0) strData = "≤" + ((int)this.DensityScale[cIdx].Current).ToString("G4");
				else if (cIdx < this.DensityScale.Length) strData = "=" + ((int)this.DensityScale[cIdx].Current).ToString("G4");
				else strData = ">";

				for (int sIdx = 0; sIdx < strData.Length; sIdx++)
					buffer[pixelIdx + sIdx + 1] = new ConsoleExtensions.CharInfo(strData[sIdx], ConsoleColor.White);

				pixelIdx += Parameters.WINDOW_WIDTH;
			}
		}

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