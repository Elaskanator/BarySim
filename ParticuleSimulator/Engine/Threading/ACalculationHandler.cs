using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Generic.Classes;
using ParticleSimulator.Engine.Threading;

namespace ParticleSimulator.Engine {
	public abstract class ACalculationHandler : IRunnable {
		private const int _warn_interval_check_ms = 500;
		private static int _globalId = 0;

		public ACalculationHandler(AutoResetEvent[] signals, AutoResetEvent[] returns) {
			this.ReadySignals = signals ?? Array.Empty<AutoResetEvent>();
			this.DoneSignals = returns ?? Array.Empty<AutoResetEvent>();
		}
		public ACalculationHandler(AutoResetEvent readySignal, AutoResetEvent doneSignal)
		: this(new AutoResetEvent[] { readySignal }, new AutoResetEvent[] { doneSignal }) { }

		~ACalculationHandler() { this.Dispose(false); }

		public override string ToString() => string.Format("Runner{0}", this.Name is null ? "" : "[" + this.Name + "]");
		
		private readonly int _id = ++_globalId;
		public int Id => this._id;
		public bool IsOpen { get; private set; }
		public DateTime? StartTimeUtc { get; private set; }
		public DateTime? EndTimeUtc { get; private set; }
		public AutoResetEvent[] ReadySignals { get; private set; }
		public AutoResetEvent[] DoneSignals { get; private set; }

		public bool IsActive { get; private set; }
		public bool IsPaused { get; private set; }
		public int IterationCount { get; private set; }
		public int FullIterationCount { get; private set; }
		public bool IsWaiting { get; private set; }
		public bool IsComputing { get; private set; }

		public SimpleExponentialMovingTimeAverage WaitTime { get; private set; }
		public SimpleExponentialMovingTimeAverage SyncTime { get; private set; }
		public SimpleExponentialMovingTimeAverage ExclusiveTime { get; private set; }
		public SimpleExponentialMovingTimeAverage FullTime { get; private set; }
		public SimpleExponentialMovingTimeAverage FullTimePunctual { get; private set; }
		
		public virtual string Name => null;
		public virtual TimeSpan? SignalTimeout => null;
		public virtual Action<EvalResult> Callback => null;
		public virtual TimeSynchronizer Synchronizer => null;
		public virtual bool ReleaseEarly => false;
		protected virtual int StopWarnTimeMs => 1000;

		private Thread _thread;
		private ManualResetEvent _pauseSignal = new ManualResetEvent(true);
		private Stopwatch _timer = new Stopwatch();
		private Stopwatch _timerFull = new Stopwatch();
		private Stopwatch _timerFullPunctual = new Stopwatch();

		protected virtual void PreProcess(EvalResult prepResult) { }
		protected abstract void Process(EvalResult prepResult);
		protected virtual void PostProcess(EvalResult result) { }

		protected virtual void Init(bool running) { }
		protected virtual void Shutdown() { }

		public void Start(bool running = true) {
			if (this.IsOpen) {
				throw new InvalidOperationException("Already open");
			} else {
				this.IsActive = false;
				this.IterationCount = 0;
				this.FullIterationCount = 0;
				this.IsComputing = false;

				this.WaitTime = new SimpleExponentialMovingTimeAverage(Parameters.PERF_SMA_ALPHA);
				this.SyncTime = new SimpleExponentialMovingTimeAverage(Parameters.PERF_SMA_ALPHA);
				this.ExclusiveTime = new SimpleExponentialMovingTimeAverage(Parameters.PERF_SMA_ALPHA);
				this.FullTime = new SimpleExponentialMovingTimeAverage(Parameters.PERF_SMA_ALPHA);
				this.FullTimePunctual = new SimpleExponentialMovingTimeAverage(Parameters.PERF_SMA_ALPHA);

				this.IsOpen = true;
				this.StartTimeUtc = DateTime.UtcNow;

				this.Init(running);

				this.SetRunningState(running);

				this._thread = new Thread(Runner);
				this._thread.Start();
			}
		}

		public void Pause() {
			this.IsPaused = true;
			this._pauseSignal.Reset();
		}

		public void SetRunningState(bool running) {
			if (running) this.Resume();
			else this.Pause();
		}

		public virtual void TogglePause() {
			if (this.IsPaused)
				this.Resume();
			else this.Pause();
		}

		public void Resume() {
			this.IsPaused = false;
			this._pauseSignal.Set();
		}

		public void Stop() {
			if (this.IsOpen) {
				this.IsOpen = false;
				this.Shutdown();

				this._thread.Interrupt();
				DateTime startUtc = DateTime.UtcNow;
				if (!this._thread.Join(this.StopWarnTimeMs))
					this.PromptAbort(startUtc);
				this.EndTimeUtc = DateTime.UtcNow;
			} else throw new InvalidOperationException("Not open");
		}

		public void Restart(bool running = true) {
			this.Stop();
			this.Start(running);
		}

		public void Runner() {
			this.IsActive = true;
			try {
				bool isPunctual = true;
				this._timerFullPunctual.Restart();
				EvalResult evalResult;
				while (this.IsOpen) {
					evalResult = new();
					this._timerFull.Restart();

					if (this.ReadySignals.Length > 0) {
						this._timer.Restart();
						
						this.IsWaiting = true;
						isPunctual = this.SignalTimeout.HasValue
							? WaitHandle.WaitAll(this.ReadySignals, this.SignalTimeout.Value)
							: WaitHandle.WaitAll(this.ReadySignals);
						this.IsWaiting = false;

						this._timer.Stop();
						this.WaitTime.Update(this._timer.Elapsed);
						evalResult.PrepTime = this._timer.Elapsed;
					}
					evalResult.PrepPunctual = isPunctual;

					this.PreProcess(evalResult);
					if (this.ReleaseEarly)
						for (int i = 0; i < this.DoneSignals.Length; i++)
							this.DoneSignals[i].Set();
					
					this._timer.Restart();

					this._pauseSignal.WaitOne();

					this._timer.Stop();
					evalResult.PauseDelay = this._timer.Elapsed;

					if (!(this.Synchronizer is null)) {
						this.Synchronizer.Synchronize();
						evalResult.SyncDelay = this.Synchronizer.LastSyncDuration.Value;
						this.SyncTime.Update(this.Synchronizer.LastSyncDuration.Value);
					}
						
					if (this.IsOpen) {
						this._timer.Restart();

						this.IsComputing = true;
						this.Process(evalResult);
						this.IsComputing = false;

						this._timer.Stop();
						evalResult.ExclusiveTime = this._timer.Elapsed;
						this.ExclusiveTime.Update(this._timer.Elapsed);

						this._timerFull.Stop();
						this.FullTime.Update(this._timerFull.Elapsed);
						evalResult.TotalTime = this._timerFull.Elapsed;
						if (isPunctual) {
							this._timerFullPunctual.Stop();
							this.FullTimePunctual.Update(this._timerFullPunctual.Elapsed);
							evalResult.TotalTimePunctual = this._timerFullPunctual.Elapsed;
							this._timerFullPunctual.Restart();
						}

						this.PostProcess(evalResult);

						this.IterationCount++;
						if (isPunctual)
							this.FullIterationCount++;

						if (!(this.Callback is null))
							this.Callback(evalResult);
				
						if (!this.ReleaseEarly)
							for (int i = 0; i < this.DoneSignals.Length; i++)
								this.DoneSignals[i].Set();
					}
				}
			} catch (ThreadInterruptedException) { }//die

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