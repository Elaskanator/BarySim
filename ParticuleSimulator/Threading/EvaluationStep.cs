﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Generic.Extensions;
using Generic.Models;

namespace ParticleSimulator.Threading {
	public abstract class AEvaluationStep :IEquatable<AEvaluationStep>, IEqualityComparer<AEvaluationStep>, IDisposable {
		private static int _id = 0;
		public int ID { get; private set; }
		public string Name { get; set; }

		public Prerequisite[] Prerequisites { get; private set; }
		public bool IsActive { get; internal set; }
		public int IterationCount { get; private set; }
		public DateTime? LastIerationEndUtc { get; private set; }
		public SampleSMA LatencyTimings_Ticks { get; private set; }
		public SampleSMA IterationTimings_Ticks { get; private set; }
		public SampleSMA CalculationTimings_Ticks { get; protected set; }
		public bool DoTrackLatency {
			get { return this._trackLatency; }
			set { this._trackLatency = value; this._timer.Reset(); this._timerCalc.Reset(); } }
		
		private TimeSynchronizer _timeSynchronizer;
		private Action _callback;
		private bool _trackLatency = Parameters.PERF_ENABLE;
		private List<Thread> _threads;
		private AutoResetEvent[] _refreshListeners;
		private object[] _buffer;
		private AutoResetEvent _event_gatherData;
		private EventWaitHandle _event_processData;
		private Stopwatch _timer = new();
		protected Stopwatch _timerCalc = new();

		protected AEvaluationStep(TimeSynchronizer sync, Action callback, params Prerequisite[] prerequisites) {
			this.ID = _id++;
			this.IterationCount = 0;
			this.IsActive = false;
			this.Prerequisites = prerequisites ?? Array.Empty<Prerequisite>();
			
			this._timeSynchronizer = sync;
			this._callback = callback;
			this._threads = new List<Thread>();
			this._threads.Add(new Thread(this.Calculate));
			this._event_gatherData = new AutoResetEvent(true);
			this._refreshListeners = new AutoResetEvent[this.Prerequisites.Length];
			this.CalculationTimings_Ticks = new(Parameters.PERF_SMA_ALPHA);
			this.LatencyTimings_Ticks = new(Parameters.PERF_SMA_ALPHA);
			this.IterationTimings_Ticks = new(Parameters.PERF_SMA_ALPHA);

			if (!(this.Prerequisites is null) && this.Prerequisites.Length > 0) {
				this._threads.Add(new Thread(this.AssimilateInput));
				this._event_processData = new EventWaitHandle(false, EventResetMode.AutoReset);
				AutoResetEvent signal;
				for (int pIdx = 0; pIdx < this.Prerequisites.Length; pIdx++) {
					if (this.Prerequisites[pIdx].ConsumptionType == DataConsumptionType.OnUpdate) {
						signal = new AutoResetEvent(false);
						this._refreshListeners[pIdx] = signal;
						this.Prerequisites[pIdx].Resource.RefreshListeners.Add(signal);
					}
				}
			} else this._event_processData = new EventWaitHandle(true, EventResetMode.ManualReset);
		}
		protected AEvaluationStep(TimeSynchronizer sync, params Prerequisite[] prerequisites) : this(sync, null, prerequisites) { }
		protected AEvaluationStep(Action callback, params Prerequisite[] prerequisites) : this(null, callback, prerequisites) { }
		protected AEvaluationStep(params Prerequisite[] prerequisites) : this(null, null, prerequisites) { }

		public void Start() {
			this.IsActive = true;

			foreach (Thread t in this._threads) t.Start();
		}
		public void Dispose() {
			this.IsActive = false;
			foreach (Prerequisite prereq in this.Prerequisites) prereq.Resource.Dispose();
			if (!(this._threads is null)) Parallel.ForEach(this._threads, t => t.Join(0));
			GC.SuppressFinalize(this);
		}

