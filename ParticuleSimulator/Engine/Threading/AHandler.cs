using System;
using System.Threading;
using Generic.Models;

namespace ParticleSimulator.Engine {
	public abstract class AHandler : IRunnable, IDisposable {
		private const int _warn_interval_check_ms = 500;

		public AHandler(EventWaitHandle[] signals, EventWaitHandle[] returns) {
			this.ReadySignals = signals ?? Array.Empty<EventWaitHandle>();
			this.DoneSignals = returns ?? Array.Empty<EventWaitHandle>();
		}
		public AHandler(EventWaitHandle readySignal, EventWaitHandle doneSignal) {
			this.ReadySignals = new EventWaitHandle[] { readySignal };
			this.DoneSignals = new EventWaitHandle[] { doneSignal };
		}

		~AHandler() { this.Dispose(false); }

		public override string ToString() => string.Format("Runner{0}", this.Name is null ? "" : "[" + this.Name + "]");

		public bool IsOpen { get; private set; }
		public DateTime? StartTimeUtc { get; private set; }
		public DateTime? EndTimeUtc { get; private set; }
		public EventWaitHandle[] ReadySignals { get; private set; }
		public EventWaitHandle[] DoneSignals { get; private set; }

		public SimpleExponentialMovingAverage ExclusiveTimeTicks { get; private set; }

		public bool IsActive { get; private set; }
		public int IterationCount { get; private set; }
		public int PunctualIterationCount { get; private set; }
		public TimeSpan? IterationDuration { get; private set; }
		public bool IsWaiting { get; private set; }
		public TimeSpan? WaitDuration { get; private set; }
		public TimeSpan? SynchronizeDuration { get; private set; }
		public bool? IsPunctual { get; private set; }
		public DateTime? ComputeStartUtc { get; private set; }
		public bool IsComputing { get; private set; }
		public TimeSpan? ComputeDuration { get; private set; }

		public virtual string Name => null;
		public virtual TimeSpan? SignalTimeout => null;
		public virtual Action<AHandler> Callback => null;
		public virtual TimeSynchronizer Synchronizer => null;
		protected virtual int StopWarnTimeMs => 1000;

		private Thread _thread;
		private EventWaitHandle _pauseSignal = new ManualResetEvent(true);

		protected abstract void Process();

		public void Start() {
			if (this.IsOpen) {
				throw new InvalidOperationException("Already open");
			} else {
				this.IsActive = false;
				this.IterationCount = 0;
				this.PunctualIterationCount = 0;
				this.IterationDuration = null;
				this.IsWaiting = false;
				this.WaitDuration = null;
				this.SynchronizeDuration = null;
				this.IsPunctual = null;
				this.IsComputing = false;
				this.ComputeDuration = null;
				this.ExclusiveTimeTicks = new SimpleExponentialMovingAverage(Parameters.PERF_SMA_ALPHA);

				this.IsOpen = true;
				this.StartTimeUtc = DateTime.UtcNow;

				this._pauseSignal.Set();
				this.PreStart();
				this._thread = new Thread(Runner);
				this._thread.Start();
				this.PostStart();
			}
		}
		protected virtual void PreStart() { }
		protected virtual void PostStart() { }

		public void Pause() {
			if (this.IsOpen) {
				this._pauseSignal.Reset();
			} else throw new InvalidOperationException("Not open");
		}

		public void Resume() {
			if (this.IsOpen) {
				this._pauseSignal.Set();
			} else throw new InvalidOperationException("Not open");
		}

		public void Stop() {
			if (this.IsOpen) {
				this.IsOpen = false;
				this.PreStop();
				this._thread.Interrupt();
				DateTime startUtc = DateTime.UtcNow;
				if (!this._thread.Join(this.StopWarnTimeMs))
					this.PromptAbort(startUtc);
				this.PostStop();
				this.EndTimeUtc = DateTime.UtcNow;
			} else throw new InvalidOperationException("Not open");
		}
		protected virtual void PreStop() { }
		protected virtual void PostStop() { }

		public void Restart() {
			this.Stop();
			this.Restart();
		}

		public void Runner() {
			this.IsActive = true;

			DateTime startUtc, endUtc;
			while (this.IsOpen) {

				if (this.ReadySignals.Length > 0) {
					this.IsWaiting = true;
					startUtc = DateTime.UtcNow;

					this.IsPunctual = this.WaitDuration.HasValue
						? WaitHandle.WaitAll(this.ReadySignals, this.WaitDuration.Value)
						: WaitHandle.WaitAll(this.ReadySignals);

					endUtc = DateTime.UtcNow;
					this.IsWaiting = false;
					this.WaitDuration = endUtc.Subtract(startUtc);
				}

				startUtc = DateTime.UtcNow;
				this._pauseSignal.WaitOne();
				endUtc = DateTime.UtcNow;
				this.SynchronizeDuration = endUtc.Subtract(startUtc);

				if (!(this.Synchronizer is null)) {
					this.Synchronizer.Synchronize();
					if (!this.IsOpen) return;
				}

				this.ComputeStartUtc = DateTime.UtcNow;
						
				if (this.IsOpen) {
					this.IsComputing = true;
					startUtc = DateTime.UtcNow;

					this.Process();

					endUtc = DateTime.UtcNow;
					this.IsComputing = false;
					this.ComputeDuration = endUtc.Subtract(startUtc);
					this.ExclusiveTimeTicks.Update(this.ComputeDuration.Value.Ticks);
					this.IterationDuration = this.WaitDuration.Value.Add(this.ComputeDuration.Value);
				
					for (int i = 0; this.IsOpen && i < this.DoneSignals.Length; i++)
						this.DoneSignals[i].Set();

					if (this.IsOpen && !(this.Callback is null))
						this.Callback(this);

					this.IterationCount++;
					this.PunctualIterationCount += this.IsPunctual.Value ? 1 : 0;
				}
			}

			this.IsActive = false;
		}

		public void Dispose() { this.Dispose(true); GC.SuppressFinalize(this); }
		public virtual void Dispose(bool fromDispose) {
			if (fromDispose) {
				if (this.IsOpen) {
					this.IsOpen = false;
					this._thread.Interrupt();
					this._thread = null;
					this._pauseSignal.Dispose();
					this._pauseSignal = null;
				}
				this._pauseSignal.Dispose();
			}
		}

		private void PromptAbort(DateTime startUtc) {
			bool oldVisibility = Console.CursorVisible;
			ConsoleColor oldForeground = Console.ForegroundColor,
				oldbackground = Console.BackgroundColor;
			
			Console.ForegroundColor = ConsoleColor.Red;
			Console.BackgroundColor = ConsoleColor.Black;

			bool doWait = true;
			while (doWait && this._thread.ThreadState == ThreadState.Running) {
				Console.Write("Shutdown is taking longer than normal ({0}). Press enter to ignore.",
					DateTime.UtcNow.Subtract(startUtc));
				Thread.Sleep(_warn_interval_check_ms);
				while (Console.KeyAvailable) {
					if (Console.ReadKey(true).Key == ConsoleKey.Enter) {
						doWait = false;
						break;
					}
				}
			}

			Console.ForegroundColor = oldForeground;
			Console.BackgroundColor = oldbackground;
			Console.CursorVisible = true;

			ConsoleKeyInfo keyInfo = Console.ReadKey(true);
			while (keyInfo.Key != ConsoleKey.Enter)
				keyInfo = Console.ReadKey(true);

			Console.CursorVisible = oldVisibility;
		}
	}
}