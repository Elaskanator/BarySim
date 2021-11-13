using System;

namespace Generic.Extensions {
	public static class NumberExtensions {
		public static double ModuloAbsolute(this int value, int mod) {
			value = value % mod;
			return value < 0 ? value + mod : value;
		}
		public static double ModuloAbsolute(this double value, int mod) {
			value = value % mod;
			return value < 0 ? value + mod : value;
		}
		public static double ModuloAbsolute(this double value, double mod) {
			value = value % mod;
			return value < 0 ? value + mod : value;
		}

		public static double BaseExponent(this double value, double numberBase = 10) {
			if (value == 0) throw new ArgumentOutOfRangeException();
			if (value < 0) value *= -1;
			return Math.Log(value, numberBase);
		}

		public static double RoundDown_Log(this double value, double numberBase = 10) {
			if (value == 0) return 0;

			int exp = (int)Math.Floor(value.BaseExponent(numberBase));
			return Math.Pow(numberBase, exp);
		}
		public static double RoundUp_Log(this double value, double numberBase = 10) {
			if (value == 0) return 0;

			int exp = (int)Math.Ceiling(value.BaseExponent(numberBase));
			return Math.Pow(numberBase, exp);
		}

		public static double[] RandomCoordinate_Spherical(double radius, int dimensionality, Random rand = null) {
			if (dimensionality < 1) throw new ArgumentOutOfRangeException("dimensionality");
			rand ??= new Random();

			double r = rand.NextDouble();
			switch (dimensionality) {
				case 1:
					return new double[] { radius * r };
				case 2:
					r = Math.Sqrt(radius * radius * r);

					double angle = 2d*Math.PI * rand.NextDouble();
					return new double[] {
						r * Math.Cos(angle),
						r * Math.Sin(angle)
					};
				case 3:
					r = Math.Cbrt(radius * r);

					double u = rand.NextDouble();
					double v = rand.NextDouble();
					double theta = 2d*Math.PI * u;
					double phi = Math.Acos(2d*v - 1d);

					return new double[] {
						r * Math.Sin(phi) * Math.Cos(theta),
						r * Math.Sin(phi) * Math.Sin(theta),
						r * Math.Cos(phi)
					};
				default:
					throw new NotImplementedException("4D+");
			}
		}

		public static double[] RandomUnitVector_Spherical(int dimensionality, Random rand = null) {
			if (dimensionality < 1) throw new ArgumentOutOfRangeException("dimensionality");
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
					throw new NotImplementedException("4d+");
			}
		}

		public static double HypersphereVolume(double radius, int dimensionality) {
			switch (dimensionality) {
				case 0:
					return 0d;
				case 1:
					return radius;
				case 2:
					return Math.PI * (radius*radius);
				case 3:
					return (4d/3d) * Math.PI * (radius*radius*radius);
				default:
					throw new NotImplementedException();
			}
		}

		public static double HypersphereRadius(double volume, int dimensionality) {
			switch (dimensionality) {
				case 0:
					return 0d;
				case 1:
					return volume;
				case 2:
					return Math.Sqrt(volume / Math.PI);
				case 3:
					return Math.Cbrt(volume * 3d/4d / Math.PI);
				default:
					throw new NotImplementedException();
			}
		}

		public static int BaseExponent(this long value, int numberBase = 10) {
			return (int)BaseExponent((double)value, numberBase);
		}
		public static int BaseExponent(this int value, int numberBase = 10) {
			return (int)BaseExponent((double)value, numberBase);
		}

		private static string DecimalFmtString(int precision) {
			if (precision < 1) throw new ArgumentOutOfRangeException(nameof(precision), precision, "Must be strictly positive");
			return "{0:0." + new string('0', precision - 1) + "E+0}";
		}
		public static string ToStringScientificNotation(this decimal value, int precision = 3) {
			return string.Format(DecimalFmtString(precision), value);
		}
		public static string ToStringScientificNotation(this double value, int precision = 3) {
			return string.Format(DecimalFmtString(precision), value);
		}
	}
}