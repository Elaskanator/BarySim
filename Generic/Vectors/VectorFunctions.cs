﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Generic.Extensions;

namespace Generic.Vectors {
	// TODO - Clifford Algebra
	// http://eusebeia.dyndns.org/4d/vis/10-rot-1#Clifford_Rotations
	// https://www.cis.upenn.edu/~cis610/clifford.pdf
	// https://www.av8n.com/physics/clifford-intro.htm
	// http://scipp.ucsc.edu/~haber/archives/physics251_11/Clifford_Slides.pdf
	public static partial class VectorFunctions {
		public static readonly Vector<int> NegativeOne = -Vector<int>.One;
		public static readonly Vector<int> PowersOfTwo = new Vector<int>(Enumerable.Range(0, Vector<int>.Count).Select(i => 1 << i).ToArray());
		public static readonly Vector<int>[] DimensionFilters = Enumerable.Range(0, Vector<int>.Count + 1).Select(d1 => new Vector<int>(Enumerable.Range(0, Vector<int>.Count).Select(d2 => d2 >= d1 ? 0 : 1).ToArray())).ToArray();
		public static readonly Vector<int>[] DimensionSignals = Enumerable.Range(0, Vector<int>.Count + 1).Select(d1 => new Vector<int>(Enumerable.Range(0, Vector<int>.Count).Select(d2 => d2 >= d1 ? 0 : -1).ToArray())).ToArray();
		public static readonly Vector<int>[] DimensionFiltersInverted = Enumerable.Range(0, Vector<int>.Count + 1).Select(d1 => new Vector<int>(Enumerable.Range(0, Vector<int>.Count).Select(d2 => d2 >= d1 ? 1 : 0).ToArray())).ToArray();
		public static readonly Vector<int>[] DimensionSignalsInverted = Enumerable.Range(0, Vector<int>.Count + 1).Select(d1 => new Vector<int>(Enumerable.Range(0, Vector<int>.Count).Select(d2 => d2 >= d1 ? -1 : 0).ToArray())).ToArray();

		public static bool EqualsAny(this Vector<float> first, Vector<float> second, int? dim = null) =>
			Vector.EqualsAny(
				NegativeOne,
				Vector.ConditionalSelect(
					DimensionSignals[dim ?? Vector<int>.Count],
					Vector.Equals(first, second),
					Vector<int>.Zero));

		public static int BitmaskEqual(this Vector<float> first, Vector<float> second, int? dim = null) =>
			Vector.Dot(
				Vector.ConditionalSelect(
					Vector.Equals(first, second),
					PowersOfTwo,
					Vector<int>.Zero),
				DimensionFilters[dim ?? Vector<float>.Count]);
		public static int BitmaskInequal(this Vector<float> first, Vector<float> second, int? dim = null) =>
			Vector.Dot(
				Vector.ConditionalSelect(
					Vector.Equals(first, second),
					Vector<int>.Zero,
					PowersOfTwo),
				DimensionFilters[dim ?? Vector<float>.Count]);
		
		public static int BitmaskGreaterThan(this Vector<float> first, Vector<float> second, int? dim = null) =>
			Vector.Dot(
				Vector.ConditionalSelect(
					Vector.GreaterThan(first, second),
					PowersOfTwo,
					Vector<int>.Zero),
				DimensionFilters[dim ?? Vector<float>.Count]);
		public static int BitmaskGreaterThanOrEqual(this Vector<float> first, Vector<float> second, int? dim = null) =>
			Vector.Dot(
				Vector.ConditionalSelect(
					Vector.GreaterThanOrEqual(first, second),
					PowersOfTwo,
					Vector<int>.Zero),
				DimensionFilters[dim ?? Vector<float>.Count]);
		
		public static int BitmaskLessThan(this Vector<float> first, Vector<float> second, int? dim = null) =>
			Vector.Dot(
				Vector.ConditionalSelect(
					Vector.LessThan(first, second),
					PowersOfTwo,
					Vector<int>.Zero),
				DimensionFilters[dim ?? Vector<float>.Count]);
		public static int BitmaskLessThanOrEqual(this Vector<float> first, Vector<float> second, int? dim = null) =>
			Vector.Dot(
				Vector.ConditionalSelect(
					Vector.LessThanOrEqual(first, second),
					PowersOfTwo,
					Vector<int>.Zero),
				DimensionFilters[dim ?? Vector<float>.Count]);

