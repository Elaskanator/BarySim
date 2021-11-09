using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Generic.Models {
	public class SimpleVector : IEnumerable<double>{
		public int Dimensionality { get { return this.Coordinates.Length; } }
		public virtual double[] Coordinates { get; set; }
		public double this[int dimension] => this.Coordinates[dimension];

		public SimpleVector(int dimensionality) { this.Coordinates = new double[dimensionality]; }
		public SimpleVector(double[] v) { this.Coordinates = v; }

		public static implicit operator SimpleVector(double[] v) => new SimpleVector(v);
		public static implicit operator double[](SimpleVector v) => v.Coordinates;

		public static SimpleVector operator -(SimpleVector v) { return v.Negate(); }
		public static SimpleVector operator +(SimpleVector v1, SimpleVector v2) { return v1.Add(v2); }
		public static SimpleVector operator -(SimpleVector v1, SimpleVector v2) { return v1.Subtract(v2); }
		public static SimpleVector operator *(SimpleVector v, double scalar) { return v.Multiply(scalar); }
		public static SimpleVector operator *(double scalar, SimpleVector v) { return v.Multiply(scalar); }
		public static SimpleVector operator /(SimpleVector v, double scalar) { return v.Divide(scalar); }

		public IEnumerator<double> GetEnumerator() { return this.Coordinates.AsEnumerable().GetEnumerator(); }
		IEnumerator IEnumerable.GetEnumerator() { return this.Coordinates.GetEnumerator(); }
		public override string ToString() { return string.Format("Vector<{0}>", string.Join(",", this.Coordinates.Select(c => c.ToString("G5")))); }
	}

	public static class VectorFunctions {
		public static SimpleVector Negate(this SimpleVector v) {
			return v.Select(n => -n).ToArray();
		}
		public static SimpleVector Add(this SimpleVector v1, SimpleVector v2) {
			return v1.Select((n, i) => n + v2[i]).ToArray();
		}
		public static SimpleVector Subtract(this SimpleVector v1, SimpleVector v2) {
			return v1.Select((n, i) => n - v2[i]).ToArray();
		}
		public static SimpleVector Multiply(this SimpleVector v, double scalar) {
			return v.Select(n => n * scalar).ToArray();
		}
		public static SimpleVector Divide(this SimpleVector v, double scalar) {
			return v.Select(n => n / scalar).ToArray();
		}

		/// <summary>
		/// Normalizes a vector to have a Euclidean length of 1
		/// </summary>
		/// <param name="v">The vector to normalize</param>
		public static SimpleVector Normalize(this SimpleVector v) {
			double magnitude = Magnitude(v);
			if (magnitude > 0) {
				double componentScale = 1d / magnitude;
				return v.Select(n => n * componentScale).ToArray();
			} else
				return v;
		}

		public static SimpleVector Clamp(this SimpleVector v, double maxMagnitude) {
			double magnitude = Magnitude(v);
			if (magnitude > maxMagnitude) {
				return v.Select(n => n * maxMagnitude / magnitude).ToArray();
			} else {
				return v;
			}
		}

		public static double Magnitude(this SimpleVector v) {
			return Distance(v, Enumerable.Repeat(0d, v.Dimensionality).ToArray());
		}
		public static double Distance(this SimpleVector v1, SimpleVector v2) {
			return Math.Sqrt(
				Enumerable
					.Range(0, v1.Dimensionality)
					.Select(i => v1[i] - v2[i])
					.Select(x => x * x)
					.Sum());
		}

		public static double Dot(this SimpleVector v1, SimpleVector v2) {
			return Enumerable.Range(0, v1.Dimensionality).Aggregate(0d, (xs, d) => xs + (v1[d] * v2[d]));
		}

		public static double AngleTo(this SimpleVector v1, SimpleVector v2) {
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