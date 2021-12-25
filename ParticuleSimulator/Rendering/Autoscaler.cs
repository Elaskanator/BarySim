using System;
using System.Collections.Generic;
using System.Linq;
using Generic.Extensions;
using Generic.Models;

namespace ParticleSimulator.Rendering {
	public class Autoscaler {
		public Autoscaler() {
			if (Parameters.COLOR_USE_FIXED_BANDS)
				this.Values = Parameters.COLOR_FIXED_BANDS ?? new float[0];
			else if (Parameters.COLOR_METHOD ==  ParticleColoringMethod.Depth)
				this.Values = Enumerable.Range(1, Parameters.COLOR_ARRAY.Length).Select(i => i / (Parameters.COLOR_ARRAY.Length + 1f)).ToArray();
			else this.Values = Enumerable.Range(0, Parameters.COLOR_ARRAY.Length).Select(i => (float)i).ToArray();
			if (Parameters.COLOR_METHOD == ParticleColoringMethod.Random)
				this._randOffset = (int)(this.Values.Length * Program.Random.NextDouble());
		}

		public float[] Values { get; private set; }
		private int _randOffset = 0;

		public ConsoleColor RankColor(ParticleData particle, int count, float density) {
			if (Parameters.COLOR_METHOD == ParticleColoringMethod.Random) {
				return Parameters.COLOR_ARRAY[(particle.ID + this._randOffset) % this.Values.Length];
			} else if (Parameters.COLOR_METHOD == ParticleColoringMethod.Group) {
				return Parameters.COLOR_ARRAY[(particle.GroupID + this._randOffset) % this.Values.Length];
			} else {
				float rank;
				if (Parameters.COLOR_METHOD == ParticleColoringMethod.Count) {
					rank = count;
				} else if (Parameters.COLOR_METHOD == ParticleColoringMethod.Density) {
					rank = density;
				} else if (Parameters.COLOR_METHOD == ParticleColoringMethod.Luminosity) {
					rank = particle.Luminosity;
				} else return Parameters.COLOR_ARRAY[0];

				return Parameters.COLOR_ARRAY[this.Values.Drop(1).TakeWhile(ds => ds < rank).Count()];
			}
		}

		public void Update(object[] parameters) {
			float[] scalingValues = ((float[])parameters[0]).Without(t => t == float.NegativeInfinity).ToArray();

			if (scalingValues.Length > 0) {
				StatsInfo stats = new(scalingValues.Select(x => (double)x));
				float max = (float)stats.Data_asc[^1];
				if (Parameters.AUTOSCALE_CUTOFF_PCT > 0f) {
					stats.FilterData(Parameters.AUTOSCALE_CUTOFF_PCT);
					if (stats.Data_asc.Length == 0)
						return;
				}

				if (Parameters.AUTOSCALE_PERCENTILE) {
					List<float> results = new(Parameters.COLOR_ARRAY.Length);
					int position, diff,
						totalBands = Parameters.COLOR_ARRAY.Length < stats.Data_asc.Length
							? Parameters.COLOR_ARRAY.Length
							: stats.Data_asc.Length;
					float newValue, threshold;

					int bandIdx;
					for (bandIdx = 0; bandIdx < totalBands && stats.Data_asc.Length > 0; bandIdx++) {
						newValue = (float)stats.GetPercentileValue(100d * bandIdx / (totalBands + 1d), true);

						if (results.Count == 0) {
							results.Add(newValue);
							stats.Data_asc = stats.Data_asc
								.SkipWhile(d => d == newValue)
								.ToArray();
						} else {
							position = 0;
							threshold = results[^1];
							threshold = threshold == 0f
								? Parameters.AUTOSCALE_MIN_STEP
								: Parameters.AUTOSCALE_MIN_STEP < 0f
									? threshold * (1f + 0.01f*MathF.Sign(threshold))
									: threshold + Parameters.AUTOSCALE_MIN_STEP;

							while (newValue < threshold) {
								diff = stats.Data_asc.Skip(position).TakeWhile(x => x <= newValue).Count();
								position += diff;
								if (position < stats.Data_asc.Length) {
									if (diff > 0)
										newValue = (float)stats.Data_asc[position];
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
					float
						min = (float)stats.Data_asc[0],
						range = max - min;
					if (range > Parameters.WORLD_EPSILON) {
						float step = range / (Parameters.COLOR_ARRAY.Length + 1);
						this.Values = Enumerable.Range(1, Parameters.COLOR_ARRAY.Length).Select(i => min + step*i).ToArray();
					} else this.Values = new float[] { max };
				}
			}
		}
	}
}