		public void AssimilateInput() {
			object[] buffer = new object[this.Prerequisites.Length];
			bool success;
			Prerequisite req;
			int[] skips = new int[this.Prerequisites.Length];
			while (this.IsActive) {
				if (!(this._timeSynchronizer is null)) this._timeSynchronizer.Synchronize();
				this._event_gatherData.WaitOne();
				if (!this.IsActive) return;

				for (int pIdx = 0; pIdx < this.Prerequisites.Length; pIdx++) {
					req = this.Prerequisites[pIdx];

					if (req.ConsumptionReuse <= 0 || this.IterationCount % req.ConsumptionReuse == 0) {
						switch (req.ConsumptionType) {
							case DataConsumptionType.Consume:
								if (req.ReadTimeout.HasValue || req.ReuseSlipTolerance != 0) {
									if (req.ReadTimeout.HasValue) {
										success = req.Resource.TryDequeue(false, out buffer[pIdx], req.ReadTimeout.Value);
										if (!this.IsActive) return;
									} else success = req.Resource.TryDequeue(false, out buffer[pIdx]);

									if (success) {
										skips[pIdx] = 0;
									} else {
										skips[pIdx]++;
										if (req.AllowDirtyRead)
											buffer[pIdx] = req.Resource.Current;
										else if (req.ReuseSlipTolerance > 0 && skips[pIdx] > req.ReuseSlipTolerance) {
											buffer[pIdx] = req.Resource.Dequeue(false);
											skips[pIdx] = 0;
										} else buffer[pIdx] = req.Resource.Peek();
									}
								} else buffer[pIdx] = req.Resource.Dequeue(false);
								break;
							case DataConsumptionType.OnUpdate:
								if (req.ReadTimeout.HasValue)
									WaitHandle.WaitAny(new[] { this._refreshListeners[pIdx] }, req.ReadTimeout.Value);
								else this._refreshListeners[pIdx].WaitOne();

								if (!this.IsActive) return;

								if (req.AllowDirtyRead)
									buffer[pIdx] = req.Resource.Current;
								else buffer[pIdx] = req.Resource.Peek();

								break;
							case DataConsumptionType.Read:
								if (req.AllowDirtyRead)
									buffer[pIdx] = req.Resource.Current;
								else buffer[pIdx] = req.Resource.Peek();

								break;
							default:
								throw new InvalidEnumArgumentException(
									nameof(Prerequisite.ConsumptionType),
									(int)req.ConsumptionType,
									typeof(DataConsumptionType));
						}
					}
					if (req.ConsumptionType == DataConsumptionType.Consume && req.ConsumptionReuse > 0 && (this.IterationCount + 1) % req.ConsumptionReuse == 0)
						req.Resource.AllowResume();
				}
				_buffer = buffer;
				this._event_processData.Set();
			}
		}

		public void Calculate() {
			while (this.IsActive) {
				if (this.DoTrackLatency) this._timer.Start();
				this._event_processData.WaitOne();
				if (this.DoTrackLatency) {
					this._timer.Stop();
					this.LatencyTimings_Ticks.Update(this._timer.ElapsedTicks);
				}
				if (!this.IsActive) return;

				if (this.Prerequisites.Length == 0 && !(this._timeSynchronizer is null))
					this._timeSynchronizer.Synchronize();
				if (this.DoTrackLatency) this._timer.Start();
				this.Refresh(this._buffer);

				this.IterationCount++;
				this.LastIerationEndUtc = DateTime.UtcNow;
				if (this.DoTrackLatency) {
					this._timer.Stop();
					this.IterationTimings_Ticks.Update(_timer.ElapsedTicks);
					this._timer.Reset();
				}

				if (!(this._callback is null)) this._callback();
				this._event_gatherData.Set();
			}
		}

		protected abstract void Refresh(object[] data);

