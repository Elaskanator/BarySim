using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Generic.Models {
	/// <summary>
	/// Represents a rational number
	/// </summary>
	public class Fraction : IEnumerable<PrimeFactor>, IEquatable<IEnumerable<PrimeFactor>>, IEquatable<Fraction>, IComparer<Fraction>, IEqualityComparer<Fraction>, IComparable<Fraction>, IComparable<long>, IComparable<double> {
		public static readonly Fraction Zero = new Fraction(0);
		public static readonly Fraction One = new Fraction(1);

		/// <summary>
		/// Whether the value is positive, negative, or zero
		/// </summary>
		public readonly int Sign;
		/// <summary>
		/// The complete factorization of the fraction
		/// </summary>
		public readonly PrimeFactor[] CompoundFactorization;

		public IEnumerable<PrimeFactor> Numerator { get { return this.CompoundFactorization.Where(t => t.Exponent > 0); } }
		public IEnumerable<PrimeFactor> NumeratorFactorization { get { return this.Numerator; } }
		public long NumeratorValue { get { return this.Numerator.DefactorizePositiveExponents(); } }
		public IEnumerable<PrimeFactor> Denominator { get { return this.CompoundFactorization.Where(t => t.Exponent < 0); } }
		public IEnumerable<PrimeFactor> DenominatorFactorization { get { return this.Denominator.Select(f => f.Reciprocol); } }
		public long DenominatorValue { get { return this.Denominator.DefactorizePositiveExponents(); } }

		/// <summary>
		/// Returns the numeric representation of the fraction
		/// </summary>
		public double AsNumber { get { return this.Sign * (double)this.NumeratorValue / this.DenominatorValue; } }
		public bool IsIntegral { get { return !this.Denominator.Any(); } }

		public Fraction(int sign, IEnumerable<PrimeFactor> compoundFactorization) {
			this.Sign = sign;
			this.CompoundFactorization = (compoundFactorization ?? new PrimeFactor[0]).Simplify().ToArray();
		}
		public Fraction(int sign, IEnumerable<Fraction> fractions) : this(sign, fractions.SelectMany(f => f)) { }
		public Fraction(IEnumerable<Fraction> fractions) : this(1, fractions.SelectMany(f => f)) { }
		public Fraction(IEnumerable<PrimeFactor> compoundFactorization) : this(1, compoundFactorization) { }
		public Fraction(long numerator = 1, long denominator = 1)
			: this(numerator == 0 ? 0 : numerator < 0 ^ denominator < 0 ? -1 : 1,
				  numerator == 0 ? new PrimeFactor[0] : numerator.Factorize().Concat(denominator.Factorize().Select(f => f.Reciprocol))) {
			if (denominator == 0) throw new DivideByZeroException("denominator cannot be zero");
		}

		public override string ToString() {
			if (this.IsIntegral) return this.AsNumber.ToString();
			else return string.Format("{0}{1}/{2}",
				this.Sign == -1 ? "-" : "",
				this.NumeratorValue,
				this.DenominatorValue);
		}
		public string ToFactorString(string formatString = null) {
			if (!this.CompoundFactorization.Any()) return this.AsNumber.ToString();
			else return string.Format("{0} = {1}{2}{3}",
				this.AsNumber.ToString(formatString),
				this.Sign == -1 ? "-" : "",
				this.NumeratorFactorization.ToFactorString(),
				this.IsIntegral ? "" : " / " + this.DenominatorFactorization.ToFactorString());
		}
		public string ToFractionString(bool combineFactors = true) {
			return string.Format("{0}{1}{2}",
				this.Sign == -1 ? "-" : "",
				combineFactors ? this.NumeratorValue.ToString() : this.NumeratorFactorization.ToFactorString(),
				this.IsIntegral ? "" : " / " + (combineFactors ? this.DenominatorValue.ToString() : this.DenominatorFactorization.ToFactorString()));
		}

		public Fraction Reciprocal() {
			return new Fraction(this.Sign, this.CompoundFactorization.Select(f => f.Reciprocol));
		}

		public Fraction Negate() {
			return new Fraction(-this.Sign, this.CompoundFactorization);
		}

		public Fraction Add(Fraction other) {
			return this.AddOrSubtract(other.Sign, other.CompoundFactorization);
		}
		public Fraction Add(IEnumerable<Fraction> others) {
			return others.Aggregate(this, (acc, x) => acc += x);
		}
		public Fraction Add(params Fraction[] others) {
			return this.Add(others.AsEnumerable());
		}
		public Fraction Add(params PrimeFactor[] other) {
			return this.AddOrSubtract(1, other);
		}
		public Fraction Add(IEnumerable<PrimeFactor> others) {
			return this.AddOrSubtract(1, others);
		}
		public Fraction Add(long other) {
			if (other == 0) return this;
			else return this.AddOrSubtract(Math.Sign(other), other.Factorize().ToArray());
		}

		public Fraction Subtract(Fraction other) {
			return this.AddOrSubtract(-1 * other.Sign, other.CompoundFactorization);
		}
		public Fraction Subtract(params PrimeFactor[] others) {
			return this.AddOrSubtract(-1, others);
		}
		public Fraction Subtract(IEnumerable<Fraction> others) {
			return others.Aggregate(this, (acc, x) => acc -= x);
		}
		public Fraction Subtract(params Fraction[] others) {
			return this.Add(others.AsEnumerable());
		}
		public Fraction Subtract(IEnumerable<PrimeFactor> others) {
			return this.AddOrSubtract(-1, others);
		}
		public Fraction Subtract(long other) {
			if (other == 0) return this;
			else return this.AddOrSubtract(-1 * Math.Sign(other), other.Factorize().ToArray());
		}

		public Fraction Multiply(Fraction other) {
			return this.Multiply(other.Sign, other.CompoundFactorization);
		}
		public Fraction Multiply(params PrimeFactor[] others) {
			return this.Multiply(1, others);
		}
		public Fraction Multiply(IEnumerable<PrimeFactor> others) {
			return this.Multiply(1, others);
		}
		public Fraction Multiply(long other) {
			if (other == 0) return Fraction.Zero;
			else return this.Multiply(Math.Sign(other), other.Factorize().ToArray());
		}

		public Fraction Divide(Fraction other) {
			return this.Divide(other.Sign, other.CompoundFactorization);
		}
		public Fraction Divide(params PrimeFactor[] others) {
			return this.Divide(1, others);
		}
		public Fraction Divide(IEnumerable<PrimeFactor> others) {
			return this.Divide(1, others);
		}
		public Fraction Divide(long other) {
			if (other == 0) throw new DivideByZeroException();
			else return this.Divide(Math.Sign(other), other.Factorize().ToArray());
		}

		private Fraction AddOrSubtract(int sign, IEnumerable<PrimeFactor> otherFactors) {
			if (sign == 0) return this;
			//there is no known way to compute addition or subtraction of two rational numbers that avoids factorizing at some point
			//refer to the ABC conjecture
			long numerator =
				(this.Sign * this.Numerator.Concat(otherFactors.Where(f => f.Exponent < 0)).DefactorizePositiveExponents())
				+ (sign * otherFactors.Where(f => f.Exponent > 0).Concat(this.DenominatorFactorization).DefactorizePositiveExponents());
			return numerator == 0
				? Fraction.Zero
				: new Fraction(
					Math.Sign(numerator),
					PrimeFunctions.Factorize(numerator).Concat(this.Denominator).Concat(otherFactors.Where(f => f.Exponent < 0)));
		}
		private Fraction Multiply(int otherSign, IEnumerable<PrimeFactor> otherFactors) {
			if (this.Sign == 0 || otherSign == 0)
				return Fraction.Zero;
			else if (otherFactors.Any())
				return new Fraction(
					this.Sign * otherSign,
					this.CompoundFactorization.Concat(otherFactors));
			else
				return otherSign == 1 ? this : this.Negate();
		}
		private Fraction Divide(int otherSign, IEnumerable<PrimeFactor> otherFactors) {
			if (otherSign == 0) throw new DivideByZeroException();
			else return this.Multiply(otherSign, otherFactors.Select(f => f.Reciprocol).ToArray());
		}
		
		public bool Equals(Fraction other) {
			return other != null && this.CompoundFactorization.SequenceEqual(other.CompoundFactorization);
		}
		public override bool Equals(object obj) {
			return (obj != null) && (obj is Fraction) && this.Equals(obj as Fraction);
		}
		public bool Equals(int value) {
			return this.AsNumber == value;
		}
		public bool Equals(double value) {
			return this.AsNumber == value;
		}
		public bool Equals(IEnumerable<PrimeFactor> other) {
			return other != null && this.CompoundFactorization.SequenceEqual(other.Simplify());
		}
		public bool Equals(Fraction x, Fraction y) {
			return !(x is null ^ y is null) && x.Equals(y);
		}

		public int CompareTo(Fraction other) {//TODO exact version based on factors
			return this.AsNumber.CompareTo(other.AsNumber);
		}
		public int CompareTo(long other) {
			return this.AsNumber.CompareTo(other);
		}
		public int CompareTo(double other) {
			return this.AsNumber.CompareTo(other);
		}
		public int Compare(Fraction x, Fraction y) {
			return x.CompareTo(y);
		}

		public IEnumerator<PrimeFactor> GetEnumerator() {
			return this.CompoundFactorization.AsEnumerable().GetEnumerator();
		}
		IEnumerator IEnumerable.GetEnumerator() { return this.GetEnumerator(); }

		public override int GetHashCode() {
			return this.AsNumber.GetHashCode();
		}
		public int GetHashCode(Fraction obj) {
			return obj.GetHashCode();
		}

		#region OperatorExtensions
		//these two probably make other operator extensions redundant
		public static implicit operator double (Fraction f) {
			return f.AsNumber;
		}
		public static explicit operator Fraction (long A) {
			return new Fraction(A);
		}

		//fractions are implicitly collections of factors
		public static implicit operator Fraction (PrimeFactor[] factors) {
			return new Fraction(factors);
		}
		public static implicit operator Fraction (Fraction[] fractions) {
			return new Fraction(fractions);
		}

		public static bool operator ==(Fraction A, Fraction B) {
			return !(A is null ^ B is null) && A.Equals(B);
		}
		public static bool operator ==(Fraction A, long B) {
			return !(A is null) && A.Equals(B);
		}
		public static bool operator ==(Fraction A, double B) {
			return !(A is null) && A.Equals(B);
		}
		public static bool operator ==(long A, Fraction B) {
			return !(B is null) && B.Equals(A);
		}
		public static bool operator ==(double A, Fraction B) {
			return !(B is null) && B.Equals(A);
		}
		public static bool operator !=(Fraction A, Fraction B) {
			return (A is null ^ B is null) && !A.Equals(B);
		}
		public static bool operator !=(Fraction A, long B) {
			return (A is null) || !A.Equals(B);
		}
		public static bool operator !=(Fraction A, double B) {
			return (A is null) || !A.Equals(B);
		}
		public static bool operator !=(long A, Fraction B) {
			return (B is null) ||!B.Equals(A);
		}
		public static bool operator !=(double A, Fraction B) {
			return (B is null) || !B.Equals(A);
		}

		public static bool operator >(Fraction A, Fraction B) {
			return A.CompareTo(B) > 0;
		}
		public static bool operator >(Fraction A, long B) {
			return A.CompareTo(B) > 0;
		}
		public static bool operator >(Fraction A, double B) {
			return A.CompareTo(B) > 0;
		}
		public static bool operator >(long A, Fraction B) {
			return B.CompareTo(A) < 0;
		}
		public static bool operator >(double A, Fraction B) {
			return B.CompareTo(A) < 0;
		}

		public static bool operator >=(Fraction A, Fraction B) {
			return A.CompareTo(B) >= 0;
		}
		public static bool operator >=(Fraction A, long B) {
			return A.CompareTo(B) >= 0;
		}
		public static bool operator >=(Fraction A, double B) {
			return A.CompareTo(B) >= 0;
		}
		public static bool operator >=(long A, Fraction B) {
			return B.CompareTo(A) <= 0;
		}
		public static bool operator >=(double A, Fraction B) {
			return B.CompareTo(A) <= 0;
		}

		public static bool operator <(Fraction A, Fraction B) {
			return A.CompareTo(B) < 0;
		}
		public static bool operator <(Fraction A, long B) {
			return A.CompareTo(B) < 0;
		}
		public static bool operator <(Fraction A, double B) {
			return A.CompareTo(B) < 0;
		}
		public static bool operator <(long A, Fraction B) {
			return B.CompareTo(A) > 0;
		}
		public static bool operator <(double A, Fraction B) {
			return B.CompareTo(A) > 0;
		}

		public static bool operator <=(Fraction A, Fraction B) {
			return A.CompareTo(B) <= 0;
		}
		public static bool operator <=(Fraction A, long B) {
			return A.CompareTo(B) <= 0;
		}
		public static bool operator <=(Fraction A, double B) {
			return A.CompareTo(B) <= 0;
		}
		public static bool operator <=(long A, Fraction B) {
			return B.CompareTo(A) >= 0;
		}
		public static bool operator <=(double A, Fraction B) {
			return B.CompareTo(A) >= 0;
		}

		public static Fraction operator + (Fraction A, Fraction B) {
			return A.Add(B);
		}
		public static Fraction operator + (Fraction A, long B) {
			return A.Add(B);
		}
		public static Fraction operator + (long A, Fraction B) {
			return B.Add(A);
		}

		public static Fraction operator -(Fraction A) {
			return A.Negate();
		}

		public static Fraction operator - (Fraction A, Fraction B) {
			return A.Subtract(B);
		}
		public static Fraction operator - (Fraction A, long B) {
			return A.Subtract(B);
		}
		public static Fraction operator - (long A, Fraction B) {
			return B.Subtract(A);
		}

		public static Fraction operator * (Fraction A, Fraction B) {
			return A.Multiply(B);
		}
		public static Fraction operator * (Fraction A, long B) {
			return A.Multiply(B);
		}
		public static Fraction operator * (long A, Fraction B) {
			return B.Multiply(A);
		}

		public static Fraction operator / (Fraction A, Fraction B) {
			return A.Divide(B);
		}
		public static Fraction operator / (Fraction A, long B) {
			return A.Divide(B);
		}
		public static Fraction operator / (long A, Fraction B) {
			return B.Divide(A);
		}
		#endregion OperatorOverloads
	}

	public static class FractionExtensions {
		public static Fraction Sum(this IEnumerable<Fraction> source) {
			return source.Aggregate(Fraction.Zero, (acc, x) => acc += x);
		}
		public static Fraction Product(this IEnumerable<Fraction> source) {
			return source.Aggregate(Fraction.One, (acc, x) => acc *= x);
		}
	}
}