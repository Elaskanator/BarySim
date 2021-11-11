using System;
using System.Linq;
using System.Threading;
using Generic.Extensions;

namespace ParticleSimulator.Threading {
	#pragma warning disable CS0162 // Unreachable code detected
	public class StepEvaluator : IDisposable {
		public readonly EvaluationStep Step;

		public bool IsActive { get; internal set; }

		public int NumCompleted { get; private set; }
		public DateTime? IterationStartUtc { get; private set; }
		public DateTime? IterationReceiveUtc { get; private set; }
		public DateTime? IterationSyncResumeUtc { get; private set; }
		public DateTime? IterationCalcEndUtc { get; private set; }
		public DateTime? IterationEndUtc { get; private set; }
		public bool? IsPunctual { get; private set; }
		
		private Thread[] _threads;
		private EventWaitHandle[] _handles;
		private object[] _ingestBuffer;

		public StepEvaluator(EvaluationStep step) {
			this.Step = step;
			if (step.InputResourceUses is null)
				this._ingestBuffer = Array.Empty<object>();
			else this._ingestBuffer = new object[step.InputResourceUses.Length];
		}
		public override string ToString() {
			return string.Format("{0}[{1}]", nameof(StepEvaluator), this.Step.Name);
		}
		
		public void Start() {
			this.IsActive = true;
			
			int numAssimilationThreads = this.Step.InputResourceUses is null ? 0 : this.Step.InputResourceUses.Length;
			this._threads = new Thread[numAssimilationThreads + 1];

			Thread thread;
			if (numAssimilationThreads > 0) {
				ParameterizedThreadStart threadStart;
				Tuple<int, Prerequisite, EventWaitHandle, EventWaitHandle, EventWaitHandle, EventWaitHandle>[] startInfos
					= new Tuple<int, Prerequisite, EventWaitHandle, EventWaitHandle, EventWaitHandle, EventWaitHandle>[numAssimilationThreads];

				Prerequisite req;
				for (int pIdx = 0; pIdx < this.Step.InputResourceUses.Length; pIdx++) {
					req = this.Step.InputResourceUses[pIdx];

					startInfos[pIdx] = new(
						pIdx,
						req,
						new AutoResetEvent(true),
						new AutoResetEvent(req.AllowDirtyRead),
						req.OnChange ? req.Resource.AddRefreshListener() : null,
						req.DoHold ? req.Resource.AddReleaseListener() : null);

					threadStart = new(this.ReceiveInput);
					thread = new(threadStart);
					this._threads[pIdx + 1] = thread;

					thread.Start(startInfos[pIdx]);
				}

				Tuple<EventWaitHandle[], EventWaitHandle[]> moreStartInfos = new(
					startInfos.Select(t => t.Item4).Except(x => x is null).ToArray(),
					startInfos.Select(t => t.Item3)
						.Concat(startInfos.Select(t => t.Item6)).Except(x => x is null).ToArray());
				
				this._handles =
					startInfos.Select(t => t.Item3)
						.Concat(startInfos.Select(t => t.Item3))
						.Concat(startInfos.Select(t => t.Item4))
						.Concat(startInfos.Select(t => t.Item5))
						.Concat(startInfos.Select(t => t.Item6))
						.Except(x => x is null)
						.ToArray();

				threadStart = new(this.Process);
				thread = new(threadStart);
				this._threads[0] = thread;

				thread.Start(moreStartInfos);
			} else {
				this._handles = Array.Empty<EventWaitHandle>();

				thread = new(() => this.Process(null));
				this._threads[0] = thread;

				thread.Start();
			}
		}

		public void Dispose() {
			if (!this.IsActive) return;

			this.IsActive = false;
			foreach (Prerequisite prereq in this.Step.InputResourceUses)
				prereq.Resource.Dispose();
			foreach (EventWaitHandle handle in this._handles)
				handle.Dispose();
			foreach (Thread thread in this._threads)
				thread.Join(0);
		}

