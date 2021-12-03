using System;
using System.Collections.Generic;
using System.Linq;
using Generic.Extensions;

namespace Generic.Models {
	public class StatsInfo {
		public StatsInfo(params double[] data) {
			this.Data_asc = data.Order().ToArray();
		}
		public StatsInfo(IEnumerable<double> data)
		: this(data.ToArray()) { }

		public double[] Data_asc { get; private set; }
		public double Min => this.Data_asc[0];
		public double Median => this.GetPercentileValue(50);
		public double Max => this.Data_asc[^1];
		public double Mean => this.Data_asc.Average();
		public double StdDev => Math.Sqrt(this.Data_asc.Average(d => (d-this.Mean)*(d-this.Mean)));

		public double GetPercentileValue(double pct, bool enableInterpolation = true) {
			double idx = (this.Data_asc.Length - 1) * pct / 100d;
			if (enableInterpolation && idx % 1d > 0) {
				return this.Data_asc[(int)idx]
					+ (idx % 1d) * (this.Data_asc[(int)idx + 1] - this.Data_asc[(int)idx]);
			} else return this.Data_asc[(int)idx];
		}

		public void FilterData(double cutPct) {
			int lowIdx = (int)((this.Data_asc.Length - 1) * cutPct / 100d),
				highIdx = (int)Math.Ceiling(this.Data_asc.Length * (100d - cutPct) / 100d);
			this.Data_asc = this.Data_asc.Skip(lowIdx).Take(highIdx - lowIdx).ToArray();
		}
	}
}