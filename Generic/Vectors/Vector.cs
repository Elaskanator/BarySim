using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Generic.Vectors {
	public class VectorDouble : ICollection<double>, IEnumerable<double>, IList<double>, ICollection, IEnumerable, IList, IStructuralComparable, IStructuralEquatable, ICloneable {
		public int DIM { get { return this.Coordinates.Length; } }
		public virtual double[] Coordinates { get; set; }

		public VectorDouble() {
			this.Coordinates = null;
		}
		protected VectorDouble(double[] coordinates) {
			this.Coordinates = coordinates;
		}

		double IList<double>.this[int index] { get => this.Coordinates[index]; set => this.Coordinates[index] = value; }
		object IList.this[int index] { get => this.Coordinates[index]; set => this.Coordinates[index] = (double)value; }

		public static readonly VectorDouble Zero1D = new(new double[1]);
		public static readonly VectorDouble Zero2D = new(new double[2]);
		public static readonly VectorDouble Zero3D = new(new double[3]);

		public double this[int dimension] => this.Coordinates[dimension];

		public static explicit operator VectorDouble(double[] v) => new VectorDouble(v);

		public static VectorDouble operator - (VectorDouble v) { return v.Negate(); }
		public static VectorDouble operator + (VectorDouble v1, VectorDouble v2) { return v1.Addition(v2); }
		public static VectorDouble operator - (VectorDouble v1, VectorDouble v2) { return v1.Subtract(v2); }
		public static VectorDouble operator * (VectorDouble v, double scalar) { return v.Multiply(scalar); }
		public static VectorDouble operator * (double scalar, VectorDouble v) { return v.Multiply(scalar); }
		public static VectorDouble operator / (VectorDouble v, double scalar) { return v.Divide(scalar); }

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
}