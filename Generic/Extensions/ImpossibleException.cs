using System;

namespace Generic.Extensions {
	/// <summary>
	/// Sometimes you just need to shut up the compiler about "Not all execution paths return a value" when your code is (hopefully) logically guaranteed
	/// </summary>
	public class ImpossibleException : Exception {
		public ImpossibleException(string message = null, Exception innerException = null)
			: base(message, innerException) { }
	}
	/// <summary>
	/// Sometimes you just need to shut up the compiler because a default clause of a switch(CompareTo()) doesn't make any sense
	/// </summary>
	public class ImpossibleCompareToException : ImpossibleException {
		public ImpossibleCompareToException(string message = "A CompareTo method call returned a value other than -1, 0, or 1", Exception innerException = null)
			: base(message, innerException) { }
	}
}