		private void ReceiveInput(object info) {
			Tuple<int, Prerequisite, EventWaitHandle, EventWaitHandle, EventWaitHandle, EventWaitHandle> castedInfo
				= (Tuple<int, Prerequisite, EventWaitHandle, EventWaitHandle, EventWaitHandle, EventWaitHandle>)info;
			int outIdx = castedInfo.Item1;
			Prerequisite req = castedInfo.Item2;
			EventWaitHandle
				signal = castedInfo.Item3,
				returnSignal = castedInfo.Item4,
				refreshSignal = castedInfo.Item5;

			int skips = 0, reuses = 0;
			bool allowAccess, allowReuse;
			while (this.IsActive) {
				signal.WaitOne();
				if (Parameters.ENABLE_VERBOSE_DEBUG_PROCESS_LOGGING) DebugExtensions.DebugWriteline(string.Format("Receiving - {0} {1} {2} start", this.Step.Name, req.Resource.Name, this.NumCompleted));

				allowAccess = true; allowReuse = false;
				if (this.NumCompleted > 0) {
					allowReuse = true;
					if (req.ReuseAmount < 0) {
						allowAccess = false;
					} else if (reuses < req.ReuseAmount) {
						allowAccess = false;
						reuses++;
					} else if (req.ReuseTolerance >= 0 && skips >= req.ReuseTolerance) {
						allowReuse = false;
					}

					if (Parameters.ENABLE_VERBOSE_DEBUG_PROCESS_LOGGING) DebugExtensions.DebugWriteline(string.Format("Receiving - {0} {1} {2} Reuse {3} skips {4} reusing allowance {5} + {6} {7} {8}", this.Step.Name, req.Resource.Name, this.NumCompleted, reuses, skips, req.ReuseAmount, req.ReuseTolerance, allowReuse, allowAccess));
				}

				if (allowReuse) {
					if (Parameters.ENABLE_VERBOSE_DEBUG_PROCESS_LOGGING) DebugExtensions.DebugWriteline(string.Format("Receiving - {0} {1} {2} Reusing", this.Step.Name, req.Resource.Name, this.NumCompleted));
					returnSignal.Set();
					skips++;
					if (allowAccess)
						if (Parameters.ENABLE_VERBOSE_DEBUG_PROCESS_LOGGING) DebugExtensions.DebugWriteline(string.Format("Receiving - {0} {1} {2} attempt access", this.Step.Name, req.Resource.Name, this.NumCompleted));
						if (req.Resource.TryDequeue(ref _ingestBuffer[outIdx], TimeSpan.Zero))
							skips = 0;
				} else if (allowAccess) {
					if (Parameters.ENABLE_VERBOSE_DEBUG_PROCESS_LOGGING) DebugExtensions.DebugWriteline(string.Format("Receiving - {0} {1} {2} - start access", this.Step.Name, req.Resource.Name, this.NumCompleted));

					if (!(refreshSignal is null)) {
						if (Parameters.ENABLE_VERBOSE_DEBUG_PROCESS_LOGGING) DebugExtensions.DebugWriteline(string.Format("Receiving - {0} {1} {2} - refresh wait", this.Step.Name, req.Resource.Name, this.NumCompleted));
						refreshSignal.WaitOne();
						if (Parameters.ENABLE_VERBOSE_DEBUG_PROCESS_LOGGING) DebugExtensions.DebugWriteline(string.Format("Receiving - {0} {1} {2} - refresh signaled", this.Step.Name, req.Resource.Name, this.NumCompleted));
					}

					if (Parameters.ENABLE_VERBOSE_DEBUG_PROCESS_LOGGING) DebugExtensions.DebugWriteline(string.Format("Receiving - {0} {1} {2} - access start", this.Step.Name, req.Resource.Name, this.NumCompleted));
					if (req.DoConsume) {
						if (Parameters.ENABLE_VERBOSE_DEBUG_PROCESS_LOGGING) DebugExtensions.DebugWriteline(string.Format("Receiving - {0} {1} {2} - access consume", this.Step.Name, req.Resource.Name, this.NumCompleted));
						if (req.ReadTimeout.HasValue && req.Resource.TryDequeue(ref _ingestBuffer[outIdx], req.ReadTimeout.Value)) {
							if (Parameters.ENABLE_VERBOSE_DEBUG_PROCESS_LOGGING) DebugExtensions.DebugWriteline(string.Format("Receiving - {0} {1} {2} - access consume immediate", this.Step.Name, req.Resource.Name, this.NumCompleted));
							skips = 0;
							reuses = 0;
						} else {
							if (req.AllowDirtyRead) {
								if (Parameters.ENABLE_VERBOSE_DEBUG_PROCESS_LOGGING) DebugExtensions.DebugWriteline(string.Format("Receiving - {0} {1} {2} - access consume dirty", this.Step.Name, req.Resource.Name, this.NumCompleted));
								skips++;
								_ingestBuffer[outIdx] = req.Resource.Current;
							} else {
								if (Parameters.ENABLE_VERBOSE_DEBUG_PROCESS_LOGGING) DebugExtensions.DebugWriteline(string.Format("Receiving - {0} {1} {2} - access consume dequeue", this.Step.Name, req.Resource.Name, this.NumCompleted));
								_ingestBuffer[outIdx] = req.Resource.Dequeue();
								if (Parameters.ENABLE_VERBOSE_DEBUG_PROCESS_LOGGING) DebugExtensions.DebugWriteline(string.Format("Receiving - {0} {1} {2} - access consume dequeue end", this.Step.Name, req.Resource.Name, this.NumCompleted));

								skips = 0;
								reuses = 0;
							}
						}
					} else {
						if (req.ReadTimeout.HasValue && req.Resource.TryPeek(ref _ingestBuffer[outIdx], req.ReadTimeout.Value)) {
							if (Parameters.ENABLE_VERBOSE_DEBUG_PROCESS_LOGGING) DebugExtensions.DebugWriteline(string.Format("Receiving - {0} {1} {2} - access immediate", this.Step.Name, req.Resource.Name, this.NumCompleted));
							skips = 0;
							reuses = 0;
						} else {
							if (req.AllowDirtyRead) {
								if (Parameters.ENABLE_VERBOSE_DEBUG_PROCESS_LOGGING) DebugExtensions.DebugWriteline(string.Format("Receiving - {0} {1} {2} - access dirty", this.Step.Name, req.Resource.Name, this.NumCompleted));
								skips++;
								_ingestBuffer[outIdx] = req.Resource.Current;
							} else {
								if (Parameters.ENABLE_VERBOSE_DEBUG_PROCESS_LOGGING) DebugExtensions.DebugWriteline(string.Format("Receiving - {0} {1} {2} - access peek", this.Step.Name, req.Resource.Name, this.NumCompleted));
								_ingestBuffer[outIdx] = req.Resource.Peek();
								if (Parameters.ENABLE_VERBOSE_DEBUG_PROCESS_LOGGING) DebugExtensions.DebugWriteline(string.Format("Receiving - {0} {1} {2} - access peek end", this.Step.Name, req.Resource.Name, this.NumCompleted));
								skips = 0;
								reuses = 0;
							}
						}

						if (Parameters.ENABLE_VERBOSE_DEBUG_PROCESS_LOGGING) DebugExtensions.DebugWriteline(string.Format("Receiving - {0} {1} {2} - access end", this.Step.Name, req.Resource.Name, this.NumCompleted));
					}

					returnSignal.Set();
				}

				if (Parameters.ENABLE_VERBOSE_DEBUG_PROCESS_LOGGING) DebugExtensions.DebugWriteline(string.Format("Receiving - {0} {1} {2} - end", this.Step.Name, req.Resource.Name, this.NumCompleted));
			}
		}

