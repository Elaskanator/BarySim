using System;
using System.Linq;
using Generic.Extensions;

namespace Generic.Vectors {
	// Clifford Algebra
	// http://eusebeia.dyndns.org/4d/vis/10-rot-1#Clifford_Rotations
	// https://www.cis.upenn.edu/~cis610/clifford.pdf
	// https://www.av8n.com/physics/clifford-intro.htm
	// http://scipp.ucsc.edu/~haber/archives/physics251_11/Clifford_Slides.pdf
	public static class HyperspaceFunctions {
		public static double[] RandomCoordinate_Spherical(this double radius, int dimensionality, Random rand = null) {
			if (dimensionality < 1) throw new ArgumentOutOfRangeException(nameof(dimensionality));
			rand ??= new Random();

			double r = rand.NextDouble();
			switch (dimensionality) {
				case 1:
					return new double[] { radius * r - (radius/2d) };
				case 2:
					r = Math.Sqrt(radius * radius * r);

					double angle = 2d*Math.PI * rand.NextDouble();
					return new double[] {
						r * Math.Cos(angle),
						r * Math.Sin(angle)
					};
				case 3:
					r = Math.Cbrt(radius * radius * radius * r);

					double u = rand.NextDouble();
					double v = rand.NextDouble();
					double theta = 2d*Math.PI * u;
					double phi = Math.Acos(2d*v - 1d);

					return new double[] {
						r * Math.Sin(phi) * Math.Cos(theta),
						r * Math.Sin(phi) * Math.Sin(theta),
						r * Math.Cos(phi)
					};
				default://rejection sampling
					double[] coords;
					while ((coords = Enumerable.Range(0, dimensionality).Select(i => 2d*(0.5d - rand.NextDouble()) * radius).ToArray()).Magnitude() > radius) { }
					return coords;
			}
		}

		public static double[] RandomUnitVector_Spherical(this int dimensionality, Random rand = null) {
			if (dimensionality < 1) throw new ArgumentOutOfRangeException(nameof(dimensionality));
			rand ??= new Random();

			switch (dimensionality) {
				case 1:
					if (rand.NextDouble() < 0.5)
						return new double[] { 1 };
					else return new double[] { -1 };
				case 2:
					double angle = 2d * Math.PI * rand.NextDouble();
					return new double[] {
						Math.Cos(angle),
						Math.Sin(angle)
					};
				case 3:
					double theta = 2d * Math.PI * rand.NextDouble();
					double phi = Math.PI * rand.NextDouble();
					return new double[] {
						Math.Cos(theta) * Math.Sin(phi),
						Math.Sin(theta) * Math.Sin(phi),
						Math.Cos(phi)
					};
				default:
					throw new NotImplementedException();
					//return Enumerable.Range(0, dimensionality).Select(i => rand.NextDouble()).ToArray().Normalize();//TODODODO
			}
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

		public static double[] Project(this double[] vector1, double[] vector2) {
			return vector2.Multiply(vector2.DotProduct(vector1) / vector2.DotProduct(vector2));
		}

		//see https://www.geometrictools.com/Documentation/OrthonormalSets.pdf
		public static double[][] Orthonormalize(this double[][] vectors) {//Gram-Schmidt
			double[][] result = new double[vectors.Length][];
			for (int i = 0; i < vectors.Length; i++) {
				result[i] = vectors[i];
				for (int j = 0; j < i; j++)
					result[i] = result[i].Subtract(vectors[j].Project(result[i]));
				result[i] = result[i].Normalize();
			}
			return result;
		}
		public static double[] NewNormalVector(this double[][] orthonormalization) {
			return Enumerable
				.Range(0, orthonormalization.Length + 1)
				.Select(i => Enumerable.Range(0, orthonormalization.Length + 1).Select(n => n == 1 ? 0d : 1d).ToArray())
				.Select((e, i) =>
					e.Multiply(
						(i % 2 == 0 ? 1d : -1d)
						* orthonormalization.Without((x, j) => i == j).ToArray().Determinant()))
				.Aggregate(new double[orthonormalization.Length + 1], (x, agg) => agg.Add(x));
		}

		//https://en.wikipedia.org/wiki/Rodrigues%27_rotation_formula
		//https://en.wikipedia.org/wiki/Rotations_in_4-dimensional_Euclidean_space#Relation_to_quaternions
		//public static double[] Rotate(this double[] v, double[] axis, double angle) {//Rodrigues's rotation formula (3d max)

		//}

		//see https://www.math10.com/en/algebra/matrices/determinant.html
		public static double Determinant(this double[][] squareVectorColumns) {
			if (squareVectorColumns.Length == 1)
				return squareVectorColumns[0][0];
			//else if (size == 2)
			//	return	squareVector[0][0]*squareVector[1][1]
			//		-	squareVector[0][1]*squareVector[1][0];
			//else if (size == 3)//Leibniz formula
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
		
		public static double Cofactor(this double[][] squareVectorColumns, int colIdx, int rowIdx) {
			return squareVectorColumns
				.Without((col, c) => c == colIdx)
				.Select((col, c) => col
					.Without((x, r) => r == rowIdx)
					.Select((x, r) => c + r % 2 == 0 ? 1d : -1d)
					.ToArray())
				.ToArray()
				.Determinant();
		}
	}
}