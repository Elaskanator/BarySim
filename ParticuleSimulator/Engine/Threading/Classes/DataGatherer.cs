using System;
using System.Threading;
using Generic.Classes;
using ParticleSimulator.Engine.Threading;

namespace ParticleSimulator.Engine {
	public static class DataGatherer {
		public static IDataGatherer New(IIngestedResource config, AutoResetEvent readySignal, AutoResetEvent doneSignal, AutoResetEvent refreshSignal) =>
			(IDataGatherer)Activator.CreateInstance(
				typeof(DataGatherer<>).MakeGenericType(config.Resource.DataType),
				config, readySignal, doneSignal, refreshSignal);
	}

	public class DataGatherer<T> : ACalculationHandler, IDataGatherer {
		public DataGatherer(IngestedResource<T> config, AutoResetEvent readySignal, AutoResetEvent doneSignal, AutoResetEvent refreshSignal)
		: base (readySignal, doneSignal) {
			this.Config = config;
			this._refreshSignal = refreshSignal;
		}

		public T Value => this._myValue;
		object IDataGatherer.Value => this.Value;

		public IngestedResource<T> Config { get; private set; }
		public int Skips { get; private set; }
		public int Reuses { get; private set; }
		
		public override string Name => this.Config.Resource.Name;
		public override TimeSpan? SignalTimeout => null;
		public override TimeSynchronizer Synchronizer => null;

		private readonly AutoResetEvent _refreshSignal;
		private T _myValue;

		protected override void Init(bool running) {
			this.Skips = 0;
			this.Reuses = 0;
			this._myValue = default;

			if (!(this._refreshSignal is null))
				this._refreshSignal.Reset();
		}

		protected override void Process(EvalResult prepResult) {
			bool allowAccess = true, allowReuse = false;
			if (this.IterationCount > 0) {
				allowReuse = true;
				if (this.Config.ReuseAmount < 0) {
					allowAccess = false;
					this.Reuses++;
				} else if (this.Reuses < this.Config.ReuseAmount) {
					allowAccess = false;
					this.Reuses++;
				} else if (this.Config.ReuseTolerance >= 0 && this.Skips >= this.Config.ReuseTolerance)
					allowReuse = false;
			}

			if (allowAccess) {
				switch (this.Config.ReadType) {
					case ConsumptionType.ConsumeReady:
						if (this.Config.Resource.TryDequeue(ref this._myValue, TimeSpan.Zero)) {
							this.Skips = 0;
							this.Reuses = 0;
						} else this._myValue = this.Config.Resource.Peek();
						break;
					case ConsumptionType.Consume:
						if (allowReuse) {
							if (this.Config.Resource.TryDequeue(ref this._myValue, TimeSpan.Zero)) {
								this.Skips = 0;
								this.Reuses = 0;
							}
						} else {
							if (this.Config.ReadTimeout.HasValue)
								this.Config.Resource.TryDequeue(ref this._myValue, this.Config.ReadTimeout.Value);
							else this._myValue = this.Config.Resource.Dequeue();

							this.Skips = 0;
							this.Reuses = 0;
						}
						break;
					case ConsumptionType.ReadOnChange:
						if (allowReuse) {
							if (this._refreshSignal.WaitOne(TimeSpan.Zero)) {
								this._myValue = this.Config.Resource.Current;
								this.Skips = 0;
								this.Reuses = 0;
							}
						} else {
							if (this.Config.ReadTimeout.HasValue)
								this._refreshSignal.WaitOne(this.Config.ReadTimeout.Value);
							else this._refreshSignal.WaitOne();

							this._myValue = this.Config.Resource.Current;
							this.Skips = 0;
							this.Reuses = 0;
						}
						break;
					case ConsumptionType.ReadReady:
						if (this.Config.ReadTimeout.HasValue)
							this.Config.Resource.TryPeek(ref this._myValue, this.Config.ReadTimeout.Value);
						else this._myValue = this.Config.Resource.Peek();

						this.Skips = 0;
						this.Reuses = 0;
						break;
					case ConsumptionType.ReadImmediate:
						this._myValue = this.Config.Resource.Current;
						this.Skips = 0;
						this.Reuses = 0;
						break;
				}
			}
		}

		public override void Dispose(bool fromDispose) {
			if (fromDispose)
				this._refreshSignal.Dispose();
			base.Dispose(fromDispose);
		}
	}
}