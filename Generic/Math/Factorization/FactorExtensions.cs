using System.Collections.Generic;
using System.Linq;

using Generic.Extensions;

namespace Generic.Models {
	public static class FactorExtensions {
		/// <summary>
		/// Returns the numeric representation of the provided prime factors representation the factorization of a fraction
		/// </summary>
		/// <param name="factors">A prime factor representation of the factorization of a fraction</param>
		/// <returns>A numeric representation of the provided prime factors representation the factorization of a fraction</returns>
		public static double Defactorize(this IEnumerable<PrimeFactor> factors) {
			return factors.Aggregate(1d, (acc, f) => acc *= f.AsNumber);
		}
		/// <summary>
		/// Returns the numeric representation of a collection of prime factors that is treated as either the factorization of a numerator or denominator of a fraction, treating all exponents as positive
		/// </summary>
		/// <param name="factors">A prime factor representation of the factorization of a fraction</param>
		/// <returns>A numeric representation of the provided prime factors representation the factorization of a fraction, treating all exponents as positive</returns>
		internal static long DefactorizePositiveExponents(this IEnumerable<PrimeFactor> factors) {
			return factors.Aggregate(1L, (acc, f) => acc *= f.AsNumberPositiveExponent);
		}

		/// <remarks>
		/// Unicode warning for multiplication symbol
		/// </remarks>
		public static string ToFactorString(this IEnumerable<PrimeFactor> factors) {
			if (factors.Any()) {
				return string.Join(" × ",
					factors.OrderBy(f => f.Prime).Select(f =>
						f.Prime.ToString()
						+ (f.Exponent == 1 ? "" : "^" + f.Exponent.ToString())));
			} else {
				return "1";
			}
		}

		/// <summary>
		/// Simplifies a collection of prime factors representing a factorization of a fraction
		/// </summary>
		/// <param name="factors">The prime factors representing a factorization of a fraction</param>
		/// <returns>A simplified collection of prime factors that represents a fraction</returns>
		public static IEnumerable<PrimeFactor> Simplify(this IEnumerable<PrimeFactor> factors) {
			return factors
				.GroupBy(f => f.Prime)
				.Where(g => g.Sum(f => f.Exponent) != 0)//if Count() is optimized to access repeatedly, could maybe optimize with an OR g.Count() == 1
				.OrderBy(g => g.Key)
				.Select(g =>
					g.Count() == 1
						? g.First()//reuse instance
						: new PrimeFactor(g.Key, g.Sum(f => f.Exponent)));
		}
		
		/// <summary>
		/// Determines the prime factorization of the scalar needed to cancel out all denominators of the provided fractions' factorizations
		/// </summary>
		/// <param name="factorGroups">Collections of fraction factorizations</param>
		/// <returns>Determines the shared prime factors of the denominators represented by the provided collection of fraction factorizations</returns>
		public static IEnumerable<PrimeFactor> LargestDenominators(this IEnumerable<IEnumerable<PrimeFactor>> factorGroups) {
			return factorGroups
				.SelectMany(g => g.Simplify())
				.Where(f => f.Exponent < 0)
				.GroupBy(f => f.Prime)
				.Select(g => g.MinBy(f => f.Exponent))
				.Select(f => f.Reciprocol);
		}

		public static IEnumerable<PrimeFactor> SmallestNumerators(this IEnumerable<PrimeFactor>[] factorGroups) {
			IEnumerable<IEnumerable<PrimeFactor>> simplified = factorGroups.Select(g => g.Simplify());
			return factorGroups
				.Select(g => g.Simplify())
				.SelectMany()
				.Where(f => f.Exponent > 0)
				.GroupBy(f => f.Prime)
				.Where(g => g.Count() == factorGroups.Length)
				.Select(g => g.MinBy(f => f.Exponent));
		}
		public static IEnumerable<PrimeFactor> SmallestNumerators(this IEnumerable<IEnumerable<PrimeFactor>> factorGroups) {
			return SmallestNumerators(factorGroups.ToArray());
		}
		public static IEnumerable<PrimeFactor> SmallestNumerators(params Fraction[] fractions) {
			return SmallestNumerators(fractions.Select(f => f.CompoundFactorization).ToArray());
		}
		public static IEnumerable<PrimeFactor> SmallestNumerators(this IEnumerable<Fraction> fractions) {
			return SmallestNumerators(fractions.Select(f => f.CompoundFactorization).ToArray());
		}

