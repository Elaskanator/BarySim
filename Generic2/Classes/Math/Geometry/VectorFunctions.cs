using System;
using System.Linq;

namespace Generic {
	public static class VectorFunctions {
		public static double[] Add(this double[] v1, double[] v2) {
			return v1.Select((n, i) => n + v2[i]).ToArray();
		}
		public static double[] Subtract(this double[] v1, double[] v2) {
			return v1.Select((n, i) => n - v2[i]).ToArray();
		}
		public static double[] Multiply(this double[] v, double scalar) {
			return v.Select(n => n * scalar).ToArray();
		}
		public static double[] Divide(this double[] v, double scalar) {
			return v.Select(n => n / scalar).ToArray();
		}

		/// <summary>
		/// Normalizes a vector to have a Euclidean length of 1
		/// </summary>
		/// <param name="v">The vector to normalize</param>
		public static double[] Normalize(this double[] v) {
			double magnitude = v.Magnitude();
			if (magnitude > 0) {
				double componentScale = 1 / v.Magnitude();
				return v.Select(n => n * componentScale).ToArray();
			} else return v;
		}
		
		public static double Magnitude(this double[] v) {
			return Distance(v, Enumerable.Repeat(0d, v.Length).ToArray());
		}
		public static double Distance(this double[] v1, double[] v2) {
			return Math.Sqrt(
				Enumerable
					.Range(0, v1.Length)
					.Select(i => v1[i] - v2[i])
					.Select(x => x*x)
					.Sum());
		}

		public static double[] Clamp(this double[] v, double maxMagnitude) {
			double magnitude = v.Magnitude();
			if (magnitude > maxMagnitude) {
				return v.Select(n => n * maxMagnitude / magnitude).ToArray();
			} else {
				return v;
			}
		}

		public static double Dot(this double[] v1, double[] v2) {
			return Enumerable.Range(0, v1.Length).Aggregate(0d, (xs, d) => xs + (v1[d] * v2[d]));
		}

		public static double AngleTo(this double[] v1, double[] v2) {
			double
				dot = v1.Dot(v2),
				len1 = v1.Magnitude(),
				len2 = v2.Magnitude();
			if (len1 == 0 || len2 == 0) return 0;
			else return Math.Acos(dot / len1 / len2);
		}
	}
}