		private void Process(object info) {
			EventWaitHandle[] signals = null, returnSignals = null;
			if (!(info is null)) {
				Tuple<EventWaitHandle[], EventWaitHandle[]> castedInfo = (Tuple<EventWaitHandle[], EventWaitHandle[]>)info;
				signals = castedInfo.Item1;
				returnSignals = castedInfo.Item2;
			}
			signals ??= Array.Empty<EventWaitHandle>();
			returnSignals ??= Array.Empty<EventWaitHandle>();

			bool waitHolds;
			object result;
			while (this.IsActive) {
				if (Parameters.ENABLE_DEBUG_PROCESS_LOGGING) DebugExtensions.DebugWriteline(string.Format("Process - {0} {1} - start", this.Step.Name, this.NumCompleted));

				this.IsPunctual = false;
				this.IterationStartUtc = DateTime.UtcNow;
				waitHolds = false;

				if (signals.Any()) {
					if (Parameters.ENABLE_DEBUG_PROCESS_LOGGING) DebugExtensions.DebugWriteline(string.Format("Process - {0} {1} - wait", this.Step.Name, this.NumCompleted));
					this.IsPunctual = this.Step.DataLoadingTimeout.HasValue
						? WaitHandle.WaitAll(signals, this.Step.DataLoadingTimeout.Value)
						: WaitHandle.WaitAll(signals);
					if (Parameters.ENABLE_VERBOSE_DEBUG_PROCESS_LOGGING) DebugExtensions.DebugWriteline(string.Format("Process - {0} {1} - signaled {2}", this.Step.Name, this.NumCompleted, this.IsPunctual));
				}

				this.IterationReceiveUtc = DateTime.UtcNow;
				if (!(this.Step.DataAssimilationTicksAverager is null))
					this.Step.DataAssimilationTicksAverager.Update(this.IterationReceiveUtc.Value.Subtract(this.IterationStartUtc.Value).Ticks);

				if (!(this.Step.Synchronizer is null)) {
					if (Parameters.ENABLE_VERBOSE_DEBUG_PROCESS_LOGGING) DebugExtensions.DebugWriteline(string.Format("Process - {0} {1} - synchronize", this.Step.Name, this.NumCompleted));
					this.Step.Synchronizer.Synchronize();
					if (Parameters.ENABLE_VERBOSE_DEBUG_PROCESS_LOGGING) DebugExtensions.DebugWriteline(string.Format("Process - {0} {1} - synchronize end", this.Step.Name, this.NumCompleted));
				}

				this.IterationSyncResumeUtc = DateTime.UtcNow;
				if (!(this.Step.SynchronizationTicksAverager is null))
					this.Step.SynchronizationTicksAverager.Update(this.IterationSyncResumeUtc.Value.Subtract(this.IterationReceiveUtc.Value).Ticks);

				if (this.Step.OutputResource is null) {
					if (Parameters.ENABLE_DEBUG_LOGGING) DebugExtensions.DebugWriteline(string.Format("Process - {0} {1} - evaluating", this.Step.Name, this.NumCompleted));
					this.Step.Evaluator(this._ingestBuffer);

					this.IterationCalcEndUtc = DateTime.UtcNow;
					if (!(this.Step.ExclusiveTimeAverage is null))
						this.Step.ExclusiveTimeAverage.Update(this.IterationCalcEndUtc.Value.Subtract(this.IterationSyncResumeUtc.Value).Ticks);
					if (Parameters.ENABLE_DEBUG_LOGGING) DebugExtensions.DebugWriteline(string.Format("Process - {0} {1} - eval complete", this.Step.Name, this.NumCompleted));
				} else {
					if (Parameters.ENABLE_DEBUG_LOGGING) DebugExtensions.DebugWriteline(string.Format("Process - {0} {1} - calulating", this.Step.Name, this.NumCompleted));
					result = this.Step.Initializer is null ? this.Step.Calculator(this._ingestBuffer) : this.Step.Initializer();

					this.IterationCalcEndUtc = DateTime.UtcNow;
					if (!(this.Step.ExclusiveTimeAverage is null))
						this.Step.ExclusiveTimeAverage.Update(this.IterationCalcEndUtc.Value.Subtract(this.IterationSyncResumeUtc.Value).Ticks);
					if (Parameters.ENABLE_DEBUG_LOGGING) DebugExtensions.DebugWriteline(string.Format("Process - {0} {1} - eval complete", this.Step.Name, this.NumCompleted));

					if (this.Step.OutputSkips < 1 || this.NumCompleted % (this.Step.OutputSkips + 1) == 0) {
						if (this.Step.IsOutputOverwrite)
							this.Step.OutputResource.Overwrite(result);
						else {
							this.Step.OutputResource.Enqueue(result);
							waitHolds = true;
						}
					}
				}

				this.IterationEndUtc = DateTime.UtcNow;
				if (!(this.Step.IterationTicksAverager is null))
					this.Step.IterationTicksAverager.Update(this.IterationEndUtc.Value.Subtract(this.IterationStartUtc.Value).Ticks);
				this.NumCompleted++;

				if (!(this.Step.Callback is null))
					this.Step.Callback(this);
				
				if (waitHolds) {
					if (this.Step.OutputResource.ReleaseListeners.Any()) {
						if (Parameters.ENABLE_DEBUG_PROCESS_LOGGING) DebugExtensions.DebugWriteline(string.Format("Process - {0} {1} - wait after", this.Step.Name, this.NumCompleted));
						WaitHandle.WaitAll(this.Step.OutputResource.ReleaseListeners);
					}
				}

				if (returnSignals.Any())
					foreach (EventWaitHandle sig in returnSignals)
						sig.Set();

				if (Parameters.ENABLE_DEBUG_PROCESS_LOGGING) DebugExtensions.DebugWriteline(string.Format("Process - {0} {1} - end", this.Step.Name, this.NumCompleted - 1));
			}
		}
	}
	#pragma warning restore CS0162 // Unreachable code detected
}