using System.Numerics;

namespace Generic.Vectors {
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