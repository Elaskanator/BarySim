using System;
using System.Collections.Generic;
using System.Linq;

namespace Generic.Classes {
	/// <summary>
	/// Represents a proper factor of a whole number, without consideration of sign (positive or negative)
	/// </summary>
	public struct PrimeFactor : IEquatable<PrimeFactor>, IEqualityComparer<PrimeFactor> {
		/// <summary>
		/// The prime factor
		/// </summary>
		public readonly long Prime;
		/// <summary>
		/// The exponent of this Factor
		/// </summary>
		/// <remarks>System.Long is a VERY bad idea to /need/ for hopefully obvious reasons</remarks>
		public readonly int Exponent;
		/// <summary>
		/// The absolute value the exponent of this Factor
		/// </summary>
		public readonly int ExponentAbs;

		/// <summary>
		/// The number representation of this Factor
		/// </summary>
		public double AsNumber {
			get {
				long prime = this.Prime;
				if (this.Exponent > 0) return Enumerable.Range(0, this.ExponentAbs).Aggregate(1d, (acc, e) => acc *= prime);
				else return Enumerable.Range(0, this.ExponentAbs).Aggregate(1d, (acc, e) => acc /= prime);
			}
		}
		/// <summary>
		/// The number representation of this factor if its exponent were positive
		/// </summary>
		public long AsNumberPositiveExponent {
			get {
				long prime = this.Prime;//cannot reference directly in enumerations
				return Enumerable.Range(0, this.ExponentAbs).Aggregate(1L, (acc, e) => acc *= prime);
			}
		}
		/// <summary>
		/// Returns a new Factor with the opposite exponent
		/// </summary>
		public PrimeFactor Reciprocol { get { return new PrimeFactor(this.Prime, -this.Exponent); } }

		/// <summary>
		/// Initializes a new instance of a prime factor.
		/// </summary>
		/// <param name="prime">The prime number of the factor</param>
		/// <param name="exponent">The exponent of the prime number of the factor</param>
		internal PrimeFactor(long prime, int exponent = 1) {
			if (prime < 2) throw new ArgumentOutOfRangeException("prime");
			else if (exponent == 0) throw new ArgumentOutOfRangeException("exponent");

			this.Prime = prime;
			this.Exponent = exponent;
			this.ExponentAbs = exponent * (exponent > 0 ? 1 : -1);
		}

		public override string ToString() {
			return this.AsNumber.ToString();
		}
		public override bool Equals(object obj) {
			return obj is PrimeFactor && this.Equals((PrimeFactor)obj);
		}

		/// <summary>
		/// Returns a Factor with the same prime factor as this Factor and the specified exponent
		/// </summary>
		/// <param name="value">The exponent for the resulting Factor</param>
		/// <returns>A Factor instance with the same prime factor as this Factor and the specified exponent</returns>
		public PrimeFactor ChangeExponent(int value) {
			if (value == 0) throw new ArgumentOutOfRangeException("value", "Cannot use an exponent of zero, because the number one is not a proper factor");
			else if (this.Exponent == value) return this;
			else return new PrimeFactor(this.Prime, value);
		}

		public bool Equals(PrimeFactor other) {
			return this.Prime == other.Prime && this.Exponent == other.Exponent;
		}
		public bool Equals(PrimeFactor x, PrimeFactor y) {
			return x.Equals(y);
		}

		public override int GetHashCode() {
			return this.Prime.GetHashCode() + this.Exponent.GetHashCode();
		}
		public int GetHashCode(PrimeFactor obj) {
			return obj.GetHashCode();
		}

		public static implicit operator double (PrimeFactor f) {
			return f.AsNumber;
		}
	}
}