		public static readonly Vector<float>[] IdentityMatrixColumns = Enumerable
			.Range(0, Vector<float>.Count)
			.Select(i => new Vector<float>(
				Enumerable
					.Range(0, Vector<float>.Count)
					.Select(j => i == j ? 1f : 0f)
					.ToArray()))
			.ToArray();

		public static Vector<float> New(params float[] components) {
			if (components.Length == Vector<float>.Count) {
				return new Vector<float>(components);
			} else {
				Span<float> values = stackalloc float[Vector<float>.Count];
				values[0] = components.Length > 0 ? components[0] : 0;
				values[1] = components.Length > 1 ? components[1] : 0;
				values[2] = components.Length > 2 ? components[2] : 0;
				values[3] = components.Length > 3 ? components[3] : 0;
				values[4] = components.Length > 4 ? components[4] : 0;
				values[5] = components.Length > 5 ? components[5] : 0;
				values[6] = components.Length > 6 ? components[6] : 0;
				values[7] = components.Length > 7 ? components[7] : 0;
				return new Vector<float>(values);
			}
		}
		public static Vector<float> New(IEnumerable<float> components) => New(components.ToArray());
		public static Vector<float> New(float x, float y) {
			Span<float> values = stackalloc float[Vector<float>.Count];
			values[0] = x;
			values[1] = y;
			return new Vector<float>(values);
		}

		public static float Magnitude(this Vector<float> v) =>  MathF.Sqrt(Vector.Dot(v, v));
			//return MathF.Sqrt(
			//	Enumerable.Range(0, dim)
			//		.Select(d => v[d] * v[d])
			//		.Sum());
		public static float Magnitude(this float[] v) => MathF.Sqrt(v.Sum(x => x * x));

		public static float Distance(this Vector<float> v1, Vector<float> v2) {
			Vector<float> temp = Vector.Subtract(v1, v2);
			return MathF.Sqrt(Vector.Dot(temp, temp));

			//return MathF.Sqrt(
			//	Enumerable
			//		.Range(0, dim)
			//		.Select(i => v2[i] - v1[i])
			//		.Select(x => x * x)
			//		.Sum());
		}

		public static Vector<float> Clamp(this Vector<float> v, float maxMagnitude) {
			float magnitude = v.Magnitude();
			return magnitude > maxMagnitude
				? v * (maxMagnitude / magnitude)
				: v;
		}

		public static float[] Normalize(this float[] v, float length = 1f) {
			float magnitude = v.Magnitude(),
				ratio = length / magnitude;
			return magnitude == length
				? v
				: v.Select(x => x * ratio).ToArray();
		}
		public static Vector<float> Normalize(this Vector<float> v, float length = 1f) =>
			v * (length / MathF.Sqrt(Vector.Dot(Vector.Multiply(v, v), Vector<float>.One)));

			//float magnitude = v.Magnitude(dim),
			//	ratio = length / magnitude;
			//return magnitude == length
			//	? v
			//	: New(Enumerable.Range(1, dim).Select(d => v[d] * ratio));

		public static float AngleTo_FullRange(this Vector<float> v1, Vector<float> v2, int dim) {
			switch (dim) {
				case 0:
					throw new Exception("0D vectors have no angle");
				case 1:
					return MathF.Atan2(0f, v1[0] - v2[0]);
				case 2:
					return MathF.Atan2(v1[1] - v2[1], v1[0] - v2[0]);
				default:
					float angle = MathF.Acos(Vector.Dot(v1, v2) / v1.Magnitude() / v2.Magnitude());
					float[][] sqMatrixCols = new float[][] { Enumerable.Range(0, dim).Select(d => v1[d]).ToArray(), Enumerable.Range(0, dim).Select(d => v2[d]).ToArray() }
						.Concat(Enumerable
							.Range(0, dim - 2)
							.Select(cIdx => Enumerable
								.Range(0, dim)
								.Select(idx => idx == cIdx + 2 ? 1f : 0f)
								.ToArray()))
						.ToArray();
					int orientation = MathF.Sign(sqMatrixCols.Determinant());
					return angle + (orientation < 0 ? MathF.PI : 0f);
			}
		}

