using System;

namespace Generic.Extensions {
	public static class NumberExtensions {
		public const double GOLDEN_RATIO = 1.61803398874989484820458683436d;

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

		public static int BaseExponent(this long value, int numberBase = 10) {
			return (int)BaseExponent((double)value, numberBase);
		}
		public static int BaseExponent(this int value, int numberBase = 10) {
			return (int)BaseExponent((double)value, numberBase);
		}
	}
}