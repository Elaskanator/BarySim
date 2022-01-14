using System;

namespace Generic.Classes {
	/// <summary>
	/// Provides an endless enumeration of the prime number sequence
	/// </summary>
	internal class PrimeEnumerator : ALazyComputedSequenceEnumerator<long> {
		public static readonly PrimeEnumerator Singleton = new PrimeEnumerator();

		private PrimeEnumerator() : base(new[] { 2L, 3L }) { }

		/// <summary>
		/// Determines the next prime number using previous primes
		/// </summary>
		/// <todo>Try making a version incrementing with the modulus of some last known primes?</todo>
		protected override long ComputeNext() {
			long current = this.LastKnown + 2;//start at 2 past last known value, which is guaranteed odd because we initialize up thru 3

			int testIdx;
			long sqrt;
			bool isComposite;
			while (true) {//keep going until a new prime is found
				testIdx = 1;//all test values are odd, so skip testing the first known prime (two)
				sqrt = (long)Math.Sqrt(current);//round down, and avoid casting due to the comparison type of the while loop condition

				isComposite = false;
				while (this.Known[testIdx] <= sqrt) {
					if (current % this.Known[testIdx++] == 0L) {
						isComposite = true;
						break;
					}
				}

				if (isComposite) current += 2;
				else return current;//and end
			}
		}
	}
}