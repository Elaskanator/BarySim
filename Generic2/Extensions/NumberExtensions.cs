﻿using System;

namespace Generic {
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

		public static double[] Random_Spherical(double radius, int dimensionality, Random rand = null) {
			if (dimensionality < 1) throw new ArgumentOutOfRangeException("dimensionality");

			rand ??= new Random();

			double r = rand.NextDouble();
			switch (dimensionality) {
				case 1:
					return new double[] { radius * r };
				case 2:
					r = Math.Sqrt(radius * r);

					double angle = rand.NextDouble() * 2d * Math.PI;
					return new double[] {
						r * Math.Cos(angle),
						r * Math.Sin(angle)
					};
				case 3:
					r = Math.Cbrt(radius * r);

					double u = rand.NextDouble();
					double v = rand.NextDouble();
					double theta = u * 2d * Math.PI;
					double phi = Math.Acos(2d*v - 1d);

					return new double[] {
						r * Math.Sin(phi) * Math.Cos(theta),
						r * Math.Sin(phi) * Math.Sin(theta),
						r * Math.Cos(phi)
					};
				default:
					throw new NotImplementedException("Random sampling within a hyperspherical coordinates");
			}
		}

		public static double HypersphereVolume(double radius, int dimensionality) {
			switch (dimensionality) {
				case 0:
					return 0;
				case 1:
					return radius;
				case 2:
					return Math.PI * (radius * radius);
				case 3:
					return (4d/3d) * Math.PI * (radius * radius * radius);
				default:
					throw new NotImplementedException();
			}
		}

		public static double HypersphereRadius(double volume, int dimensionality) {
			switch (dimensionality) {
				case 0:
					return 0;
				case 1:
					return volume;
				case 2:
					return Math.Sqrt(volume / Math.PI);;
				case 3:
					return Math.Cbrt(volume * 3d / 4d / Math.PI);
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
			if (precision < 1) throw new ArgumentOutOfRangeException("precision", precision, "Must be strictly positive");
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