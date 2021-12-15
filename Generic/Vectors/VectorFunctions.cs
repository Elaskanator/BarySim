using System;
using System.Linq;
using Generic.Extensions;

namespace Generic.Vectors {
	public static class VectorFunctions {
		public static double[] Negate(this double[] v) {
			return v.Select(n => -n).ToArray();
		}
		public static VectorDouble Negate(this VectorDouble v) { return (VectorDouble)Negate(v.Coordinates); }

		public static double[] Add(this double[] v1, double[] v2) {
			return v1.Select((n, i) => n + v2[i]).ToArray();
		}
		public static VectorDouble Addition(this VectorDouble v1, VectorDouble v2) { return (VectorDouble)Add(v1.Coordinates, v2.Coordinates); }

		public static double[] Subtract(this double[] v1, double[]v2) {
			return v1.Select((n, i) => n - v2[i]).ToArray();
		}
		public static VectorDouble Subtract(this VectorDouble v1, VectorDouble v2) { return (VectorDouble)Subtract(v1.Coordinates, v2.Coordinates); }

		public static double[] Multiply(this double[] v, double scalar) {
			return v.Select(n => n * scalar).ToArray();
		}
		public static VectorDouble Multiply(this VectorDouble v, double scalar) { return (VectorDouble)Multiply(v.Coordinates, scalar); }

		public static double[] Divide(this double[] v, double scalar) {
			return v.Select(n => n / scalar).ToArray();
		}
		public static VectorDouble Divide(this VectorDouble v, double scalar) { return (VectorDouble)Divide(v.Coordinates, scalar); }

		/// <summary>
		/// Normalizes a vector to have a Euclidean length of 1
		/// </summary>
		/// <param name="v">The vector to normalize</param>
		public static double[] Normalize(this double[] v, double len = 1d) {
			double magnitude = Magnitude(v);
			return magnitude == len
				? v
				: v.Multiply(len / magnitude);
		}
		public static VectorDouble Normalize(this VectorDouble v, double len = 1d) { return (VectorDouble)Normalize(v.Coordinates, len); }

		public static double[] Clamp(this double[] v, double maxMagnitude) {
			double magnitude = Magnitude(v);
			return magnitude > maxMagnitude
				? v.Multiply(maxMagnitude / magnitude)
				: v;
		}
		public static VectorDouble Clamp(this VectorDouble v, double maxMagnitude) { return (VectorDouble)Clamp(v.Coordinates, maxMagnitude); }

		public static double Magnitude(this double[] v) {
			return Math.Sqrt(v.Sum(x => x * x));
		}
		public static double Magnitude(this VectorDouble v) { return Magnitude(v.Coordinates); }

		public static double Distance(this double[] v1, double[] v2) {
			return Math.Sqrt(
				Enumerable
					.Range(0, v1.Length)
					.Select(i => v1[i] - v2[i])
					.Select(x => x * x)
					.Sum());
		}
		public static double Distance(this VectorDouble v1, VectorDouble v2) { return Distance(v1.Coordinates, v2.Coordinates); }

		public static double DotProduct(this double[] v1, double[] v2) {
			return v1.Zip(v2, (x1, x2) => x1 * x2).Sum();
		}
		public static double DotProduct(this VectorDouble v1, VectorDouble v2) { return DotProduct(v1.Coordinates, v2.Coordinates); }

		public static double AngleTo(this double[] v1, double[] v2) {
			double
				dot = DotProduct(v1, v2),
				len1 = Magnitude(v1),
				len2 = Magnitude(v2);
			if (len1 == 0d || len2 == 0d)
				return 0d;
			else return Math.Acos(dot / len1 / len2);
		}
		public static double AngleTo(this VectorDouble v1, VectorDouble v2) { return AngleTo(v1.Coordinates, v2.Coordinates); }
	}
}