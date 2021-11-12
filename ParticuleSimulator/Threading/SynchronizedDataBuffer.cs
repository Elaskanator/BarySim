using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Generic.Extensions;
using Generic.Models;

namespace ParticleSimulator.Threading {
	public class SynchronizedDataBuffer : IDisposable, IEquatable<SynchronizedDataBuffer>, IEqualityComparer<SynchronizedDataBuffer> {
		private static int _id = 0;
		public int ID { get; private set; }
		public string Name { get; private set; }

		public SynchronizedDataBuffer(string name, int size = 1) {
			if (size < 0) throw new ArgumentOutOfRangeException(nameof(size), "Must be nonzero");
			this.ID = ++_id;
			this.Name = name;
			this.BUFFER_SIZE = size;
			this.EnqueueTimings_Ticks = new(Parameters.PERF_SMA_ALPHA);
			this.DequeueTimings_Ticks = new(Parameters.PERF_SMA_ALPHA);

			this._latch_canReturnFromAdd = new(false);
			this.ReleaseListeners = Array.Empty<AutoResetEvent>();
			this.RefreshListeners = new();
			this._queue = new object[this.BUFFER_SIZE];
		}
		public override string ToString() {
			return string.Format("{0}[{1}, {2}]",
				nameof(SynchronizedDataBuffer),
				this.Name,
				this.QueueLength.Pluralize("entry"));
		}

		public readonly int BUFFER_SIZE;
		public object Current { get; private set; }
		public int TotalEnqueues { get; private set; }
		public int TotalDequeues { get; private set; }
		public int QueueLength { get; private set; }

		public List<AutoResetEvent> RefreshListeners { get; private set; }
		public AutoResetEvent AddRefreshListener() {
			AutoResetEvent signal = new(false);
			this.RefreshListeners.Add(signal);
			return signal;
		}
		public AutoResetEvent[] ReleaseListeners { get; private set; }
		public AutoResetEvent AddReleaseListener() {
			AutoResetEvent signal = new(false);
			this.ReleaseListeners = ReleaseListeners.Append(signal).ToArray();
			return signal;
		}

		private AutoResetEvent _latch_canReturnFromAdd;
		private AutoResetEvent _latch_canAdd = new(true);
		private AutoResetEvent _latch_canPop = new(false);
		private ManualResetEvent _latch_hasAny = new(false);
		private object _lock = new();
		private object[] _queue;
		
		private bool _trackLatency = false;
		public bool DoTrackLatency {
			get { return this._trackLatency; }
			set { this._trackLatency = value; this._timerEnqueue.Reset(); this._timerDequeue.Reset(); } }
		public SampleSMA EnqueueTimings_Ticks { get; private set; }
		public SampleSMA DequeueTimings_Ticks { get; private set; }
		private Stopwatch _timerEnqueue = new(), _timerDequeue = new();

		// MUST lock and enforce size constraints before invoking either Pop or Add
		private object CommitPop() {
			this._latch_canAdd.Set();
			if (this.QueueLength > this.BUFFER_SIZE)
				this._latch_canPop.Set();

			if (this.BUFFER_SIZE == 0) {
				_latch_canReturnFromAdd.Set();
				return this.Current;
			}
			else return this._queue[(this.TotalDequeues++ + --this.QueueLength) % this.BUFFER_SIZE];
		}
		private void CommitAdd(object value) {
			if (this.BUFFER_SIZE > 0)
				this._queue[(this.TotalDequeues + this.QueueLength++) % this.BUFFER_SIZE] = value;
			this.Current = value;
			if (this.TotalEnqueues == 0)
				this._latch_hasAny.Set();
			this.TotalEnqueues++;

			this._latch_canPop.Set();
			if (this.QueueLength < this.BUFFER_SIZE)
				this._latch_canAdd.Set();
			
			foreach (AutoResetEvent e in this.RefreshListeners)
				e.Set();
		}

		public void Overwrite(object value) {
			lock (this._lock) {
				if (this.QueueLength == 0 || this.BUFFER_SIZE == 0) {
					this.CommitAdd(value);//create initial
				} else {
					this._queue[(this.TotalDequeues + (this.QueueLength-1)) % this.BUFFER_SIZE] = value;
					this.Current = value;
					if (this.TotalEnqueues == 0) {
						this.TotalEnqueues++;
						this._latch_hasAny.Set();
					}
					foreach (AutoResetEvent e in this.RefreshListeners) e.Set();
				}
			}
		}

		public object Peek() {
			if (this.TotalEnqueues == 0)
				this._latch_hasAny.WaitOne();
			return this.Current;
		}

		public bool TryPeek(ref object output) {
			bool result = false;
			if (this.TotalEnqueues > 0) {
				output = this.Current;
				result = true;
			}
			return result;
		}
		public bool TryPeek(ref object output, TimeSpan timeout) {
			bool result = false;
			if (this.TotalEnqueues > 0
			|| WaitHandle.WaitAny(new[] { this._latch_hasAny }, timeout) != WaitHandle.WaitTimeout) {
				output = this.Current;
				result = true;
			}
			return result;
		}
		
		public object Dequeue() {
			if (this.DoTrackLatency) this._timerDequeue.Start();
			this._latch_canPop.WaitOne();
			if (this.DoTrackLatency) {
				this._timerDequeue.Stop();
				this.DequeueTimings_Ticks.Update(this._timerDequeue.ElapsedTicks);
				this._timerDequeue.Reset();
			}

			object result;
			lock (this._lock)
				result = this.CommitPop();

			return result;
		}
		public bool TryDequeue(ref object output, TimeSpan timeout) {
			bool result = false;
			if (WaitHandle.WaitAny(new[] { this._latch_canPop }, timeout) != WaitHandle.WaitTimeout) {
				result = true;
				lock (this._lock)
					output = this.CommitPop();
			}
			return result;
		}

		public void Enqueue(object value) {
			if (this.DoTrackLatency) this._timerEnqueue.Start();
			this._latch_canAdd.WaitOne();
			if (this.DoTrackLatency) {
				this._timerEnqueue.Stop();
				this.EnqueueTimings_Ticks.Update(this._timerEnqueue.ElapsedTicks);
				this._timerEnqueue.Reset();
			}

			lock (this._lock)
				this.CommitAdd(value);
			
			if (this.BUFFER_SIZE == 0)
				this._latch_canReturnFromAdd.WaitOne();
		}

		public void Dispose() {
			this._latch_canAdd.Dispose();
			this._latch_canPop.Dispose();
			this._latch_hasAny.Dispose();
			foreach (AutoResetEvent e in this.RefreshListeners)
				e.Dispose();
		}

		public override bool Equals(object obj) { return !(obj is null) && (obj is SynchronizedDataBuffer) && this.Equals(obj as SynchronizedDataBuffer); }
		public bool Equals(SynchronizedDataBuffer other) { return !(other is null) && this.ID == other.ID; }
		public bool Equals(SynchronizedDataBuffer x, SynchronizedDataBuffer y) { return (x is null && y is null) || (!(x is null || y is null) && x.ID == y.ID); }////(A && B) || (!(A || B) && x.ID == y.ID)  or  !(!A || !B || ((A && B) || x.ID != y.ID))
		public override int GetHashCode() { return base.GetHashCode(); }
		public int GetHashCode(SynchronizedDataBuffer obj) { return obj.ID.GetHashCode(); }
	}
}