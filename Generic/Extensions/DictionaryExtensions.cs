using System;
using System.Collections.Generic;
using System.Linq;

namespace Generic.Extensions {
	public static class DictionaryExtensions {
		public static void AddMany<TKey, TValue>(this Dictionary<TKey, TValue> source, IEnumerable<TValue> additions, Func<TValue, TKey> projection) {
			foreach (TValue value in additions) source.Add(projection(value), value);
		}
		public static void AddMany<TKey, TValue>(this Dictionary<TKey, TValue> source, IEnumerable<TKey> additions, Func<TKey, TValue> projection) {
			foreach (TKey value in additions) source.Add(value, projection(value));
		}
		public static void AddMany<TKey, TValue>(this SortedDictionary<TKey, TValue> source, IEnumerable<TValue> additions, Func<TValue, TKey> projection) {
			foreach (TValue value in additions) source.Add(projection(value), value);
		}
		public static void AddMany<TKey, TValue>(this SortedDictionary<TKey, TValue> source, IEnumerable<TKey> additions, Func<TKey, TValue> projection) {
			foreach (TKey value in additions) source.Add(value, projection(value));
		}
		
		public static Dictionary<TKey, TValue> ToDictionary<TKey, TValue>(this IEnumerable<Tuple<TKey, TValue>> source)
		where TKey : IComparable<TKey> {
			return source.ToDictionary(t => t.Item1, t => t.Item2);
		}
		public static SortedDictionary<TKey, TValue> ToSortedDictionary<TKey, TValue>(this IEnumerable<Tuple<TKey, TValue>> source)
		where TKey : IComparable<TKey> {
			return new SortedDictionary<TKey, TValue>(source.ToDictionary(t => t.Item1, t => t.Item2));
		}
		public static SortedDictionary<TKey, TElement> ToSortedDictionary<TKey, TValue, TElement>(this IEnumerable<TValue> source, Func<TValue, TKey> keySelector, Func<TValue, TElement> elementSelector)
		where TKey : IComparable<TKey> {
			return new SortedDictionary<TKey, TElement>(source.ToDictionary(i => keySelector(i), i => elementSelector(i)));
		}
		public static SortedDictionary<TKey, TValue> ToSortedDictionary<TKey, TValue>(this IEnumerable<TValue> source, Func<TValue, TKey> keySelector)
		where TKey : IComparable<TKey> {
			return source.ToSortedDictionary(i => keySelector(i), i => i);
		}
		public static SortedDictionary<TKey, TValue[]> ToSortedDictionary<TKey, TValue>(this IEnumerable<IGrouping<TKey, TValue>> source)
		where TKey : IComparable<TKey> {
			return source.ToSortedDictionary(t => t.Key, t => t.ToArray());
		}
	}
}