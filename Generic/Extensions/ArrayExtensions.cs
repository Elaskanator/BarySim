using System;

namespace Generic.Extensions {
	public static class ArrayExtensions {
		public static T[] RotateRight<T>(this T[] sourceArray, int amount = 1, bool wrap = false) {
			amount %= sourceArray.Length;

			T[] result = new T[sourceArray.Length];
			if (wrap) Array.Copy(sourceArray, sourceArray.Length - amount, result, 0, amount);//copy end to beginning
			Array.Copy(sourceArray, 0, result, amount, sourceArray.Length - amount);//copy remainder

			return result;
		}

		public static T[] RotateLeft<T>(this T[] sourceArray, int amount = 1, bool wrap = false) {
			amount %= sourceArray.Length;

			T[] result = new T[sourceArray.Length];
			if (wrap) Array.Copy(sourceArray, 0, result, sourceArray.Length - amount, amount);//copy beginning to end
			Array.Copy(sourceArray, amount, result, 0, sourceArray.Length - amount);//copy remainder

			return result;
		}

		/// <summary>
		/// Removes the element at the specified index in the source array, shifting values to its right and returning an equal-sized array
		/// </summary>
		public static T[] RemoveShift<T>(this T[] sourceArray, int index) {
			T[] result = new T[sourceArray.Length];
			if (index > 0)
				Array.Copy(sourceArray, 0, result, 0, index);//elements before the index
			if (index < sourceArray.Length - 1)
				Array.Copy(sourceArray, index + 1, result, 0, sourceArray.Length - index - 1);//elements after the index
			return result;
		}
	}
}