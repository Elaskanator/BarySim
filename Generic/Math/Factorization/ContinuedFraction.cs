using System;
using System.Collections.Generic;
using System.Linq;

namespace Generic.Classes;

public class ContinuedFraction : Fraction {
	public readonly long[] ContinuedFractionTerms;

	public ContinuedFraction(int sign, long[] terms)
		: base(sign, Rationalization(terms)) {
		this.ContinuedFractionTerms = terms.ToArray();
	}
	public ContinuedFraction(long[] terms) : this(1, terms) { }

	public ContinuedFraction Truncate(int numTerms) {
		return new ContinuedFraction(this.Sign, this.ContinuedFractionTerms.Take(numTerms).ToArray());
	}

	public static IEnumerable<PrimeFactor> Rationalization(long[] terms) {
		if (terms.Length == 0)
			throw new ArgumentException("No terms provided");
		else if (terms.Length == 1)
			return terms[0].Factorize();
			
		long numerator = 1L, denominator = 1L;
		int startPos = terms.Length - 1;

		// ensure the finite continued fraction is unique in the proper form,
		// such that the final term is not 1 (unless it is exactly the value 1)
		if (terms.Length > 1 && terms[startPos] == 1L) {
			terms[startPos - 1] ++;
			startPos --;
		}

		//aggregate from the bottom up
		denominator = terms[startPos];
		for (int i = startPos - 1; i >= 0; i--) {
			if (terms[i] <= 0) throw new ArgumentException("All terms must be strictly positive");
					
			numerator += denominator*terms[i];
					
			if (i > 0)
				(numerator, denominator) = (denominator, numerator);
		}

		return numerator.Factorize().Concat(denominator.Factorize(true));
	}
}

public static class ContinuedFractionExtensions {
	public static long[] SuperPrecisePi = new long[] { 3, 7, 15, 1, 292, 1, 1, 1, 2, 1, 3, 1, 14, 2, 1, 1, 2, 2, 2, 2, 1, 84, 2, 1, 1, 15 };

	public static ContinuedFraction ToContinuedFraction(this double number, int? depthLimit = 10, int? goodThreshold = 20) {
		int sign = Math.Sign(number);
		number = Math.Abs(number);

		// if new term STRICTLY less than good threshold

		throw new NotImplementedException();
	}
}