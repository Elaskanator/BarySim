using System;
using System.Threading;
using Generic.Models;

namespace ParticleSimulator.Engine {
	public static class DataGatherer {
		public static IDataGatherer New(IPrerequisite config, EventWaitHandle readySignal, EventWaitHandle doneSignal, EventWaitHandle refreshSignal) =>
			(IDataGatherer)Activator.CreateInstance(
				typeof(DataGatherer<>).MakeGenericType(config.Resource.DataType),
				config, readySignal, doneSignal, refreshSignal);
	}

	public class DataGatherer<T> : ACalculationHandler, IDataGatherer {
		public DataGatherer(Prerequisite<T> config, EventWaitHandle readySignal, EventWaitHandle doneSignal, EventWaitHandle refreshSignal)
		: base (readySignal, doneSignal) {
			this.Config = config;
			this._refreshSignal = refreshSignal;
		}

		public T Value => this._myValue;
		object IDataGatherer.Value => this.Value;

		public Prerequisite<T> Config { get; private set; }
		public int Skips { get; private set; }
		public int Reuses { get; private set; }
		
		public override string Name => this.Config.Resource.Name;
		public override TimeSpan? SignalTimeout => null;
		public override TimeSynchronizer Synchronizer => null;

		private readonly EventWaitHandle _refreshSignal;
		private T _myValue;

		protected override void Process(bool punctual) {
			bool allowAccess = true, allowReuse = false, ready;
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

			if (allowReuse) {
				this.Skips++;
				if (allowAccess) {
					ready = true;
					if (!(this._refreshSignal is null))
						ready = this._refreshSignal.WaitOne(TimeSpan.Zero);
					if (ready)
						if (this.Config.DoConsume) {
							if (this.Config.Resource.TryDequeue(ref this._myValue, TimeSpan.Zero)) {
								this.Skips = 0;
								this.Reuses = 0;
							} else if (this.Config.AllowDirtyRead)
								this._myValue = this.Config.Resource.Current;
						} else if (this.Config.AllowDirtyRead)
							this._myValue = this.Config.Resource.Current;
				}
			} else if (allowAccess) {
				if (!(this._refreshSignal is null))
					this._refreshSignal.WaitOne();

				if (this.Config.DoConsume) {
					if (this.Config.ReadTimeout.HasValue && this.Config.Resource.TryDequeue(ref this._myValue, this.Config.ReadTimeout.Value)) {
						this.Skips = 0;
						this.Reuses = 0;
					} else if (this.Config.AllowDirtyRead) {
						this.Skips++;
						this._myValue = this.Config.Resource.Current;
					} else {
						this._myValue = this.Config.Resource.Dequeue();
						this.Skips = 0;
						this.Reuses = 0;
					}
				} else {
					if (this.Config.ReadTimeout.HasValue && this.Config.Resource.TryPeek(ref this._myValue, this.Config.ReadTimeout.Value)) {
						this.Skips = 0;
						this.Reuses = 0;
					} else if (this.Config.AllowDirtyRead) {
						this.Skips++;
						this._myValue = this.Config.Resource.Current;
					} else {
						this._myValue = this.Config.Resource.Peek();
						this.Skips = 0;
						this.Reuses = 0;
					}
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