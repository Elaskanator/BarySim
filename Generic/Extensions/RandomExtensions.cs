using System;

namespace Generic.Extensions {
	public static class RandomExtensions {
		/// SEEALSO https://stackoverflow.com/a/110570/2799848
		/// Fisher-Yates algorithm
		public static void ShuffleInPlace<T>(this Random rng, T[] array) {
			int n = array.Length;
			while (n > 1) {
				int k = rng.Next(n--);
				T temp = array[n];
				array[n] = array[k];
				array[k] = temp;
			}
		}
	}
}