using System.Collections;
using System.Collections.Generic;

namespace Generic.Classes {
	public class Enumerator2<T> : IEnumerator<T> {
		private readonly IEnumerator<T> _iterator;
		public Enumerator2(IEnumerable<T> collection) {
			this._iterator = collection.GetEnumerator();
		}
		public Enumerator2(IEnumerator<T> iterator) {
			this._iterator = iterator;
		}

		public bool HasBegun { get; private set; }
		public bool HasEnded { get; private set; }
		public T Current { get { return this._iterator.Current; } }
		object IEnumerator.Current { get { return this._iterator.Current; } }

		public void Dispose() {
			this._iterator.Dispose();
		}

		public bool MoveNext() {
			this.HasBegun = true;
			return !(this.HasEnded = !this._iterator.MoveNext());
		}

		public void Reset() {
			this.HasBegun = this.HasEnded = false;
			this._iterator.Reset();
		}
	}
}