		public override string ToString() {
			return string.Format("{0}{1}<{2}>{3}", nameof(AEvaluationStep),
				this.Name is null ? "" : string.Format("<{0}>", this.Name),
				this.Prerequisites.Length.Pluralize("prerequisite"),
				this.Prerequisites.Length == 0 ? "" : "[" + string.Join(", ", this.Prerequisites.AsEnumerable()) + "]");//string.Join ambiguous without AsEnumerable()
		}

		public override bool Equals(object obj) { return (obj is AEvaluationStep) && this.ID == (obj as AEvaluationStep).ID; }
		public bool Equals(AEvaluationStep other) { return this.ID == other.ID; }
		public bool Equals(AEvaluationStep x, AEvaluationStep y) { return x.ID == y.ID; }
		public override int GetHashCode() { return base.GetHashCode(); }
		public int GetHashCode(AEvaluationStep obj) { return obj.ID.GetHashCode(); }
	}
	public class NonOutputtingEvaluationStep : AEvaluationStep {
		private readonly Action<object[]> _evaluator;

		public NonOutputtingEvaluationStep(Action<object[]> evaluator, TimeSynchronizer sync, Action callback, params Prerequisite[] prerequisites)
		: base(sync, callback, prerequisites) {
			this._evaluator = evaluator;
		}
		public NonOutputtingEvaluationStep(Action<object[]> evaluator, TimeSynchronizer sync, params Prerequisite[] prerequisites)
		: this(evaluator, sync, null, prerequisites) { }
		public NonOutputtingEvaluationStep(Action<object[]> evaluator, Action callback, params Prerequisite[] prerequisites)
		: this(evaluator, null, callback, prerequisites) { }
		public NonOutputtingEvaluationStep(Action<object[]> evaluator, params Prerequisite[] prerequisites)
		: this(evaluator, null, null, prerequisites) { }
		
		protected override void Refresh(object[] data) {
			if (this.DoTrackLatency) this._timerCalc.Start();
			this._evaluator(data);
			if (this.DoTrackLatency) {
				this._timerCalc.Stop();
				this.CalculationTimings_Ticks.Update(this._timerCalc.ElapsedTicks);
				this._timerCalc.Reset();
			}
		}
	}
	public class EvaluationStep : AEvaluationStep {
		public int SubsamplingOut { get; private set; }
		public SynchronizedDataBuffer OutputResource { get; private set; }
		public bool IsOutputOverwrite { get; private set; }
		private readonly Func<object[], object> _evaluator;
		
		public EvaluationStep(SynchronizedDataBuffer outputResource, bool isOverwrite, int subsamplingOut, Func<object[], object> calculator,
		Action callback, params Prerequisite[] prerequisites) : base(callback, prerequisites) {
			if (subsamplingOut < 1) throw new ArgumentOutOfRangeException(nameof(subsamplingOut), "Must be strictly positive");
			this.SubsamplingOut = subsamplingOut;
			this.IsOutputOverwrite = isOverwrite;
			this._evaluator = calculator;
			this.OutputResource = outputResource;
		}
		public EvaluationStep(SynchronizedDataBuffer outputResource, bool isOverwrite, int subsamplingOut, Func<object[], object> calculator,
		params Prerequisite[] prerequisites) : this(outputResource, isOverwrite, subsamplingOut, calculator, null, prerequisites) { }

		protected override void Refresh(object[] data) {
			if (this.DoTrackLatency) this._timerCalc.Start();
			object results = this._evaluator(data);
			if (this.DoTrackLatency) {
				this._timerCalc.Stop();
				this.CalculationTimings_Ticks.Update(this._timerCalc.ElapsedTicks);
				this._timerCalc.Reset();
			}

			if (this.IterationCount % this.SubsamplingOut == 0) {
				if (this.IsOutputOverwrite) this.OutputResource.Overwrite(results);
				else this.OutputResource.Enqueue(results);
			}
		}
	}
}