using System;
using System.Diagnostics;
using System.Threading;
using Generic.Models;

namespace ParticleSimulator.Engine {
	public abstract class ACalculationHandler : IRunnable {
		private const int _warn_interval_check_ms = 500;
		private static int _globalId = 0;

		public ACalculationHandler(EventWaitHandle[] signals, EventWaitHandle[] returns) {
			this.ReadySignals = signals ?? Array.Empty<EventWaitHandle>();
			this.DoneSignals = returns ?? Array.Empty<EventWaitHandle>();
		}
		public ACalculationHandler(EventWaitHandle readySignal, EventWaitHandle doneSignal)
		: this(new EventWaitHandle[] { readySignal }, new EventWaitHandle[] { doneSignal }) { }

		~ACalculationHandler() { this.Dispose(false); }

		public override string ToString() => string.Format("Runner{0}", this.Name is null ? "" : "[" + this.Name + "]");
		
		private readonly int _id = ++_globalId;
		public int Id => this._id;
		public bool IsOpen { get; private set; }
		public DateTime? StartTimeUtc { get; private set; }
		public DateTime? EndTimeUtc { get; private set; }
		public EventWaitHandle[] ReadySignals { get; private set; }
		public EventWaitHandle[] DoneSignals { get; private set; }

		public bool IsActive { get; private set; }
		public int IterationCount { get; private set; }
		public int PunctualIterationCount { get; private set; }
		public bool IsWaiting { get; private set; }
		public bool IsComputing { get; private set; }
		public DateTime? LastComputeStartUtc { get; private set; }

		public SimpleExponentialMovingTimeAverage Waitiyme { get; private set; }
		public SimpleExponentialMovingTimeAverage SyncTime { get; private set; }
		public SimpleExponentialMovingTimeAverage ExclusiveTime { get; private set; }
		
		public virtual string Name => null;
		public virtual TimeSpan? SignalTimeout => null;
		public virtual Action<bool> Callback => null;
		public virtual TimeSynchronizer Synchronizer => null;
		protected virtual int StopWarnTimeMs => 1000;

		private Thread _thread;
		private EventWaitHandle _pauseSignal = new ManualResetEvent(true);
		private Stopwatch _timer = new Stopwatch();

		protected abstract void Process(bool punctual);
		protected virtual void PreStart() { }
		protected virtual void PostStop() { }

		public void Start() {
			if (this.IsOpen) {
				throw new InvalidOperationException("Already open");
			} else {
				this.IsActive = false;
				this.IterationCount = 0;
				this.PunctualIterationCount = 0;
				this.IsComputing = false;
				this.LastComputeStartUtc = null;

				this.Waitiyme = new SimpleExponentialMovingTimeAverage(Parameters.PERF_SMA_ALPHA);
				this.SyncTime = new SimpleExponentialMovingTimeAverage(Parameters.PERF_SMA_ALPHA);
				this.ExclusiveTime = new SimpleExponentialMovingTimeAverage(Parameters.PERF_SMA_ALPHA);

				this.IsOpen = true;
				this.StartTimeUtc = DateTime.UtcNow;

				this._pauseSignal.Set();
				this.PreStart();
				this._thread = new Thread(Runner);
				this._thread.Start();
			}
		}

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
				this._thread.Interrupt();
				DateTime startUtc = DateTime.UtcNow;
				if (!this._thread.Join(this.StopWarnTimeMs))
					this.PromptAbort(startUtc);
				this.PostStop();
				this.EndTimeUtc = DateTime.UtcNow;
			} else throw new InvalidOperationException("Not open");
		}

		public void Restart() {
			this.Stop();
			this.Restart();
		}

		public void Runner() {
			this.IsActive = true;

			bool isPunctual = true;
			while (this.IsOpen) {
				if (this.ReadySignals.Length > 0) {
					this._timer.Reset();
					this._timer.Start();

					this.IsWaiting = true;
					isPunctual = this.SignalTimeout.HasValue
						? WaitHandle.WaitAll(this.ReadySignals, this.SignalTimeout.Value)
						: WaitHandle.WaitAll(this.ReadySignals);
					this.IsWaiting = false;

					this._timer.Stop();
					this.Waitiyme.Update(this._timer.Elapsed);
				}
				
				this._timer.Reset();
				this._timer.Start();

				this._pauseSignal.WaitOne();

				this._timer.Stop();
				this.SyncTime.Update(this._timer.Elapsed);

				if (!(this.Synchronizer is null)) {
					this.Synchronizer.Synchronize();
					if (!this.IsOpen) return;
				}
						
				if (this.IsOpen) {
					this._timer.Reset();
					this.LastComputeStartUtc = DateTime.UtcNow;
					this._timer.Start();

					this.IsComputing = true;
					this.Process(isPunctual);
					this.IsComputing = false;

					this._timer.Stop();
					this.ExclusiveTime.Update(this._timer.Elapsed);
				
					for (int i = 0; this.IsOpen && i < this.DoneSignals.Length; i++)
						this.DoneSignals[i].Set();

					this.IterationCount++;
					this.PunctualIterationCount += isPunctual ? 1 : 0;

					if (this.IsOpen && !(this.Callback is null))
						this.Callback(isPunctual);
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
			while (doWait && this._thread.ThreadState == System.Threading.ThreadState.Running) {
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