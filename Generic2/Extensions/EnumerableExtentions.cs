﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace Generic {
	public static class EnumerableExtentions {
		#region Projections
		/// <summary>
		/// Complement of the Where filter, returning only items that do not return true when using the projection
		/// </summary>
		public static IEnumerable<T> Except<T>(this IEnumerable<T> source, Func<T, bool> test) {
			foreach (T element in source)
				if (!test(element)) yield return element;
		}
		public static IEnumerable<T> Except<T>(this IEnumerable<T> source, T skip)
		where T :IEquatable<T> {
			foreach (T element in source)
				if (!skip.Equals(element)) yield return element;
		}

		public static IEnumerable<T> TakeUntil<T>(this IEnumerable<T> source, Func<T, bool> test) {
			foreach (T element in source)
				if (test(element)) yield break;
				else yield return element;
		}

		public static IEnumerable<T> SelectMany<T>(this IEnumerable<IEnumerable<T>> source) {
			return source.SelectMany(x => x);
		}

		public static IEnumerable<TOut> SelectDistinct<TIn, TOut>(this IEnumerable<TIn> source, Func<TIn, TOut> projection)
		where TOut : IEquatable<TOut> {
			return source.Select(x => projection(x)).Distinct();
		}
		
		public static IEnumerable<T> DistinctDuplicates<T>(this IEnumerable<T> source)
		where T : IEquatable<T> {
			return source.Except(source.Distinct()).Distinct();
		}

		public static IEnumerable<T> Order<T>(this IEnumerable<T> source)
		where T : IComparable<T> {
			return source.OrderBy(x => x);
		}
		public static IEnumerable<T> OrderDescending<T>(this IEnumerable<T> source)
		where T : IComparable<T> {
			return source.OrderByDescending(x => x);
		}
		#endregion Projections

		#region Aggregations
		public static TSource MinBy<TSource, TProjected>(this IEnumerable<TSource> source, Func<TSource, TProjected> projection)
		where TProjected :IComparable<TProjected> {
			if (source is null) throw new ArgumentNullException("source");
			if (projection is null) throw new ArgumentNullException("projection");

			using (IEnumerator<TSource> enumerator = source.GetEnumerator()) {
				if (!enumerator.MoveNext()) {
					throw new InvalidOperationException("Sequence contains no elements");
				}
				TProjected minProjected, currentProjected;
				TSource min = enumerator.Current;
				minProjected = projection(min);
				while (enumerator.MoveNext()) {
					currentProjected = projection(enumerator.Current);
					if (currentProjected.CompareTo(minProjected) < 0) {
						min = enumerator.Current;
						minProjected = currentProjected;
					}
				}
				return min;
			}
		}
		public static TSource MinBy<TSource, TProjected>(this IEnumerable<TSource> source, Func<TSource, TProjected> projection, IComparer<TProjected> comparer = null) {
			comparer = comparer ?? Comparer<TProjected>.Default;
			if (source is null) throw new ArgumentNullException("source");
			if (projection is null) throw new ArgumentNullException("projection");

			using (IEnumerator<TSource> enumerator = source.GetEnumerator()) {
				if (!enumerator.MoveNext()) {
					throw new InvalidOperationException("Sequence contains no elements");
				}
				TProjected minProjected, currentProjected;
				TSource min = enumerator.Current;
				minProjected = projection(min);
				while (enumerator.MoveNext()) {
					currentProjected = projection(enumerator.Current);
					if (comparer.Compare(currentProjected, minProjected) < 0) {
						min = enumerator.Current;
						minProjected = currentProjected;
					}
				}
				return min;
			}
		}

		public static TSource MaxBy<TSource, TProjected>(this IEnumerable<TSource> source, Func<TSource, TProjected> projection)
		where TProjected :IComparable<TProjected> {
			if (source is null) throw new ArgumentNullException("source");
			if (projection is null) throw new ArgumentNullException("projection");

			using (IEnumerator<TSource> enumerator = source.GetEnumerator()) {
				if (!enumerator.MoveNext()) {
					throw new InvalidOperationException("Sequence contains no elements");
				}
				TProjected maxProjected, currentProjected;
				TSource max = enumerator.Current;
				maxProjected = projection(max);
				while (enumerator.MoveNext()) {
					currentProjected = projection(enumerator.Current);
					if (currentProjected.CompareTo(maxProjected) > 0) {
						max = enumerator.Current;
						maxProjected = currentProjected;
					}
				}
				return max;
			}
		}
		public static TSource MaxBy<TSource, TProjected>(this IEnumerable<TSource> source, Func<TSource, TProjected> projection, IComparer<TProjected> comparer = null) {
			comparer = comparer ?? Comparer<TProjected>.Default;
			if (source is null) throw new ArgumentNullException("source");
			if (projection is null) throw new ArgumentNullException("projection");

			using (IEnumerator<TSource> enumerator = source.GetEnumerator()) {
				if (!enumerator.MoveNext()) {
					throw new InvalidOperationException("Sequence contains no elements");
				}
				TProjected maxProjected, currentProjected;
				TSource max = enumerator.Current;
				maxProjected = projection(max);
				while (enumerator.MoveNext()) {
					currentProjected = projection(enumerator.Current);
					if (comparer.Compare(currentProjected, maxProjected) < 0) {
						max = enumerator.Current;
						maxProjected = currentProjected;
					}
				}
				return max;
			}
		}

		public static long Product(this IEnumerable<long> source) {
			return source.Aggregate(1L, (acc, x) => acc *= x);
		}
		public static int Product(this IEnumerable<int> source) {
			return source.Aggregate(1, (acc, x) => acc *= x);
		}
		public static double Product(this IEnumerable<double> source) {
			return source.Aggregate(1d, (acc, x) => acc *= x);
		}
		#endregion Aggregations
	}
}