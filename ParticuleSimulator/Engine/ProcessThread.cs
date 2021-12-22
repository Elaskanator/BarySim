using System;
using System.Linq;
using System.Threading;
using Generic.Extensions;
using Generic.Models;

namespace ParticleSimulator.Engine {
	public class ProcessThread : ACaclulationHandler {
		public ProcessThread(EvaluationStep config, IDataGatherer[] dataGatherers, EventWaitHandle[] readySignals, EventWaitHandle[] doneSignals)
		: base(readySignals, doneSignals) {
			this.Config = config;
			this._dataGatherers = dataGatherers ?? Array.Empty<IDataGatherer>();
		}

		public static ProcessThread New(EvaluationStep config) {
			IDataGatherer[] dataReceivers = new IDataGatherer[config.InputResourceUses.Length];
			AutoResetEvent[] readySignals = new AutoResetEvent[config.InputResourceUses.Length],
				doneSignals = new AutoResetEvent[config.InputResourceUses.Length],
				refreshListeners = new AutoResetEvent[config.InputResourceUses.Length];
			if (!(config.InputResourceUses is null)) {
				IPrerequisite req;
				for (int i = 0; i < config.InputResourceUses.Length; i++) {
					req = config.InputResourceUses[i];
					readySignals[i] = new AutoResetEvent(true);
					doneSignals[i] = new AutoResetEvent(req.AllowDirtyRead);
					if (req.OnChange)
						refreshListeners[i] = req.Resource.AddRefreshListener();
					dataReceivers[i] = DataGatherer.New(
						req,
						readySignals[i],
						doneSignals[i],
						refreshListeners[i]);
				}
			}
			return new ProcessThread(
				config,
				dataReceivers.Without(s => s is null).ToArray(),
				readySignals.Without(s => s is null).ToArray(),
				doneSignals.Without(s => s is null).ToArray());
		}

		public EvaluationStep Config { get; private set; }
		
		public override string Name => this.Config.Name;
		public override Action<bool> Callback => this.Config.Callback;
		public override TimeSynchronizer Synchronizer => this.Config.Synchronizer;
		public override TimeSpan? SignalTimeout => this.Config.DataLoadingTimeout;

		private IDataGatherer[] _dataGatherers = null;

		public override void Initialize() { }

		protected override void PreStart() {
			if (!(this._dataGatherers is null))
				for (int i = 0; i < this._dataGatherers.Length; i++)
					this._dataGatherers[i].Start();
		}

		protected override void Process() {
			object[] parameters = this._dataGatherers.Select(x => x.Value).ToArray();

			//bool waitHolds = false;
			object result;
			if (this.Config.OutputResource is null) {
				this.Config.EvaluatorFn(parameters);
			} else {
				result = this.Config.GeneratorFn is null ? this.Config.CalculatorFn(parameters) : this.Config.GeneratorFn();

				if (this.Config.OutputSkips < 1 || this.IterationCount % (this.Config.OutputSkips + 1) == 0) {
					if (this.Config.IsOutputOverwrite)
						this.Config.OutputResource.Overwrite(result);
					else this.Config.OutputResource.Enqueue(result);

					//waitHolds = true;
				}
			}

			if (!(this.Config.Callback is null))
				this.Config.Callback(true);
				
			//if (waitHolds)
			//	if (this.Config.OutputResource.RefreshReleaseListeners.Length > 0)
			//		WaitHandle.WaitAll(this.Config.OutputResource.RefreshReleaseListeners);
		}

		protected override void PreStop() {
			if (!(this._dataGatherers is null))
				for (int i = 0; i < this._dataGatherers.Length; i++)
					this._dataGatherers[i].Stop();
		}

		public override void Dispose(bool fromDispose) {
			if (fromDispose)
				for (int i = 0; this.IsOpen && i < this._dataGatherers.Length; i++)
					this._dataGatherers[i].Dispose(fromDispose);
			base.Dispose(fromDispose);
		}
	}
}