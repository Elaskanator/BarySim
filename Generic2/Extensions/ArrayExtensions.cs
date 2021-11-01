using System;

namespace Generic {
	public static class ArrayExtensions {
		public static T[] ShiftRight<T>(this T[] arr, bool wrapEnd = false) {
			T[] tmp = new T[arr.Length];

			if (wrapEnd) Array.Copy(arr, arr.Length-1, tmp, 0, 1); // move last position to first
			Array.Copy(arr, 0, tmp, 1, arr.Length-1); // copy over the rest

			return tmp;
		}
	}
}
