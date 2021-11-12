using System;
using System.Collections.Generic;
using System.Linq;
using Generic.Extensions;

namespace Generic.Models {
	public class BasicStatisticsInfo {
		public readonly bool EnableInterpolation = true;

		public readonly double[] Data_asc;
		public readonly double Min;
		public double Median { get { return this.GetPercentileValue(50); } }
		public readonly double Max;
		public readonly double Mean;
		public readonly double StdDev;

		public double GetPercentileValue(double pct) {
			double idx = (this.Data_asc.Length - 1) * pct / 100d;
			if (this.EnableInterpolation && idx % 1d > 0) {
				return this.Data_asc[(int)idx]
					+ (idx % 1d) * (this.Data_asc[(int)idx + 1] - this.Data_asc[(int)idx]);
			} else return this.Data_asc[(int)idx];
		}

		public BasicStatisticsInfo(IEnumerable<double> data) {
			if (data is null) throw new ArgumentNullException();
			this.Data_asc = data.Order().ToArray();
			if (this.Data_asc.Length == 0) throw new ArgumentException();

			this.Min = this.Data_asc[0];
			this.Max = this.Data_asc[^1];

			this.Mean = this.Data_asc.Average();
			this.StdDev = Math.Sqrt(this.Data_asc.Average(d => (d-this.Mean)*(d-this.Mean)));
		}
	}
}