using System;
using System.Collections.Generic;
using System.Threading;
using Generic.Extensions;

namespace ParticleSimulator.Engine {
	public class SynchronousBuffer<T> : ISynchronousConsumedResource {
		private static int _globalId = 0;

		public SynchronousBuffer(string name, int size = 1) {
			if (size < 0) throw new ArgumentOutOfRangeException(nameof(size), "Must be nonzero");
			this.Name = name;
			this.BufferSize = size;

			//this.RefreshReleaseListeners = Array.Empty<AutoResetEvent>();
			this.RefreshListeners = new();
			this._queue = new T[this.BufferSize];
			this._latch_canReturnFromAdd = new(size > 0);
		}

		~SynchronousBuffer() { this.Dispose(false); }

		public override string ToString() => string.Format("SynchronizedBuffer<{0}>[{1}{2}]", this.Id, this.Name is null ? "" : this.Name + ", ", this.Count.Pluralize("entry"));

		private readonly int _id = ++_globalId;
		public int Id => this._id;
		public string Name { get; private set; }
		
		public Type DataType => typeof(T);
		public int BufferSize { get; private set; }
		public int Count { get; private set; }
		public int TotalEnqueues { get; private set; }
		public int TotalDequeues { get; private set; }
		public bool IsReadOnly => false;

		public T Current { get; private set; }
		object ISynchronousConsumedResource.Current => this.Current;

		private AutoResetEvent _latch_canReturnFromAdd;
		private AutoResetEvent _latch_canAdd = new(true);
		private AutoResetEvent _latch_canPop = new(false);
		private ManualResetEvent _latch_hasAny = new(false);
		private object _lock = new();
		private T[] _queue;

		public AutoResetEvent AddRefreshListener() {
			AutoResetEvent signal = new(false);
			this.RefreshListeners.Add(signal);
			return signal;
		}
		protected List<AutoResetEvent> RefreshListeners { get; private set; }

		//public AutoResetEvent[] RefreshReleaseListeners { get; private set; }
		//public AutoResetEvent AddRefreshReleaseListener() {
		//	AutoResetEvent signal = new(false);
		//	this.RefreshReleaseListeners = RefreshReleaseListeners.Append(signal).ToArray();
		//	return signal;
		//}

		public T Peek() {
			if (this.TotalEnqueues == 0)
				this._latch_hasAny.WaitOne();
			return this.Current;
		}
		object ISynchronousConsumedResource.Peek() => this.Peek();

		public bool TryPeek(ref T output) {
			bool result = false;
			if (this.TotalEnqueues > 0) {
				output = this.Current;
				result = true;
			}
			return result;
		}
		public bool TryPeek(ref T output, TimeSpan timeout) {
			bool result = false;
			if (this.TotalEnqueues > 0
			|| WaitHandle.WaitAny(new[] { this._latch_hasAny }, timeout) != WaitHandle.WaitTimeout) {
				output = this.Current;
				result = true;
			}
			return result;
		}

		public void Enqueue(T value) {
			this._latch_canAdd.WaitOne();

			lock (this._lock)
				this.CommitAdd(value);
			
			if (this.BufferSize == 0)
				this._latch_canReturnFromAdd.WaitOne();
		}
		void ISynchronousConsumedResource.Enqueue(object item) => this.Enqueue((T)item);

		public void Overwrite(T value) {
			lock (this._lock) {
				if (this.Count == 0 || this.BufferSize == 0) {
					this.CommitAdd(value);//create initial
				} else {
					this._queue[(this.TotalDequeues + (this.Count-1)) % this.BufferSize] = value;
					this.Current = value;
					if (this.TotalEnqueues == 0) {
						this.TotalEnqueues++;
						this._latch_hasAny.Set();
					}
					foreach (AutoResetEvent e in this.RefreshListeners) e.Set();
				}
			}
		}
		public void Overwrite(object item) => this.Overwrite((T)item);
		
		public T Dequeue() {
			this._latch_canPop.WaitOne();

			T result;
			lock (this._lock)
				result = this.CommitPop();

			return result;
		}
		object ISynchronousConsumedResource.Dequeue() => this.Dequeue();
		public bool TryDequeue(ref T output, TimeSpan timeout) {
			bool result = false;
			if (WaitHandle.WaitAny(new[] { this._latch_canPop }, timeout) != WaitHandle.WaitTimeout) {
				result = true;
				lock (this._lock)
					output = this.CommitPop();
			}
			return result;
		}

		public void Reset() {
			this.Count = 0;
			this.TotalEnqueues = 0;
			this.TotalDequeues = 0;
			
			if (this.BufferSize > 0)
				this._latch_canReturnFromAdd.Set();
			else this._latch_canReturnFromAdd.Reset();
			this._latch_canAdd.Set();
			this._latch_canPop.Reset();
			this._latch_hasAny.Reset();
		}

		// MUST lock and enforce size constraints before invoking either Pop or Add
		private T CommitPop() {
			this._latch_canAdd.Set();
			if (this.Count > 1)
				this._latch_canPop.Set();

			if (this.BufferSize == 0) {
				_latch_canReturnFromAdd.Set();
				return this.Current;
			} else return this._queue[(this.TotalDequeues++ + --this.Count) % this.BufferSize];
		}

		private void CommitAdd(T value) {
			if (this.BufferSize > 0)
				this._queue[(this.TotalDequeues + this.Count++) % this.BufferSize] = value;
			this.Current = value;
			if (this.TotalEnqueues == 0)
				this._latch_hasAny.Set();
			this.TotalEnqueues++;

			this._latch_canPop.Set();
			if (this.Count < this.BufferSize)
				this._latch_canAdd.Set();
			
			foreach (AutoResetEvent e in this.RefreshListeners)
				e.Set();
		}
		
		public void Dispose() { this.Dispose(true); GC.SuppressFinalize(this); }
		private void Dispose(bool fromDispose) {
			if (fromDispose) {
				this._latch_hasAny.Dispose();
				this._latch_canAdd.Dispose();
				this._latch_canReturnFromAdd.Dispose();
				this._latch_canPop.Dispose();
				foreach (AutoResetEvent e in this.RefreshListeners)
					e.Dispose();
			}
		}
	}
}