		//see https://www.math10.com/en/algebra/matrices/determinant.html
		public static float Determinant(this float[][] squareVectorColumns) {
			if (squareVectorColumns.Length == 1)
				return squareVectorColumns[0][0];
			//else if (squareVectorColumns.Length == 2)
			//	return	squareVector[0][0]*squareVector[1][1]
			//		-	squareVector[0][1]*squareVector[1][0];
			//else if (squareVectorColumns.Length == 3)//Leibniz formula
			//	return	squareVector[0][0]*squareVector[1][1]*squareVector[2][2]
			//		-	squareVector[0][0]*squareVector[1][2]*squareVector[2][1];
			//		-	squareVector[0][1]*squareVector[1][0]*squareVector[2][2]
			//		+	squareVector[0][1]*squareVector[1][2]*squareVector[2][0]
			//		+	squareVector[0][2]*squareVector[1][0]*squareVector[2][1]
			//		-	squareVector[0][2]*squareVector[1][1]*squareVector[2][0]
			else return Enumerable
				.Range(0, squareVectorColumns.Length)
				.Select(i => squareVectorColumns[0][i] * squareVectorColumns.Cofactor(0, i))
				.Sum();
		}

		//https://en.wikipedia.org/wiki/Rodrigues%27_rotation_formula
		//https://en.wikipedia.org/wiki/Rotations_in_4-dimensional_Euclidean_space#Relation_to_quaternions
		//public static double[] Rotate(this double[] v, double[] axis, double angle) {//Rodrigues's rotation formula (3d max)

		//}
		
		public static float Cofactor(this float[][] squareVectorColumns, int colIdx, int rowIdx) {
			return squareVectorColumns
				.Without((col, c) => c == colIdx)
				.Select((col, c) => col
					.Without((x, r) => r == rowIdx)
					.Select((x, r) => c + r % 2 == 0 ? 1f : -1f)
					.ToArray())
				.ToArray()
				.Determinant();
		}

		public static float[] RandomCoordinate_Spherical(this float radius, int dimensionality, Random rand = null) {
			if (dimensionality < 1) throw new ArgumentOutOfRangeException(nameof(dimensionality));
			rand ??= new Random();

			float r = (float)rand.NextDouble();
			switch (dimensionality) {
				case 1:
					return new float[] { radius * r - (radius/2f) };
				case 2:
					r = MathF.Sqrt(radius * radius * r);

					float angle = 2f*MathF.PI * (float)rand.NextDouble();
					return new float[] {
						r * MathF.Cos(angle),
						r * MathF.Sin(angle)
					};
				case 3:
					r = MathF.Cbrt(radius * radius * radius * r);

					float u = (float)rand.NextDouble();
					float v = (float)rand.NextDouble();
					float theta = 2f*MathF.PI * u;
					float phi = MathF.Acos(2f*v - 1f);

					return new float[] {
						r * MathF.Sin(phi) * MathF.Cos(theta),
						r * MathF.Sin(phi) * MathF.Sin(theta),
						r * MathF.Cos(phi)
					};
				default://rejection sampling
					float[] coords;
					do {
						coords = Enumerable
							.Range(0, dimensionality)
							.Select(i => 2f*(0.5f - (float)rand.NextDouble()) * radius)
							.ToArray();
					} while (coords.Magnitude() > radius);
					return coords;
			}
		}

		public static float[] RandomUnitVector_Spherical(this int dimensionality, Random rand = null) {
			if (dimensionality < 1) throw new ArgumentOutOfRangeException(nameof(dimensionality));
			rand ??= new Random();

			switch (dimensionality) {
				case 1:
					if (rand.NextDouble() < 0.5)
						return new float[] { 1 };
					else return new float[] { -1 };
				case 2:
					float angle = 2f * MathF.PI * (float)rand.NextDouble();
					return new float[] {
						MathF.Cos(angle),
						MathF.Sin(angle)
					};
				case 3:
					float theta = MathF.Acos(-1f + 2f*(float)rand.NextDouble());
					float phi = 2f * MathF.PI * (float)rand.NextDouble();
					return new float[] {
						MathF.Sin(theta) * MathF.Cos(phi),
						MathF.Sin(theta) * MathF.Sin(phi),
						MathF.Cos(theta)
					};
				default://rejection sampling
					float[] coords;
					float magnitude;
					do {
						coords = Enumerable
							.Range(0, dimensionality)
							.Select(i => 2f*(0.5f - (float)rand.NextDouble()))
							.ToArray();
						magnitude = coords.Magnitude();
					} while (0.0000001f >= magnitude || magnitude > 1f);
					return coords.Select(x => x / magnitude).ToArray();
			}
		}

