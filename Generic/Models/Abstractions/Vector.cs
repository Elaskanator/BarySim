using System;
using System.Linq;

namespace Generic.Models {
	public class Vector {
		public virtual double[] Coordinates { get; set; }
		public int Dimensionality { get { return this.Coordinates.Length; } }

		public Vector(int dimensionality) {
			this.Coordinates = new double[dimensionality];
		}
		public Vector(double[] v) {
			this.Coordinates = v;
		}

		public static explicit operator Vector(double[] v) => new Vector(v);
		public static implicit operator double[](Vector v) => v.Coordinates;

		public override string ToString() {
			return string.Format("Vector<{0}>", string.Join(",", this.Coordinates.Select(c => c.ToString("G5"))));
		}
	}

	public static class VectorFunctions {
		public static double[] Add(double[] v1, double[] v2) {
			return v1.Select((n, i) => n + v2[i]).ToArray();
		}
		public static double[] Subtract(double[] v1, double[] v2) {
			return v1.Select((n, i) => n - v2[i]).ToArray();
		}
		public static double[] Multiply(double[] v, double scalar) {
			return v.Select(n => n * scalar).ToArray();
		}
		public static double[] Divide(double[] v, double scalar) {
			return v.Select(n => n / scalar).ToArray();
		}

		/// <summary>
		/// Normalizes a vector to have a Euclidean length of 1
		/// </summary>
		/// <param name="v">The vector to normalize</param>
		public static double[] Normalize(double[] v) {
			double magnitude = Magnitude(v);
			if (magnitude > 0) {
				double componentScale = 1d / magnitude;
				return v.Select(n => n * componentScale).ToArray();
			} else
				return v;
		}

		public static double[] Clamp(double[] v, double maxMagnitude) {
			double magnitude = Magnitude(v);
			if (magnitude > maxMagnitude) {
				return v.Select(n => n * maxMagnitude / magnitude).ToArray();
			} else {
				return v;
			}
		}

		public static double Magnitude(double[] v) {
			return Distance(v, Enumerable.Repeat(0d, v.Length).ToArray());
		}
		public static double Distance(double[] v1, double[] v2) {
			return Math.Sqrt(
				Enumerable
					.Range(0, v1.Length)
					.Select(i => v1[i] - v2[i])
					.Select(x => x * x)
					.Sum());
		}

		public static double Dot(double[] v1, double[] v2) {
			return Enumerable.Range(0, v1.Length).Aggregate(0d, (xs, d) => xs + (v1[d] * v2[d]));
		}

		public static double AngleTo(double[] v1, double[] v2) {
			double
				dot = Dot(v1, v2),
				len1 = Magnitude(v1),
				len2 = Magnitude(v2);
			if (len1 == 0 || len2 == 0)
				return 0;
			else
				return Math.Acos(dot / len1 / len2);
		}
	}
}