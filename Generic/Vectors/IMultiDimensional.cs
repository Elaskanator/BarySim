using System.Numerics;

namespace Generic.Vectors {
	public interface IMultiDimensional<T> {
		T Position { get; }

		int BitmaskEqual(T other, int? dim = null);

		int BitmaskGreaterThanOrEqual(T other, int? dim = null);
		int BitmaskGreaterThan(T other, int? dim = null);

		int BitmaskLessThanOrEqual(T other, int? dim = null);
		int BitmaskLessThan(T other, int? dim = null);

		string ToString() => string.Format("Location{0}", string.Join("", this.Position));
	}

	public interface IMultidimensionalFloat : IMultiDimensional<Vector<float>> {
		int IMultiDimensional<Vector<float>>.BitmaskEqual(Vector<float> other, int? dim = null) => MultidimensionalSIMD.BitmaskEqual(this.Position, other, dim);

		int IMultiDimensional<Vector<float>>.BitmaskGreaterThanOrEqual(Vector<float> other, int? dim = null) => MultidimensionalSIMD.BitmaskGreaterThanOrEqual(this.Position, other, dim);
		int IMultiDimensional<Vector<float>>.BitmaskGreaterThan(Vector<float> other, int? dim = null) => MultidimensionalSIMD.BitmaskGreaterThan(this.Position, other, dim);

		int IMultiDimensional<Vector<float>>.BitmaskLessThanOrEqual(Vector<float> other, int? dim = null) => MultidimensionalSIMD.BitmaskLessThanOrEqual(this.Position, other, dim);
		int IMultiDimensional<Vector<float>>.BitmaskLessThan(Vector<float> other, int? dim = null) => MultidimensionalSIMD.BitmaskLessThan(this.Position, other, dim);
	}
}