		/// <summary>
		/// Normalizes a vector to have a Euclidean length of 1
		/// </summary>
		/// <param name="v">The vector to normalize</param>
		public static double[] Normalize(this double[] v, double len = 1d) {
			double magnitude = Magnitude(v);
			return magnitude == len
				? v
				: v.Multiply(len / magnitude);
		}

		public static double Magnitude(this double[] v) {
			return Math.Sqrt(v.Sum(x => x * x));
		}

		public static double[] Multiply(this double[] v, double scalar) {
			return v.Select(n => n * scalar).ToArray();
		}

		//see https://en.wikipedia.org/wiki/Volume_of_an_n-ball#Low_dimensions
		public static double HypersphereVolume(double radius, int dimensionality) {
			if (dimensionality < 1) throw new ArgumentOutOfRangeException(nameof(dimensionality));

			switch (dimensionality) {
				case 1:
					return radius;
				case 2:
					return Math.PI * (radius*radius);
				case 3:
					return (4d/3d) * Math.PI * (radius*radius*radius);
				case 4:
					return Math.PI * Math.PI * radius*radius*radius*radius / 2d;
				default:
					throw new NotImplementedException();
			}
		}

		public static double HypersphereRadius(this double volume, int dimensionality) {
			if (dimensionality < 1) throw new ArgumentOutOfRangeException(nameof(dimensionality));

			switch (dimensionality) {
				case 1:
					return volume;
				case 2:
					return Math.Sqrt(volume / Math.PI);
				case 3:
					return Math.Cbrt(volume * 3d/4d / Math.PI);
				case 4:
					return Math.Pow(2d * volume, 0.25d) / Math.Sqrt(Math.PI);
				default:
					throw new NotImplementedException();
			}
		}

		//public static double[] Project(this double[] vector1, double[] vector2) {
		//	return vector2.Multiply(vector2.DotProduct(vector1) / vector2.DotProduct(vector2));
		//}

		////see https://www.geometrictools.com/Documentation/OrthonormalSets.pdf
		//public static double[][] Orthonormalize(this double[][] vectors) {//Gram-Schmidt
		//	double[][] result = new double[vectors.Length][];
		//	for (int i = 0; i < vectors.Length; i++) {
		//		result[i] = vectors[i];
		//		for (int j = 0; j < i; j++)
		//			result[i] = result[i].Subtract(vectors[j].Project(result[i]));
		//		result[i] = result[i].Normalize();
		//	}
		//	return result;
		//}
		//public static double[] NewNormalVector(this double[][] orthonormalization) {
		//	return Enumerable
		//		.Range(0, orthonormalization.Length + 1)
		//		.Select(i => Enumerable.Range(0, orthonormalization.Length + 1).Select(n => n == 1 ? 0d : 1d).ToArray())
		//		.Select((e, i) =>
		//			e.Multiply(
		//				(i % 2 == 0 ? 1d : -1d)
		//				* orthonormalization.Without((x, j) => i == j).ToArray().Determinant()))
		//		.Aggregate(new double[orthonormalization.Length + 1], (x, agg) => agg.Add(x));
		//}
	}
}

	//public static class VectorFunctions {
		//public static double[] Negate(this double[] v) {
		//	return v.Select(n => -n).ToArray();
		//}

		//public static double[] Add(this double[] v1, double[] v2) {
		//	return v1.Select((n, i) => n + v2[i]).ToArray();
		//}

		//public static double[] Subtract(this double[] v1, double[]v2) {
		//	return v1.Select((n, i) => n - v2[i]).ToArray();
		//}

		//public static double[] Divide(this double[] v, double scalar) {
		//	return v.Select(n => n / scalar).ToArray();
		//}

		//public static double DotProduct(this double[] v1, double[] v2) {
		//	return v1.Zip(v2, (x1, x2) => x1 * x2).Sum();
		//}

		//public static double AngleTo(this double[] v1, double[] v2) {
		//	double
		//		dot = DotProduct(v1, v2),
		//		len1 = Magnitude(v1),
		//		len2 = Magnitude(v2);
		//	if (len1 == 0d || len2 == 0d)
		//		return 0d;
		//	else return Math.Acos(dot / len1 / len2);
		//}
	//}