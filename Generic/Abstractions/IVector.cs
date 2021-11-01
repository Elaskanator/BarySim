using System.Linq;

namespace Generic.Abstractions {
	public interface IVector{
		public double[] Coordinates { get; }
	}

	public struct SimpleVector : IVector {
		private double[] _coordinates;
		public double[] Coordinates { get { return this._coordinates; } }

		public static explicit operator SimpleVector(double[] v) => new SimpleVector() { _coordinates = v };
		public static implicit operator double[](SimpleVector v) => v.Coordinates;

		public override string ToString() {
			return string.Format("Vector<{0}>", string.Join(",", this.Coordinates.Select(c => c.ToString("G5"))));
		}
	}
}