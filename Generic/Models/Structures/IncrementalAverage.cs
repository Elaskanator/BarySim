using System.Collections.Generic;

namespace Generic.Models {
	public abstract class AIncrementalAverage<TAvg>
	where TAvg : struct {
		protected TAvg _current = default;
		public virtual TAvg Current { get { return this._current; } }
		public virtual int NumUpdates { get; private set; }
		public TAvg LastUpdate { get; private set; }

		protected virtual double UpdateStrength { get { return 1d / this.NumUpdates; } }

		public AIncrementalAverage() { }

		public void Update(TAvg value, double? weighting = null) {
			this.LastUpdate = value;
			this.ApplyUpdate(value, weighting);
			this.NumUpdates++;
		}

		protected abstract void ApplyUpdate(TAvg value, double? weighting);

		public virtual void Reset() {
			this._current = this.LastUpdate = default;
			this.NumUpdates = 0;
		}

		public override string ToString() { return string.Format("{0}[{1}]", nameof(AIncrementalAverage<TAvg>), this.Current); }
	}

	public class IncrementalAverage : AIncrementalAverage<double> {
		public IncrementalAverage() : base() { }

		protected override void ApplyUpdate(double value, double? weighting) {
			if (this.NumUpdates == 0) {
				this._current = value;
			} else {
				double
					alpha = weighting is null
						? 1d
						: (weighting ?? this.UpdateStrength) >= 1d / this.NumUpdates ? (weighting ?? this.UpdateStrength) : 1d / this.NumUpdates,
					beta = 1d - alpha;
				this._current = alpha * value + this._current * beta;
			}
		}
	}

	public class TrackingIncrementalAverage : IncrementalAverage {
		public int MaxHistoryLength { get { return this._history.Length; } }
		private readonly double[] _history;

		public IEnumerable<double> History { get {
			for (int i = 0; i < this.MaxHistoryLength && i < this.NumUpdates; i++)
				yield return this._history[(i + (this.NumUpdates-1 % this.MaxHistoryLength)) % this.MaxHistoryLength];
		} }

		public TrackingIncrementalAverage(int historyLength = 100) {
			this._history = new double[historyLength];
		}

		protected override void ApplyUpdate(double value, double? weighting) {
			base.ApplyUpdate(value, weighting);
			this._history[this.NumUpdates % this.MaxHistoryLength] = value;
		}
	}

	public class SampleSMA : IncrementalAverage {
		public readonly double Alpha;
		public double Beta { get { return 1d - this.Alpha; } }
		protected override double UpdateStrength { get { return this.Alpha; } }

		public SampleSMA(double weighting) {
			this.Alpha = weighting;
		}
	}

	public class SampleSMA_Tracking : TrackingIncrementalAverage {
		public readonly double Alpha;
		public double Beta { get { return 1d - this.Alpha; } }

		public SampleSMA_Tracking(double weighting, int historyLength = 100) : base(historyLength) {
			this.Alpha = weighting;
		}
		
		protected override double UpdateStrength { get { return this.Alpha; } }
	}
}