		public static IEnumerable<PrimeFactor> LowestCommonMultiple(this IEnumerable<PrimeFactor>[] factorGroups) {
			IEnumerable<IEnumerable<PrimeFactor>> simplified = factorGroups.Select(g => g.Simplify());

			IEnumerable<PrimeFactor> numerator = simplified
				.SelectMany()
				.Where(f => f.Exponent > 0)
				.GroupBy(f => f.Prime)
				.Select(g => g.MaxBy(f => f.Exponent));
			IEnumerable<PrimeFactor> denominator = simplified
				.SelectMany()
				.Where(f => f.Exponent < 0)
				.GroupBy(f => f.Prime)
				.Where(g => g.Count() == factorGroups.Length)
				.Select(g => g.MaxBy(f => f.Exponent));//get smallest by magnitude (exponents are negative)

			return numerator.Concat(denominator).Simplify();
		}
		public static IEnumerable<PrimeFactor> LowestCommonMultiple(this IEnumerable<IEnumerable<PrimeFactor>> factorGroups) {
			return LowestCommonMultiple(factorGroups.ToArray());
		}
		public static IEnumerable<PrimeFactor> LowestCommonMultiple(params Fraction[] fractions) {
			return LowestCommonMultiple(fractions.Select(f => f.CompoundFactorization).ToArray());
		}
		public static IEnumerable<PrimeFactor> LowestCommonMultiple(this IEnumerable<Fraction> fractions) {
			return LowestCommonMultiple(fractions.Select(f => f.CompoundFactorization).ToArray());
		}

		/*the following does not work with repeats, and handling them makes the code overcomplicated
		internal static IEnumerable<PrimeFactor> SimplifyOptimized(IEnumerable<PrimeFactor> numerators, IEnumerable<PrimeFactor> denominators, bool denominatorsNegativeExponents) {
			IEnumerator<PrimeFactor> numeration = numerators.OrderBy(f => f.Prime).GetEnumerator();
			IEnumerator<PrimeFactor> denomination = denominators.OrderBy(f => f.Prime).GetEnumerator();
			bool anyNumerators = numeration.MoveNext();
			bool anyDenominators = denomination.MoveNext();

			while (anyNumerators || anyDenominators) {
				if (anyNumerators && anyDenominators) {
					switch (numeration.Current.Prime.CompareTo(denomination.Current.Prime)) {
						case -1:
							yield return numeration.Current;
							anyNumerators = numeration.MoveNext();
							break;
						case 0:
							if (numeration.Current.Exponent != (denominatorsNegativeExponents ? -1 : 1) * denomination.Current.Exponent)
								yield return new PrimeFactor(
									numeration.Current.Prime,
									numeration.Current.Exponent + ((denominatorsNegativeExponents ? 1 : -1) * denomination.Current.Exponent));
							anyNumerators = numeration.MoveNext();
							anyDenominators = denomination.MoveNext();
							break;
						case 1:
							yield return denominatorsNegativeExponents ? denomination.Current : denomination.Current.Reciprocol;
							anyDenominators = denomination.MoveNext();
							break;
						default:
							throw new ImpossibleCompareToException();
					}
				} else if (anyNumerators) {
					yield return numeration.Current;
					anyNumerators = numeration.MoveNext();
				} else if (anyDenominators) {
					yield return denominatorsNegativeExponents ? denomination.Current : denomination.Current.Reciprocol;
					anyDenominators = denomination.MoveNext();
				}
			}
		}
		*/
	}
}