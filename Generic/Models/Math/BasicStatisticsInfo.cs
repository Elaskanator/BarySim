using System;
using System.Collections.Generic;
using System.Linq;
using Generic.Extensions;

namespace Generic.Models {
	public class BasicStatisticsInfo {
		public readonly bool EnableInterpolation = true;

		public readonly double[] Data_asc;
		public readonly double Percentile0;
		public readonly double Percentile10;
		public readonly double Percentile25;
		public readonly double Percentile50;
		public readonly double Percentile75;
		public readonly double Percentile90;
		public readonly double Percentile100;

		public double Min { get { return this.Percentile0; } }
		public double Median { get { return this.Percentile50; } }
		public double Max { get { return this.Percentile100; } }
		
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

			this.Percentile0 = this.Data_asc[0];
			this.Percentile10 = this.GetPercentileValue(10);
			this.Percentile25 = this.GetPercentileValue(25);
			this.Percentile50 = this.GetPercentileValue(50);
			this.Percentile75 = this.GetPercentileValue(75);
			this.Percentile90 = this.GetPercentileValue(90);
			this.Percentile100 = this.Data_asc[^1];

			this.Mean = this.Data_asc.Average();
			this.StdDev = Math.Sqrt(this.Data_asc.Average(d => (d-this.Mean)*(d-this.Mean)));
		}
	}
}