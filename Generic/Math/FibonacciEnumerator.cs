namespace Generic.Classes {
	public class FibonacciEnumerator : ALazyComputedSequenceEnumerator<long> {
		public FibonacciEnumerator(long x0 = 0, long x1 = 1) : base(new[] { x0, x1 }) { }

		protected override long ComputeNext() {
			return this.Known[^1] + this.Known[^2];
		}

		public override string ToString() {
			return string.Format("FibonacciEnumerator({0}, {1})",
				this.Known[0],
				this.Known[1]);
		}
	}
}