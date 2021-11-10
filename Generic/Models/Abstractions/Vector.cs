using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Generic.Models {
	public class SimpleVector : ICollection<double>, IEnumerable<double>, IList<double>, ICollection, IEnumerable, IList, IStructuralComparable, IStructuralEquatable, ICloneable {
		public int Dimensionality { get { return this.Coordinates.Length; } }
		public virtual double[] Coordinates { get; set; }

		double IList<double>.this[int index] { get => this.Coordinates[index]; set => this.Coordinates[index] = value; }
		object IList.this[int index] { get => this.Coordinates[index]; set => this.Coordinates[index] = (double)value; }

		public SimpleVector(int dimensionality) { this.Coordinates = new double[dimensionality]; }
		public SimpleVector(double[] v) { this.Coordinates = v; }

		public double this[int dimension] => this.Coordinates[dimension];

		public static explicit operator SimpleVector(double[] v) => new SimpleVector(v);

		public static SimpleVector operator - (SimpleVector v) { return v.Negate(); }
		public static SimpleVector operator + (SimpleVector v1, SimpleVector v2) { return v1.Addition(v2); }
		public static SimpleVector operator - (SimpleVector v1, SimpleVector v2) { return v1.Subtract(v2); }
		public static SimpleVector operator * (SimpleVector v, double scalar) { return v.Multiply(scalar); }
		public static SimpleVector operator * (double scalar, SimpleVector v) { return v.Multiply(scalar); }
		public static SimpleVector operator / (SimpleVector v, double scalar) { return v.Divide(scalar); }

		public bool IsFixedSize => true;
		public bool IsReadOnly => false;
		public int Count => this.Coordinates.Length;
		public bool IsSynchronized => false;
		private readonly object _sync = new();
		public object SyncRoot => this._sync;

		public bool Contains(object value) { return this.Coordinates.Contains((double)value); }
		public int IndexOf(object value) { return Array.IndexOf(this.Coordinates, (double)value); }
		public void Insert(int index, object value) { this.Coordinates[index] = (double)value; }
		
		public int Add(object value) { throw new NotSupportedException(); }
		public void Add(double item) { throw new NotSupportedException(); }
		public void Clear() { throw new NotSupportedException(); }
		public bool Remove(double item) { throw new NotSupportedException(); }
		public void Remove(object value) { throw new NotSupportedException(); }
		public void RemoveAt(int index) { throw new NotSupportedException(); }

		public void CopyTo(Array array, int index) { this.Coordinates.CopyTo(array, index); }
		public int CompareTo(object other, IComparer comparer) { throw new NotSupportedException(); }

		public bool Equals(object other, IEqualityComparer comparer) { return Enumerable.SequenceEqual(this.Coordinates, other as IEnumerable<double>); }
		public int GetHashCode(IEqualityComparer comparer) { return this.Coordinates.GetHashCode(); }
		public object Clone() { return this.Coordinates.Clone(); }
		public int IndexOf(double item) { return Array.IndexOf(this.Coordinates, item); }
		public void Insert(int index, double item) { this.Coordinates[index] = item; }
		public bool Contains(double item) { return this.Coordinates.Contains(item); }
		public void CopyTo(double[] array, int arrayIndex) { this.Coordinates.CopyTo(array, arrayIndex); }

		public IEnumerator<double> GetEnumerator() { return this.Coordinates.AsEnumerable().GetEnumerator(); }
		IEnumerator IEnumerable.GetEnumerator() { return this.Coordinates.GetEnumerator(); }

		public override string ToString() { return string.Format("Vector<{0}>", string.Join(",", this.Coordinates.Select(c => c.ToString("G5")))); }
	}

	public static class VectorFunctions {
		public static double[] Negate(this double[] v) {
			return v.Select(n => -n).ToArray();
		}
		public static SimpleVector Negate(this SimpleVector v) { return (SimpleVector)Negate(v.Coordinates); }

		public static double[] Addition(this double[] v1, double[] v2) {
			return v1.Select((n, i) => n + v2[i]).ToArray();
		}
		public static SimpleVector Addition(this SimpleVector v1, SimpleVector v2) { return (SimpleVector)Addition(v1.Coordinates, v2.Coordinates); }

		public static double[] Subtract(this double[] v1, double[]v2) {
			return v1.Select((n, i) => n - v2[i]).ToArray();
		}
		public static SimpleVector Subtract(this SimpleVector v1, SimpleVector v2) { return (SimpleVector)Subtract(v1.Coordinates, v2.Coordinates); }

		public static double[] Multiply(this double[] v, double scalar) {
			return v.Select(n => n * scalar).ToArray();
		}
		public static SimpleVector Multiply(this SimpleVector v, double scalar) { return (SimpleVector)Multiply(v.Coordinates, scalar); }

		public static double[] Divide(this double[] v, double scalar) {
			return v.Select(n => n / scalar).ToArray();
		}
		public static SimpleVector Divide(this SimpleVector v, double scalar) { return (SimpleVector)Divide(v.Coordinates, scalar); }

		/// <summary>
		/// Normalizes a vector to have a Euclidean length of 1
		/// </summary>
		/// <param name="v">The vector to normalize</param>
		public static double[] Normalize(this double[] v) {
			double magnitude = Magnitude(v);
			if (magnitude > 0) {
				double componentScale = 1d / magnitude;
				return v.Select(n => n * componentScale).ToArray();
			} else return v;
		}
		public static SimpleVector Normalize(this SimpleVector v) { return (SimpleVector)Normalize(v.Coordinates); }

		public static double[] Clamp(this double[] v, double maxMagnitude) {
			double magnitude = Magnitude(v);
			if (magnitude > maxMagnitude)
				return v.Select(n => n * maxMagnitude / magnitude).ToArray();
			else return v;
		}
		public static SimpleVector Clamp(this SimpleVector v, double maxMagnitude) { return (SimpleVector)Clamp(v.Coordinates, maxMagnitude); }

		public static double Magnitude(this double[] v) {
			return Math.Sqrt(v.Sum(x => x * x));
		}
		public static double Magnitude(this SimpleVector v) { return Magnitude(v.Coordinates); }

		public static double Distance(this double[] v1, double[] v2) {
			return Math.Sqrt(
				Enumerable
					.Range(0, v1.Length)
					.Select(i => v1[i] - v2[i])
					.Select(x => x * x)
					.Sum());
		}
		public static double Distance(this SimpleVector v1, SimpleVector v2) { return Distance(v1.Coordinates, v2.Coordinates); }

		public static double Dot(this double[] v1, double[] v2) {
			return Enumerable.Range(0, v1.Length).Aggregate(0d, (xs, d) => xs + (v1[d] * v2[d]));
		}
		public static double Dot(this SimpleVector v1, SimpleVector v2) { return Dot(v1.Coordinates, v2.Coordinates); }

		public static double AngleTo(this double[] v1, double[] v2) {
			double
				dot = Dot(v1, v2),
				len1 = Magnitude(v1),
				len2 = Magnitude(v2);
			if (len1 == 0 || len2 == 0) return 0;
			else return Math.Acos(dot / len1 / len2);
		}
		public static double AngleTo(this SimpleVector v1, SimpleVector v2) { return AngleTo(v1.Coordinates, v2.Coordinates); }
	}
}