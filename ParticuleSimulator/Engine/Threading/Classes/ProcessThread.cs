using System;
using System.Linq;
using System.Threading;
using Generic.Classes;
using ParticleSimulator.Engine.Threading;

namespace ParticleSimulator.Engine {
	public class ProcessThread : ACalculationHandler {
		public ProcessThread(EvaluationStep config, IDataGatherer[] dataGatherers, AutoResetEvent[] readySignals, AutoResetEvent[] doneSignals)
		: base(readySignals, doneSignals) {
			this.Config = config;
			this._dataGatherers = dataGatherers ?? Array.Empty<IDataGatherer>();
		}

		public static ProcessThread New(EvaluationStep config) {
			int numResources = config.InputResourceUses is null ? 0 : config.InputResourceUses.Length;
			IDataGatherer[] dataReceivers = new IDataGatherer[numResources];
			AutoResetEvent[] readySignals = new AutoResetEvent[numResources],
				doneSignals = new AutoResetEvent[numResources],
				refreshListeners = new AutoResetEvent[numResources];
			IIngestedResource req;
			for (int i = 0; i < numResources; i++) {
				req = config.InputResourceUses[i];
				readySignals[i] = new AutoResetEvent(true);
				doneSignals[i] = new AutoResetEvent(false);
				if (req.ReadType == ConsumptionType.ReadOnChange)
					refreshListeners[i] = req.Resource.AddRefreshListener();
				dataReceivers[i] = DataGatherer.New(
					req,
					readySignals[i],
					doneSignals[i],
					refreshListeners[i]);
			}
			ProcessThread result = new ProcessThread(
				config,
				dataReceivers,
				doneSignals,
				readySignals);

			return result;
		}

		public EvaluationStep Config { get; private set; }
		
		public override string Name => this.Config.Name;
		public override Action<EvalResult> Callback => this.Config.CallbackFn;
		public override TimeSynchronizer Synchronizer => this.Config.Synchronizer;
		public override TimeSpan? SignalTimeout => this.Config.DataLoadingTimeout;

		private IDataGatherer[] _dataGatherers = null;
		private object[] _parameters;
		private object _result;

		protected override void Init(bool running) {
			if (!(this.Config.InitFn is null))
				this.Config.InitFn();

			if (!(this._dataGatherers is null))
				for (int i = 0; i < this._dataGatherers.Length; i++)
					this._dataGatherers[i].Start();
		}
		
		protected override void PreProcess(EvalResult prepResult) {
			this._parameters = this._dataGatherers.Select(x => x.Value).ToArray();
		}

		protected override void Process(EvalResult prepResult) {
			if (this.Config.OutputResource is null)
				this.Config.EvaluatorFn(prepResult, this._parameters);
			else this._result = this.Config.GeneratorFn is null
					? this.Config.CalculatorFn(prepResult, this._parameters)
					: this.Config.GeneratorFn();
		}

		protected override void PostProcess(EvalResult result) {
			//bool waitHolds = false;
			if (!(this.Config.OutputResource is null) && (this.Config.OutputSkips < 1 || this.IterationCount % (this.Config.OutputSkips + 1) == 0)) {
				if (this.Config.IsOutputOverwrite)
					this.Config.OutputResource.Overwrite(this._result);
				else this.Config.OutputResource.Enqueue(this._result);

				//waitHolds = true;
			}
			//if (waitHolds)
			//	if (this.Config.OutputResource.RefreshReleaseListeners.Length > 0)
			//		WaitHandle.WaitAll(this.Config.OutputResource.RefreshReleaseListeners);
		}

		protected override void Shutdown() {
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