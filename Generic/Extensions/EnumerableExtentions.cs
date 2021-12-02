using System;
using System.Collections.Generic;
using System.Linq;
using Generic.Models;

namespace Generic.Extensions {
	public static class EnumerableExtentions {
		#region Projections
		public static IEnumerable<T> Zip<I1, I2, I3, T>(this IEnumerable<I1> in1, IEnumerable<I2> in2, IEnumerable<I3> in3, Func<I1, I2, I3, T> projection) {
			IEnumerator<I1> it1 = in1.GetEnumerator();
			IEnumerator<I2> it2 = in2.GetEnumerator();
			IEnumerator<I3> it3 = in3.GetEnumerator();
			while (it1.MoveNext() && it2.MoveNext() && it3.MoveNext())
				yield return projection(it1.Current, it2.Current, it3.Current);
		}

		public static bool All<T>(this IEnumerable<T> source, Func<T, int, bool> test) {
			int count = 0;
			foreach (T t in source)
				if (!test(t, count++)) return false;
			return true;
		}

		public static bool Any<T>(this IEnumerable<T> source, Func<T, int, bool> test) {
			int count = 0;
			foreach (T t in source)
				if (test(t, count++)) return true;
			return false;
		}

		public static bool None<TSource>(this IEnumerable<TSource> source) { return !source.Any(); }
		public static bool None<TSource>(this IEnumerable<TSource> source, Predicate<TSource> predicate) { return !source.Any(x => predicate(x)); }
		public static bool None<T>(this IEnumerable<T> source, Func<T, int, bool> test) {
			int count = 0;
			foreach (T t in source)
				if (test(t, count++)) return false;
			return true;
		}

		/// <summary>
		/// Complement of the Where filter, returning only items that do not return true when using the projection
		/// </summary>
		public static IEnumerable<T> Without<T>(this IEnumerable<T> source, Predicate<T> test) {
			foreach (T element in source)
				if (!test(element)) yield return element;
		}
		/// <summary>
		/// Complement of the Where filter, returning only items that do not return true when using the projection
		/// </summary>
		public static IEnumerable<T> Without<T>(this IEnumerable<T> source, Func<T, int, bool> test) {
			int i = 0;
			foreach (T element in source)
				if (!test(element, i++)) yield return element;
		}
		public static IEnumerable<T> Without<T>(this IEnumerable<T> source, T skip)
		where T :IEquatable<T> {
			foreach (T element in source)
				if (!skip.Equals(element)) yield return element;
		}

		public static IEnumerable<T> TakeUntil<T>(this IEnumerable<T> source, Predicate<T> test) {
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

		public static Tuple<T, T> Range<T>(this IEnumerable<T> source)
		where T : IComparable<T> {
			bool isFirst = true;
			T min = default, max = default;
			foreach (T element in source) {
				if (isFirst) {
					isFirst = false;
					min = max = element;
				} else if (element.CompareTo(min) < 0)
					min = element;
				else if (element.CompareTo(max) > 0)
					max = element;
			}
			return new(min, max);
		}

		public static IEnumerable<IEnumerable<T>> Partition<T>(this IEnumerable<T> source, int size) {
			if (size < 1) throw new ArgumentOutOfRangeException(nameof(size), "Must be strictly positive");
			Enumerator2<T> iterator = new(source);
			while (!iterator.HasEnded) yield return SubPartition(iterator, size);
		}
		private static IEnumerable<T> SubPartition<T>(IEnumerator<T> iterator, int size) {
			int count = 0;
			while (count++ < size && iterator.MoveNext())
				yield return iterator.Current;
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
				if (!enumerator.MoveNext())
					throw new InvalidOperationException("Sequence contains no elements");

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
				if (!enumerator.MoveNext())
					throw new InvalidOperationException("Sequence contains no elements");

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
				if (!enumerator.MoveNext())
					throw new InvalidOperationException("Sequence contains no elements");

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