using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

using Generic.Extensions;
using Generic.Models;

namespace Simulation.Threading {
	public class SynchronizedDataBuffer : IDisposable, IEquatable<SynchronizedDataBuffer>, IEqualityComparer<SynchronizedDataBuffer> {
		private static int _id = 0;
		public int ID { get; private set; }
		public string Name { get; private set; }

		public int BufferSize { get; private set; }
		public object Current { get; private set; }
		public int TotalVolume { get; private set; }
		public int QueueLength { get; private set; }

		public bool DoTrackLatency {
			get { return this._trackLatency; }
			set { this._trackLatency = value; this._timerEnqueue.Reset(); this._timerDequeue.Reset(); } }
		public SampleSMA EnqueueTimings_Ticks { get; private set; }
		public SampleSMA DequeueTimings_Ticks { get; private set; }
		internal List<AutoResetEvent> RefreshListeners { get; private set; }

		public SynchronizedDataBuffer(string name, int size = 1) {
			if (size < 0) throw new ArgumentOutOfRangeException(nameof(size), "Must be nonzero");
			this.ID = ++_id;
			this.Name = name;
			this.BufferSize = size;
			this.EnqueueTimings_Ticks = new(Parameters.PERF_SMA_ALPHA);
			this.DequeueTimings_Ticks = new(Parameters.PERF_SMA_ALPHA);

			this.RefreshListeners = new List<AutoResetEvent>();
			this._queue = Enumerable.Repeat<object>(null, this.BufferSize).ToArray();
			this._latch_canContinue = new EventWaitHandle(size > 0, size > 0 ? EventResetMode.ManualReset : EventResetMode.AutoReset);
		}
		
		private bool _trackLatency = Parameters.DEBUG_ENABLE;
		private bool _isOpen = true;
		private bool _isNew = true;
		private Stopwatch _timerEnqueue = new(), _timerDequeue = new();
		private AutoResetEvent _latch_canAdd = new(true);
		private EventWaitHandle _latch_canContinue;
		private AutoResetEvent _latch_canPop = new(false);
		private ManualResetEvent _latch_hasAny = new(false);
		private object _lock = new();
		private object[] _queue;

		public object Peek() {
			if (!this._isOpen) return null;
			this._latch_hasAny.WaitOne();
			return this.Current;
		}

		public bool TryPeek(out object result) {
			if (this._isNew) {
				result = null;
				return false;
			} else {
				result = this.Current;
				return true;
			}
		}

		public void Overwrite(object value) {
			lock (this._lock) {
				if (this.QueueLength == 0 || this.BufferSize == 0) this.Add(value);//create initial
				else {
					this._queue[(this.TotalVolume + (this.QueueLength-1)) % this.BufferSize] = value;
					this.Current = value;
					this._latch_hasAny.Set();
					this.SignalRefreshListeners();
				}
			}
		}

		public void Enqueue(object value) {
			if (!this._isOpen) return;

			if (this.DoTrackLatency) this._timerEnqueue.Start();
			this._latch_canAdd.WaitOne();
			if (this.DoTrackLatency) {
				this._timerEnqueue.Stop();
				this.EnqueueTimings_Ticks.Update(this._timerEnqueue.ElapsedTicks);
				this._timerEnqueue.Reset();
			}

			if (!this._isOpen) return;
			lock (this._lock) this.Add(value);

			this._latch_canContinue.WaitOne();
		}

		public bool TryEnqueue(object value) {
			if (!this._isOpen || this.BufferSize == 0) return false;
			else lock (this._lock) {
				bool test  = false;
				if (this.QueueLength >= this.BufferSize) test = this._latch_canAdd.WaitOne(0);//I sure hope this doesn't cause a race condition
				else test = true;
				if (test) this.Add(value);
				return test;
			}
		}
		
		public object Dequeue(bool allowResume, TimeSpan? timeout = null) {
			if (!this._isOpen) return null;

			int waitResult = 0;
			if (this.DoTrackLatency) this._timerDequeue.Start();
			if (timeout.HasValue) waitResult = WaitHandle.WaitAny(new[] { this._latch_canPop }, timeout.Value);
			else this._latch_canPop.WaitOne();
			if (this.DoTrackLatency) {
				this._timerDequeue.Stop();
				this.DequeueTimings_Ticks.Update(this._timerDequeue.ElapsedTicks);
				this._timerDequeue.Reset();
			}

			if (waitResult == WaitHandle.WaitTimeout) return this.Current;
			else lock (this._lock) return this.Pop(allowResume);
		}
		public object Dequeue(TimeSpan? timeout = null) {
			return this.Dequeue(true, timeout);
		}
		
		public bool TryDequeue(bool allowResume, out object result) {
			result = null;
			if (!this._isOpen) return false;
			lock (this._lock) {
				if (this.QueueLength > 0) {
					bool test = this._latch_canPop.WaitOne(0);//I sure hope this doesn't cause a race condition
					if (test) result = this.Pop(allowResume);
					return test;
				} else {
					result = null;
					return false;
				}
			}
		}
		public bool TryDequeue(out object result) {
			return this.TryDequeue(true, out result);
		}
		
		internal void AllowResume() {
			this._latch_canContinue.Set();
		}
		// MUST lock and enforce size constraints before invoking either of these
		private object Pop(bool allowResume) {
			this._latch_canAdd.Set();
			if (allowResume) this._latch_canContinue.Set();
			if (this.QueueLength > this.BufferSize) this._latch_canPop.Set();

			if (this.BufferSize == 0) return this.Current;
			else return this._queue[(this.TotalVolume++ + --this.QueueLength) % this.BufferSize];
		}
		private void Add(object value) {
			if (this.BufferSize > 0) this._queue[(this.TotalVolume + this.QueueLength++) % this.BufferSize] = value;
			this.Current = value;
			this._isNew = false;

			this._latch_hasAny.Set();
			this._latch_canPop.Set();
			if (this.QueueLength < this.BufferSize) this._latch_canAdd.Set();

			this.SignalRefreshListeners();
		}
		private void SignalRefreshListeners() {
			foreach (AutoResetEvent e in this.RefreshListeners) e.Set();
		}

		public void Dispose() {
			this._isOpen = false;
			this._latch_canAdd.Set();
			this._latch_canPop.Set();
			this._latch_hasAny.Set();
			this._latch_canContinue.Set();
			GC.SuppressFinalize(this);
		}
		public override string ToString() {
			return string.Format("{0}[{1}, {2}{3}]",
				nameof(SynchronizedDataBuffer),
				this.Name,
				this.QueueLength.Pluralize("entry"),
				this.RefreshListeners.Count == 0 ? "" : this.RefreshListeners.Count.Pluralize(", listener"));
		}

		public override bool Equals(object obj) { return !(obj is null) && (obj is SynchronizedDataBuffer) && this.Equals(obj as SynchronizedDataBuffer); }
		public bool Equals(SynchronizedDataBuffer other) { return !(other is null) && this.ID == other.ID; }
		public bool Equals(SynchronizedDataBuffer x, SynchronizedDataBuffer y) { return (x is null && y is null) || (!(x is null || y is null) && x.ID == y.ID); }////(A && B) || (!(A || B) && x.ID == y.ID)  or  !(!A || !B || ((A && B) || x.ID != y.ID))
		public override int GetHashCode() { return base.GetHashCode(); }
		public int GetHashCode(SynchronizedDataBuffer obj) { return obj.ID.GetHashCode(); }
	}
}