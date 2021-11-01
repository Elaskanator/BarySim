using System;
using System.Collections.Generic;

namespace Generic {
	public static class PrimeFunctions {
		/// <summary>
		/// Returns a list of prime factors of the provided value, which must be nonzero.
		/// The value one has no factors, so an empty collection is returned.
		/// The sign of the value is ignored, so it is the responsibility of the caller to handle negativity.
		/// </summary>
		/// <param name="value">Test value to factorize, which must be nonzero</param>
		/// <returns>A sequence of prime factorizations of the provided whole number</returns>
		public static IEnumerable<PrimeFactor> Factorize(this long value) {
			if (value == 0) throw new ArgumentOutOfRangeException();
			
			long remaining = value > 0 ? value : -value;
			long sqrt = (long)Math.Sqrt(value);//round down, could help performance later comparing it to an integer repeatedly

			PrimeEnumerator.Singleton.Reset();
			while (remaining > 1 && PrimeEnumerator.Singleton.MoveNext()) {
				if (PrimeEnumerator.Singleton.Current > sqrt) {//remainder is prime
					yield return new PrimeFactor(remaining);//if we need to use System.Long for the factor, everything is sure to explode
					break;
				} else {
					int power = 0;
					while (remaining % PrimeEnumerator.Singleton.Current == 0) {
						power++;
						remaining /= PrimeEnumerator.Singleton.Current;
					}
					if (power > 0) {
						yield return new PrimeFactor(PrimeEnumerator.Singleton.Current, power);
					}
				}
			}
		}
		public static IEnumerable<PrimeFactor> Factorize(this int value) {
			return Factorize((long)value);
		}

		public static bool IsPrime(this long value) {
			value = value > 0 ? value : -value;

			if (value == 0) throw new ArgumentOutOfRangeException();
			if (value == 1) throw new ArgumentOutOfRangeException();

			long sqrt = (long)Math.Sqrt(value);//round down, could help performance later comparing it to an integer repeatedly
			
			PrimeEnumerator.Singleton.Reset();
			while (PrimeEnumerator.Singleton.MoveNext() && PrimeEnumerator.Singleton.Current <= sqrt) {
				if (value % PrimeEnumerator.Singleton.Current == 0) {
					return false;
				}
			}

			return true;
		}
		public static bool IsPrime(this int value) {
			return IsPrime((long)value);
		}
		public static bool IsComposite(this long value) {
			return !IsPrime(value);
		}
		public static bool IsComposite(this int value) {
			return !IsPrime((long)value);
		}
	}
}
/*
		private const int MAX_DENOMINATOR = 1 << 30;
		private const int MAX_NUMERATOR = 1 << 30;
		private const double DEFAULT_EPSIOLON = 1d / (1 << 30);

		//junk function only works if reduced fraction's denominator does not factorize to multiple distinct primes (e.g. 1/4 works but 1/10 is 1 / (2 * 5))
		public static Tuple<long, int> Rationalize(this double value, double epsilon = DEFAULT_EPSIOLON) {
			double remainder = value % 1;
			if (remainder == 0d) return new Tuple<long, int>((int)value, 1);
			
			Tuple<long, int> denominator = null;
			
			_primes.Reset();
			_primes.MoveNext();
			double close_high = 1d - epsilon;
			bool cont = true;
			double newRemainder = remainder;
			while (_primes.Current < MAX_DENOMINATOR && cont) {
				newRemainder = remainder;
				for (int i = 1; ; i++) {
					newRemainder *= _primes.Current;
					if (newRemainder % 1 < epsilon || newRemainder % 1 > close_high) {
						denominator = new Tuple<long, int>(_primes.Current, i);
						cont = false;
						break;
					} else if (newRemainder > MAX_NUMERATOR) break;
				}
				if (cont) _primes.MoveNext();
			}

			if (denominator == null) throw new NotImplementedException();//no neat answer
			
			Tuple<long, int>[] numerators;

			int numerator = ((int)(newRemainder + epsilon));
			if (numerator == 0) numerators = new Tuple<long, int>[0];
			else numerators = ((int)(newRemainder + epsilon)).Factorize().ToArray();

			for (int i = 0; i < numerators.Length && numerators[i].Item1 <= denominator.Item1; i++) {
				if (denominator.Item1 == numerators[i].Item1) {
					if (denominator.Item2 > numerators[i].Item2) {
						denominator = new Tuple<long, int>(denominator.Item1, denominator.Item2 - numerators[i].Item2);
						numerators[i] = new Tuple<long, int>(1, 1);
					} else {
						numerators[i] = new Tuple<long, int>(numerators[i].Item1, numerators[i].Item2 - denominator.Item2);
						denominator = new Tuple<long, int>(1, 1);
					}
					break;
				}
			}

			int numeratorsProduct = numerators.Aggregate(1, (xs, d) => xs *= Enumerable.Range(0, d.Item2).Aggregate(1, (fs, f) => fs *= d.Item1));
			int denominatorsProduct = Enumerable.Range(0, denominator.Item2).Aggregate(1, (fs, f) => fs *= denominator.Item1);

			return new Tuple<long, int>(numeratorsProduct + ((int)value * denominatorsProduct), denominatorsProduct);
		}
*/