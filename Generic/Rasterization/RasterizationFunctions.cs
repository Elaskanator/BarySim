using System;
using System.Collections.Generic;
using System.Linq;
using Generic;
using Generic.Abstractions;

namespace Generic {
	public static class RasterizationFunctions {
		#region Grouping
		public static double[][] GroupToArray(IEnumerable<double> data, int range, double domainBegin, double domainEnd) {
			double[][] results = new double[range][];
			foreach (IGrouping<int, double> g in Group(data, range, domainBegin, domainEnd))
				results[g.Key] = g.ToArray();
			return results;
		}
		public static double[][][] GroupToArray(IEnumerable<double[]> data, int range, double domainBegin, double domainEnd, int dimension = 0) {
			double[][][] results = new double[range][][];
			foreach (IGrouping<int, double[]> g in Group(data, range, domainBegin, domainEnd, dimension))
				results[g.Key] = g.ToArray();
			return results;
		}
		public static T[][] GroupToArray<T>(IEnumerable<T> data, int range, double domainBegin, double domainEnd, int dimension = 0)
		where T : IVector {
			T[][] results = new T[range][];
			foreach (IGrouping<int, T> g in Group(data, range, domainBegin, domainEnd, dimension))
				results[g.Key] = g.ToArray();
			return results;
		}

		public static double[][] GroupToArray(IEnumerable<double> data, int range, double domainEnd, int dimension = 0) {
			double[][] results = new double[range][];
			foreach (IGrouping<int, double> g in Group(data, range, domainEnd, dimension))
				results[g.Key] = g.ToArray();
			return results;
		}
		public static double[][][] GroupToArray(IEnumerable<double[]> data, int range, double domainEnd, int dimension = 0) {
			double[][][] results = new double[range][][];
			foreach (IGrouping<int, double[]> g in Group(data, range, domainEnd, dimension))
				results[g.Key] = g.ToArray();
			return results;
		}
		public static T[][] GroupToArray<T>(IEnumerable<T> data, int range, double domainEnd, int dimension = 0)
		where T : IVector {
			T[][] results = new T[range][];
			foreach (IGrouping<int, T> g in Group(data, range, domainEnd, dimension))
				results[g.Key] = g.ToArray();
			return results;
		}


		public static IEnumerable<IGrouping<int, double>> Group(IEnumerable<double> data, int range, double domainBegin, double domainEnd) {
			return data.GroupBy(d => (int)(range * (d - domainBegin) / (domainEnd - domainBegin)));
		}
		public static IEnumerable<IGrouping<int, double[]>> Group(IEnumerable<double[]> data, int range, double domainBegin, double domainEnd, int dimension = 0) {
			return data.GroupBy(d => (int)(range * (d[dimension] - domainBegin) / (domainEnd - domainBegin)));
		}
		public static IEnumerable<IGrouping<int, T>> Group<T>(IEnumerable<T> data, int range, double domainBegin, double domainEnd, int dimension = 0)
		where T : IVector {
			return data.GroupBy(t => (int)(range * (t.Coordinates[dimension] - domainBegin) / (domainEnd - domainBegin)));
		}

		public static IEnumerable<IGrouping<int, double>> Group(IEnumerable<double> data, int range, double domainEnd)  {
			return data.GroupBy(d => (int)(range * d / domainEnd));
		}
		public static IEnumerable<IGrouping<int, double[]>> Group(IEnumerable<double[]> data, int range, double domainEnd, int dimension = 0)  {
			return data.GroupBy(d => (int)(range * d[dimension] / domainEnd));
		}
		public static IEnumerable<IGrouping<int, T>> Group<T>(IEnumerable<T> data, int range, double domainEnd, int dimension = 0)
		where T : IVector {
			return data.GroupBy(t => (int)(range * t.Coordinates[dimension] / domainEnd));
		}
		#endregion Grouping

		#region Resampling
		public static TrackingIncrementalAverage[] Resample(IEnumerable<double> values, int expected, int range) {
			TrackingIncrementalAverage[] results = Enumerable.Range(0, range).Select(i => new TrackingIncrementalAverage()).ToArray();

			double step = (double)range / expected;
			double startX = 0d, endX = step;
			foreach (double v in values) {
				for (int x = (int)startX; x < endX; x++) {
					if (x < startX) results[x].Update(v, startX - x);
					else if (x + 1d <= endX) results[x].Update(v, 1d);
					else results[x].Update(v, endX - x);
				}

				startX += step;
				endX += step;
			}

			return results;
		}
		//public static double?[] Resample2(IEnumerable<double> values, int expected, int range) {
		//}
		#endregion Resampling
	}
}