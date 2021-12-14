using System;
using System.Collections.Generic;
using System.Linq;
using Generic.Extensions;
using Generic.Models;

namespace ParticleSimulator.Simulation {
	public class Scaling {
		public Scaling() {
			if (Parameters.COLOR_SCHEME == ParticleColoringMethod.Depth)
				this.Values =
					Enumerable
						.Range(1, Parameters.COLOR_ARRAY.Length)
						.Select(x => (double)x / Parameters.COLOR_ARRAY.Length)
						.ToArray();
			else if (Parameters.COLOR_SCHEME == ParticleColoringMethod.Group)
				this.Values =
					Enumerable
						.Range(1, Parameters.COLOR_ARRAY.Length)
						.Select(x => (double)x)
						.ToArray();
			else this.Values = new double[0];
		}

		public double[] Values { get; private set; }

		public void Update(object[] parameters) {
			Tuple<char, AClassicalParticle[], double>[] sampling = ((Tuple<char, AClassicalParticle[], double>[])parameters[0]).Without(t => t is null).ToArray();
			double[] densities;
			densities = sampling.Select(t => t.Item3).ToArray();
			//densities = Program.Simulator.AllParticles.Where(p => p.IsActive).Select(p => p.Mass).ToArray();

			if (densities.Length > 0) {
				StatsInfo stats = new(densities);
				if (Parameters.DENSITY_AUTOSCALE_CUTOFF_PCT > 0d) {
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
					this.Values = results.ToArray();
				} else {
					double
						min = stats.Data_asc[0],
						max = stats.Data_asc[^1],
						range = max - min;
					if (range > Parameters.WORLD_EPSILON) {
						double step = range / (Parameters.COLOR_ARRAY.Length + 1);
						this.Values = Enumerable.Range(1, Parameters.COLOR_ARRAY.Length).Select(i => min + step*i).ToArray();
					} else this.Values = new double[] { min };
				}
			}
		}
	}
}