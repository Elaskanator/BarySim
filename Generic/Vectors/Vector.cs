using System;
using System.Linq;

namespace Generic.Vectors {
	public interface IVectorDouble : ICloneable {
		public double[] Coordinates { get; }
		public int DIM { get; }
	}

	public class VectorDouble : IVectorDouble {
		public virtual double[] Coordinates { get; set; }
		public int DIM { get { return this.Coordinates.Length; } }

		public VectorDouble() { }
		protected VectorDouble(double[] coordinates) {
			this.Coordinates = coordinates;
		}

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

		public object Clone() { return new VectorDouble((double[])this.Coordinates.Clone()); }

		public override string ToString() { return string.Format("Vector<{0}>", string.Join(",", this.Coordinates.Select(c => c.ToString("G5")))); }
	}
}