using System;
using System.Collections.Generic;
using System.Linq;
using Generic.Extensions;
using Generic.Models;

namespace ParticleSimulator.Simulation {
	public class Autoscaler {
		public Autoscaler(double[] fixedBands = null) {
			if (fixedBands is null) {
				if (Parameters.COLOR_METHOD == ParticleColoringMethod.Depth)
					this.Values =
						Enumerable
							.Range(1, Parameters.COLOR_ARRAY.Length)
							.Select(x => (double)x / Parameters.COLOR_ARRAY.Length)
							.ToArray();
				else if (Parameters.COLOR_METHOD == ParticleColoringMethod.Group)
					this.Values =
						Enumerable
							.Range(1, Parameters.COLOR_ARRAY.Length)
							.Select(x => (double)x)
							.ToArray();
				else this.Values = new double[0];
			} else this.Values = fixedBands.Take(Parameters.COLOR_ARRAY.Length).ToArray();
		}

		public double[] Values { get; private set; }

		public void Update(object[] parameters) {
			Tuple<char, IParticle[], double>[] sampling = ((Tuple<char, IParticle[], double>[])parameters[0]).Without(t => t is null).ToArray();
			double[] densities;
			densities = sampling.Select(t => t.Item3).ToArray();
			//densities = Program.Simulator.AllParticles.Where(p => p.IsActive).Select(p => p.Mass).ToArray();

			if (densities.Length > 0) {
				StatsInfo stats = new(densities);
				double max = stats.Data_asc[^1];
				if (Parameters.AUTOSCALE_CUTOFF_PCT > 0d) {
					stats.FilterData(Parameters.AUTOSCALE_CUTOFF_PCT);
					if (stats.Data_asc.Length == 0)
						return;
				}

				if (Parameters.AUTOSCALE_PERCENTILE) {
					List<double> results = new(Parameters.COLOR_ARRAY.Length);
					int position, diff,
						totalBands = Parameters.COLOR_ARRAY.Length < stats.Data_asc.Length
							? Parameters.COLOR_ARRAY.Length
							: stats.Data_asc.Length;
					double newValue, threshold;

					int bandIdx;
					for (bandIdx = 0; bandIdx < totalBands && stats.Data_asc.Length > 0; bandIdx++) {
						newValue = stats.GetPercentileValue(100d * bandIdx / (totalBands + 1d), true);

						if (results.Count == 0) {
							results.Add(newValue);
							stats.Data_asc = stats.Data_asc
								.SkipWhile(d => d == newValue)
								.ToArray();
						} else {
							position = 0;
							threshold = results[^1];
							threshold = threshold == 0d
								? Parameters.AUTOSCALE_MIN_STEP
								: Parameters.AUTOSCALE_MIN_STEP < 0d
									? threshold * (1d + 0.01d*Math.Sign(threshold))
									: threshold + Parameters.AUTOSCALE_MIN_STEP;

							while (newValue < threshold) {
								diff = stats.Data_asc.Skip(position).TakeWhile(x => x <= newValue).Count();
								position += diff;
								if (position < stats.Data_asc.Length) {
									if (diff > 0)
										newValue = stats.Data_asc[position];
								} else break;
							}
							if (position < stats.Data_asc.Length) {
								if (newValue <= threshold) {
									totalBands -= bandIdx;
									bandIdx = 0;
								}
								results.Add(newValue);
								stats.Data_asc = stats.Data_asc
									.Skip(position)
									.SkipWhile(d => d < threshold)
									.ToArray();
							}
						}
					}
					results[^1] = max;

					this.Values = results.ToArray();
				} else {
					double
						min = stats.Data_asc[0],
						range = max - min;
					if (range > Parameters.WORLD_EPSILON) {
						double step = range / (Parameters.COLOR_ARRAY.Length + 1);
						this.Values = Enumerable.Range(1, Parameters.COLOR_ARRAY.Length).Select(i => min + step*i).ToArray();
					} else this.Values = new double[] { max };
				}
			}
		}
	}
}