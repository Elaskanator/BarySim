using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Generic.Models {
	public interface IVector : ICollection, IEnumerable, IList, IStructuralComparable, IStructuralEquatable, ICloneable {
		public int Dimensionality { get; }
		public object[] Coordinates { get; }
		public new object this[int dimension] { get; }
	}
	public interface IVector<T> : IVector, ICollection<T>, IEnumerable<T>, IList<T>
	where T :IComparable<T> {
		public new T[] Coordinates { get; }
		public new T this[int dimension] { get; }
	}
	public abstract class AVector<T> : IVector<T>
	where T :IComparable<T> {
		public int Dimensionality { get { return this.Coordinates.Length; } }
		public virtual T[] Coordinates { get; set; }
		object[] IVector.Coordinates => this.Coordinates.Cast<object>().ToArray();

		T IList<T>.this[int index] { get => this.Coordinates[index]; set => this.Coordinates[index] = value; }
		object IList.this[int index] { get => this.Coordinates[index]; set => this.Coordinates[index] = (T)value; }

		protected AVector(int dimensionality) { this.Coordinates = new T[dimensionality]; }
		protected AVector(T[] v) { this.Coordinates = v; }
		protected AVector(IVector<T> v) { this.Coordinates = v.Coordinates; }

		public T this[int dimension] => this.Coordinates[dimension];
		object IVector.this[int dimension] => this.Coordinates[dimension];

		public IEnumerator<T> GetEnumerator() { return this.Coordinates.AsEnumerable().GetEnumerator(); }
		IEnumerator IEnumerable.GetEnumerator() { return this.Coordinates.GetEnumerator(); }

		public bool IsFixedSize => true;
		public bool IsReadOnly => false;
		public int Count => this.Coordinates.Length;
		public bool IsSynchronized => false;
		private readonly object _sync = new();
		public object SyncRoot => this._sync;

		public bool Contains(object value) { return this.Coordinates.Contains((T)value); }
		public int IndexOf(object value) { return Array.IndexOf(this.Coordinates, (T)value); }
		public void Insert(int index, object value) { this.Coordinates[index] = (T)value; }
		
		public int Add(object value) { throw new NotSupportedException(); }
		public void Add(T item) { throw new NotSupportedException(); }
		public void Clear() { throw new NotSupportedException(); }
		public bool Remove(T item) { throw new NotSupportedException(); }
		public void Remove(object value) { throw new NotSupportedException(); }
		public void RemoveAt(int index) { throw new NotSupportedException(); }

		public void CopyTo(Array array, int index) { this.Coordinates.CopyTo(array, index); }
		public int CompareTo(object other, IComparer comparer) { throw new NotSupportedException(); }

		public bool Equals(object other, IEqualityComparer comparer) { return Enumerable.SequenceEqual(this.Coordinates, other as IEnumerable<T>); }
		public int GetHashCode(IEqualityComparer comparer) { return this.Coordinates.GetHashCode(); }
		public object Clone() { return this.Coordinates.Clone(); }
		public int IndexOf(T item) { return Array.IndexOf(this.Coordinates, item); }
		public void Insert(int index, T item) { this.Coordinates[index] = item; }
		public bool Contains(T item) { return this.Coordinates.Contains(item); }
		public void CopyTo(T[] array, int arrayIndex) { this.Coordinates.CopyTo(array, arrayIndex); }

	}
	public class VectorFloat : AVector<float> {
		public VectorFloat(int dimensionality) : base(dimensionality) { }
		public VectorFloat(float[] v) : base(v) { }

		public static implicit operator VectorFloat(float[] v) => new VectorFloat(v);
		public static implicit operator float[](VectorFloat v) => v.Coordinates;

		public static VectorFloat operator -(VectorFloat v) { return v.Negate(); }
		public static VectorFloat operator +(VectorFloat v1, VectorFloat v2) { return v1.Addition(v2); }
		public static VectorFloat operator -(VectorFloat v1, VectorFloat v2) { return v1.Subtract(v2); }
		public static VectorFloat operator *(VectorFloat v, float scalar) { return v.Multiply(scalar); }
		public static VectorFloat operator *(float scalar, VectorFloat v) { return v.Multiply(scalar); }
		public static VectorFloat operator /(VectorFloat v, float scalar) { return v.Divide(scalar); }

		public override string ToString() { return string.Format("Vector<{0}>", string.Join(",", this.Coordinates.Select(c => c.ToString("G5")))); }
	}
	public class VectorDouble : AVector<double> {
		public VectorDouble(int dimensionality) : base(dimensionality) { }
		public VectorDouble(double[] v) : base(v) { }

		public static implicit operator VectorDouble(double[] v) => new VectorDouble(v);
		public static implicit operator double[](VectorDouble v) => v.Coordinates;

		public static VectorDouble operator -(VectorDouble v) { return v.Negate(); }
		public static VectorDouble operator +(VectorDouble v1, VectorDouble v2) { return v1.Addition(v2); }
		public static VectorDouble operator -(VectorDouble v1, VectorDouble v2) { return v1.Subtract(v2); }
		public static VectorDouble operator *(VectorDouble v, double scalar) { return v.Multiply(scalar); }
		public static VectorDouble operator *(double scalar, VectorDouble v) { return v.Multiply(scalar); }
		public static VectorDouble operator /(VectorDouble v, double scalar) { return v.Divide(scalar); }

		public override string ToString() { return string.Format("Vector<{0}>", string.Join(",", this.Coordinates.Select(c => c.ToString("G5")))); }
	}

	public static class VectorFunctions {
		public static double[] Negate(this double[] v) {
			return v.Select(n => -n).ToArray();
		}
		public static double[] Negate(this IVector<double> v) {
			return v.Select(n => -n).ToArray();
		}
		public static float[] Negate(this float[] v) {
			return v.Select(n => -n).ToArray();
		}
		public static float[] Negate(this IVector<float> v) {
			return v.Select(n => -n).ToArray();
		}

		public static double[] Addition(this double[] v1, double[] v2) {
			return v1.Select((n, i) => n + v2[i]).ToArray();
		}
		public static double[] Addition(this IVector<double> v1, IVector<double> v2) {
			return v1.Select((n, i) => n + v2.Coordinates[i]).ToArray();
		}
		public static float[] Addition(this float[] v1, float[] v2) {
			return v1.Select((n, i) => n + v2[i]).ToArray();
		}
		public static float[] Addition(this IVector<float> v1, IVector<float> v2) {
			return v1.Select((n, i) => n + v2.Coordinates[i]).ToArray();
		}

		public static double[] Subtract(this double[] v1, double[]v2) {
			return v1.Select((n, i) => n - v2[i]).ToArray();
		}
		public static double[] Subtract(this IVector<double> v1, IVector<double> v2) {
			return v1.Select((n, i) => n - v2.Coordinates[i]).ToArray();
		}
		public static float[] Subtract(this float[] v1, float[] v2) {
			return v1.Select((n, i) => n - v2[i]).ToArray();
		}
		public static float[] Subtract(this IVector<float> v1, IVector<float> v2) {
			return v1.Select((n, i) => n - v2.Coordinates[i]).ToArray();
		}

		public static double[] Multiply(this double[] v, double scalar) {
			return v.Select(n => n * scalar).ToArray();
		}
		public static double[] Multiply(this IVector<double> v, double scalar) {
			return v.Select(n => n * scalar).ToArray();
		}
		public static float[] Multiply(this float[] v, float scalar) {
			return v.Select(n => n * scalar).ToArray();
		}
		public static float[] Multiply(this IVector<float> v, float scalar) {
			return v.Select(n => n * scalar).ToArray();
		}

		public static double[] Divide(this double[] v, double scalar) {
			return v.Select(n => n / scalar).ToArray();
		}
		public static double[] Divide(this IVector<double> v, double scalar) {
			return v.Select(n => n / scalar).ToArray();
		}
		public static float[] Divide(this float[] v, float scalar) {
			return v.Select(n => n / scalar).ToArray();
		}
		public static float[] Divide(this IVector<float> v, float scalar) {
			return v.Select(n => n / scalar).ToArray();
		}

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
		public static float[] Normalize(this float[] v) {
			float magnitude = Magnitude(v);
			if (magnitude > 0) {
				float componentScale = 1f / magnitude;
				return v.Select(n => n * componentScale).ToArray();
			} else return v;
		}

		public static double[] Clamp(this double[] v, double maxMagnitude) {
			double magnitude = Magnitude(v);
			if (magnitude > maxMagnitude)
				return v.Select(n => n * maxMagnitude / magnitude).ToArray();
			else return v;
		}
		public static float[] Clamp(this float[] v, float maxMagnitude) {
			float magnitude = Magnitude(v);
			if (magnitude > maxMagnitude)
				return v.Select(n => n * maxMagnitude / magnitude).ToArray();
			else return v;
		}

		public static double Magnitude(this double[] v) {
			return Math.Sqrt(v.Sum(x => x * x));
		}
		public static float Magnitude(this float[] v) {
			return MathF.Sqrt(v.Sum(x => x * x));
		}

		public static double Distance(this double[] v1, double[] v2) {
			return Math.Sqrt(
				Enumerable
					.Range(0, v1.Length)
					.Select(i => v1[i] - v2[i])
					.Select(x => x * x)
					.Sum());
		}
		public static float Distance(this float[] v1, float[] v2) {
			return MathF.Sqrt(
				Enumerable
					.Range(0, v1.Length)
					.Select(i => v1[i] - v2[i])
					.Select(x => x * x)
					.Sum());
		}
		public static float Distance(this IVector<float> v1, IVector<float> v2) { return v1.Coordinates.Distance(v2.Coordinates); }

		public static double Dot(this double[] v1, double[] v2) {
			return Enumerable.Range(0, v1.Length).Aggregate(0d, (xs, d) => xs + (v1[d] * v2[d]));
		}
		public static float Dot(this float[] v1, float[] v2) {
			return Enumerable.Range(0, v1.Length).Aggregate(0f, (xs, d) => xs + (v1[d] * v2[d]));
		}

		public static double AngleTo(this double[] v1, double[] v2) {
			double
				dot = Dot(v1, v2),
				len1 = Magnitude(v1),
				len2 = Magnitude(v2);
			if (len1 == 0 || len2 == 0) return 0;
			else return Math.Acos(dot / len1 / len2);
		}
		public static float AngleTo(this float[] v1, float[] v2) {
			float
				dot = Dot(v1, v2),
				len1 = Magnitude(v1),
				len2 = Magnitude(v2);
			if (len1 == 0 || len2 == 0) return 0;
			else return MathF.Acos(dot / len1 / len2);
		}
	}
}