using System;
using System.Collections.Generic;
using System.Linq;
using Generic.Extensions;
using Generic.Classes;
using ParticleSimulator.Engine.Threading;

namespace ParticleSimulator.Rendering {
	public class Autoscaler {
		public Autoscaler(SynchronousBuffer<float[]> resource) {
			this._resource = resource;

			if (Parameters.COLOR_USE_FIXED_BANDS)
				this.Values = Parameters.COLOR_FIXED_BANDS ?? new float[0];
			else if (Parameters.COLOR_METHOD == ParticleColoringMethod.Depth) {
				int minDim = Parameters.WINDOW_WIDTH > Parameters.WINDOW_HEIGHT ? Parameters.WINDOW_HEIGHT : Parameters.WINDOW_WIDTH;
				float range = MathF.Sqrt(3f) * minDim / 2f;
				this.Values = Enumerable.Range(1, Parameters.COLOR_ARRAY.Length).Select(i => -range + (i * 2f*range / (Parameters.COLOR_ARRAY.Length + 1f))).ToArray();
			} else this.Values = Enumerable
					.Range(
						Parameters.COLOR_METHOD == ParticleColoringMethod.Group ? 0
							: Parameters.COLOR_METHOD == ParticleColoringMethod.Random ? 0
							: 1,
						Parameters.COLOR_ARRAY.Length)
					.Select(i => (float)i)
					.ToArray();

			this._resource.Overwrite(this.Values);
			this.ValuesInitial = (float[])this.Values.Clone();
		}

		public readonly float[] ValuesInitial;
		public float[] Values { get; private set; }

		private SimpleExponentialMovingAverage _min = new SimpleExponentialMovingAverage(Parameters.AUTOSCALE_STRENGTH);
		private SimpleExponentialMovingAverage _max = new SimpleExponentialMovingAverage(Parameters.AUTOSCALE_STRENGTH);
		private readonly SynchronousBuffer<float[]> _resource;
		private readonly object _lock = new();

		public void Reset() {
			lock (this._lock) {
				this.Values = (float[])this.ValuesInitial.Clone();
				this._min.Reset();
				this._max.Reset();
				this._resource.Overwrite(this.Values);
			}
		}

		public float[] Update(EvalResult prepResults, object[] parameters) {
			float[] scalingValues = ((float?[])parameters[0]).Without(t => t is null).Select(t => t.Value).ToArray();

			if (scalingValues.Length > 0) {
				StatsInfo stats = new(scalingValues.Select(x => (double)x));
				float max = (float)stats.Data_asc[^1];
				if (Parameters.AUTOSCALE_CUTOFF_PCT > 0f) {
					stats.FilterData(Parameters.AUTOSCALE_CUTOFF_PCT);
					if (stats.Data_asc.Length == 0)
						return Array.Empty<float>();
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
						newValue = (float)stats.GetPercentileValue(100d * (bandIdx + 1d) / (totalBands + 1d), true);

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
									? threshold * (1f + Parameters.AUTOSCALE_DIFF_THRESH*MathF.Sign(threshold))
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
					lock (this._lock) {
						this._min.Update(stats.GetPercentileValue(100d / (Parameters.COLOR_ARRAY.Length + 1)));
						this._max.Update(stats.GetPercentileValue(100d * (1d - 1d / (Parameters.COLOR_ARRAY.Length + 1))));
					}
					float min, range;

					if (Parameters.AUTOSCALE_FIXED_MIN >= 0 && Parameters.AUTOSCALE_FIXED_MAX >= 0) {
						min = Parameters.AUTOSCALE_FIXED_MIN;
						max = Parameters.AUTOSCALE_FIXED_MAX;
					} else if (Parameters.AUTOSCALE_FIXED_MIN >= 0) {
						if (Parameters.AUTOSCALE_FIXED_MIN > (float)this._max.Current) {
							min = Parameters.AUTOSCALE_FIXED_MIN;
							max = Parameters.AUTOSCALE_FIXED_MIN;
						} else {
							min = Parameters.AUTOSCALE_FIXED_MIN;
							max = (float)this._max.Current;
						}
					} else if (Parameters.AUTOSCALE_FIXED_MAX >= 0) {
						min = Parameters.AUTOSCALE_FIXED_MAX / (Parameters.COLOR_ARRAY.Length + 1);
						max = Parameters.AUTOSCALE_FIXED_MAX;
					} else {
						min = (float)this._min.Current;
						max = (float)this._max.Current;
					}
					range = max - min;
					int numSteps = Parameters.COLOR_ARRAY.Length;
					numSteps = numSteps <= Parameters.COLOR_ARRAY.Length ? numSteps : Parameters.COLOR_ARRAY.Length;
					if (numSteps > 0) {
						float step = range / (numSteps + 1);
						this.Values = Enumerable.Range(1, numSteps).Select(i => min + step*i).ToArray();
					} else this.Values = new float[] { max };
				}
			}
			return this.Values;
		}
	}
}