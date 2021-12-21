using System;
using System.Linq;
using System.Threading;
using Generic.Extensions;
using Generic.Models;

namespace ParticleSimulator.Engine {
	public class ProcessThread : AHandler, IDisposable {
		public ProcessThread(EvaluationStep config, DataGatherer[] dataGatherers, EventWaitHandle[] readySignals, EventWaitHandle[] doneSignals)
		: base(readySignals, doneSignals) {
			this.Config = config;
			this.dataGatherers = dataGatherers ?? Array.Empty<DataGatherer>();
		}

		public static ProcessThread New(EvaluationStep config) {
			DataGatherer[] dataReceivers = new DataGatherer[config.InputResourceUses.Length];
			AutoResetEvent[] readySignals = new AutoResetEvent[config.InputResourceUses.Length],
				doneSignals = new AutoResetEvent[config.InputResourceUses.Length],
				refreshListeners = new AutoResetEvent[config.InputResourceUses.Length];
			if (!(config.InputResourceUses is null)) {
				Prerequisite req;
				for (int i = 0; i < config.InputResourceUses.Length; i++) {
					req = config.InputResourceUses[i];
					readySignals[i] = new AutoResetEvent(true);
					doneSignals[i] = new AutoResetEvent(req.AllowDirtyRead);
					if (req.OnChange)
						refreshListeners[i] = req.Resource.AddRefreshListener();
					dataReceivers[i] = new DataGatherer(
						req,
						readySignals[i],
						doneSignals[i],
						refreshListeners[i]);
				}
			}
			return new ProcessThread(
				config,
				dataReceivers.Without(s => s == null).ToArray(),
				readySignals.Without(s => s == null).ToArray(),
				doneSignals.Without(s => s == null).ToArray());
		}

		protected override void PreStart() {
			if (!(this.dataGatherers is null))
				for (int i = 0; i < this.dataGatherers.Length; i++)
					this.dataGatherers[i].Start();
		}
		protected override void PreStop() {
			if (!(this.dataGatherers is null))
				for (int i = 0; i < this.dataGatherers.Length; i++)
					this.dataGatherers[i].Stop();
		}

		public EvaluationStep Config { get; private set; }
		
		private static int _globalId = 0;
		public readonly int Id = ++_globalId;
		public override string Name => this.Config.Name;
		public override Action<AHandler> Callback => this.Config.Callback;
		public override TimeSynchronizer Synchronizer => this.Config.Synchronizer;
		public override TimeSpan? SignalTimeout => this.Config.DataLoadingTimeout;

		private DataGatherer[] dataGatherers = null;

		protected override void Process() {
			object[] parameters = this.dataGatherers.Select(x => x.MyValue).ToArray();

			bool waitHolds = false;
			object result;
			if (this.Config.OutputResource is null) {
				this.Config.Evaluator(parameters);
			} else {
				result = this.Config.Initializer is null ? this.Config.Calculator(parameters) : this.Config.Initializer();

				if (this.Config.OutputSkips < 1 || this.IterationCount % (this.Config.OutputSkips + 1) == 0) {
					if (this.Config.IsOutputOverwrite)
						this.Config.OutputResource.Overwrite(result);
					else this.Config.OutputResource.Enqueue(result);

					waitHolds = true;
				}
			}

			if (!(this.Config.Callback is null))
				this.Config.Callback(this);
				
			if (waitHolds)
				if (this.Config.OutputResource.ReleaseListeners.Any())
					WaitHandle.WaitAll(this.Config.OutputResource.ReleaseListeners);
		}
		public override void Dispose(bool fromDispose) {
			if (fromDispose)
				for (int i = 0; this.IsOpen && i < this.dataGatherers.Length; i++)
					this.dataGatherers[i].Dispose(fromDispose);
			base.Dispose(fromDispose);
		}
	}
}