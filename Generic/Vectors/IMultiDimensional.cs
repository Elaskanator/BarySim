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

	public static class MultidimensionalSIMD {
		public static int BitmaskEqual(Vector<float> first, Vector<float> second, int? dim = null) =>
			Vector.Dot(
				Vector.ConditionalSelect(
					Vector.Equals(first, second),
					VectorFunctions.PowersOfTwo,
					Vector<int>.Zero),
				VectorFunctions.DimensionFilters[dim ?? Vector<float>.Count]);
		public static int BitmaskInequal(Vector<float> first, Vector<float> second, int? dim = null) =>
			Vector.Dot(
				Vector.ConditionalSelect(
					Vector.Equals(first, second),
					Vector<int>.Zero,
					VectorFunctions.PowersOfTwo),
				VectorFunctions.DimensionFilters[dim ?? Vector<float>.Count]);
		
		public static int BitmaskGreaterThan(Vector<float> first, Vector<float> second, int? dim = null) =>
			Vector.Dot(
				Vector.ConditionalSelect(
					Vector.GreaterThan(first, second),
					VectorFunctions.PowersOfTwo,
					Vector<int>.Zero),
				VectorFunctions.DimensionFilters[dim ?? Vector<float>.Count]);
		public static int BitmaskGreaterThanOrEqual(Vector<float> first, Vector<float> second, int? dim = null) =>
			Vector.Dot(
				Vector.ConditionalSelect(
					Vector.GreaterThanOrEqual(first, second),
					VectorFunctions.PowersOfTwo,
					Vector<int>.Zero),
				VectorFunctions.DimensionFilters[dim ?? Vector<float>.Count]);
		
		public static int BitmaskLessThan(Vector<float> first, Vector<float> second, int? dim = null) =>
			Vector.Dot(
				Vector.ConditionalSelect(
					Vector.LessThan(first, second),
					VectorFunctions.PowersOfTwo,
					Vector<int>.Zero),
				VectorFunctions.DimensionFilters[dim ?? Vector<float>.Count]);
		public static int BitmaskLessThanOrEqual(Vector<float> first, Vector<float> second, int? dim = null) =>
			Vector.Dot(
				Vector.ConditionalSelect(
					Vector.LessThanOrEqual(first, second),
					VectorFunctions.PowersOfTwo,
					Vector<int>.Zero),
				VectorFunctions.DimensionFilters[dim ?? Vector<float>.Count]);
	}
}