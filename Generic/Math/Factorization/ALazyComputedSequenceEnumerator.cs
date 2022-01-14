using System;
using System.Collections;
using System.Collections.Generic;

namespace Generic.Classes {
	public abstract class ALazyComputedSequenceEnumerator<T> : IEnumerator<T>, IDisposable {
		public static readonly int WARN_LIMIT_IDX = 100000;
		public static readonly int SAFETY_LIMIT_IDX = 10000000;

		public T this[int idx] {
			get {
				this.EstablishValues(idx);
				return this.Known[idx];
			}
		}
		public IEnumerable<T> GetRange(int count) {
			this.EstablishValues(count);
			for (int i = 0; i < count; i++) yield return this.Known[i];
		}
		public IEnumerable<T> GetRange(int startIdx, int count) {
			if (startIdx < 0) throw new ArgumentOutOfRangeException("startIdx", startIdx, "Must be nonnegative");
			else if (count < 0) throw new ArgumentOutOfRangeException("count", count, "Must be nonnegative");
			
			int endIdx = startIdx + count;
			this.EstablishValues(endIdx);

			for (int i = startIdx; i < endIdx; i++) {
				yield return this.Known[i];
			}
		}

		#region Protected
		protected List<T> Known = new List<T>() { };
		protected T LastKnown { get { return this.Known[^1]; } }

		protected ALazyComputedSequenceEnumerator() { }
		protected ALazyComputedSequenceEnumerator(IEnumerable<T> initialValues) {
			this.Known.AddRange(initialValues);
		}
		protected abstract T ComputeNext();
		#endregion Protected

		#region Private
		private int _currentIdx = -1;
		private bool _unwarned = true;
		private void EstablishValues(int idx) {
			if (idx < 0) throw new ArgumentOutOfRangeException("idx", idx, "Must be nonnegative");
			else this.ValidateSafetyLimits(idx);

			for (int i = this.Known.Count; i <= idx; i++) {
				this.Known.Add(this.ComputeNext());
			}
		}
		private T ComputeNextInternal() {
			this.ValidateSafetyLimits(this.Known.Count);
			return this.ComputeNext();
		}
		private void ValidateSafetyLimits(int idx) {
			if (idx >= SAFETY_LIMIT_IDX) {
				throw new Exception("Sequence index sought beyond safety limit of " + WARN_LIMIT_IDX);
			} else if (this._unwarned && idx >= WARN_LIMIT_IDX) {
				this._unwarned = false;
				Console.WriteLine("Sequence index sought beyond warn limit of " + WARN_LIMIT_IDX);
			}
		}
		#endregion Private

		#region Enumeration
		public T Current { get { return this.Known[this._currentIdx]; } }//if we actually NEED to use System.Long, everything is sure to explode
		object IEnumerator.Current { get { return this.Current; } }
		/// <summary>
		/// Sets this iterator to the next prime number, computing new ones as needed
		/// </summary>
		/// <returns>True, as there are an infinite number of primes</returns>
		public bool MoveNext() {
			if (++this._currentIdx >= this.Known.Count) {
				this.Known.Add(this.ComputeNextInternal());
			}
			return true;//no end
		}
		public void Reset() {
			this._unwarned = true;
			this._currentIdx = -1;
		}
		#endregion Enumeration

		public virtual void Dispose() {
			this.Known = null;
		}
	}
}