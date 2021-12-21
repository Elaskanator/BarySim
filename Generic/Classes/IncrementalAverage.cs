using System;
using System.Collections.Generic;

namespace Generic.Models {
	public abstract class AIncrementalAverage<TAvg>
	where TAvg : struct {
		protected TAvg _current = default;
		public virtual TAvg Current { get { return this._current; } }
		public virtual int NumUpdates { get; private set; }
		public TAvg LastUpdate { get; private set; }

		protected virtual double UpdateStrength { get { return 1d / (this.NumUpdates + 1); } }

		public AIncrementalAverage() { }

		public virtual void Update(TAvg value, double? weighting = null) {
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

	public class TrackingIncrementalAverage<TAvg> : AIncrementalAverage<TAvg>
	where TAvg : struct {
		public TrackingIncrementalAverage(AIncrementalAverage<TAvg> averager, int historyLength = 100) {
			this._averager = averager;
			this._history = new TAvg[historyLength];
		}
		private readonly AIncrementalAverage<TAvg> _averager;

		public int MaxHistoryLength { get { return this._history.Length; } }
		private readonly TAvg[] _history;

		public IEnumerable<TAvg> History { get {
			for (int i = 0; i < this.MaxHistoryLength && i < this.NumUpdates; i++)
				yield return this._history[(i + (this.NumUpdates-1 % this.MaxHistoryLength)) % this.MaxHistoryLength];
		} }

		public override void Update(TAvg value, double? weighting = null) {
			_averager.Update(value, weighting);
		}
		protected override void ApplyUpdate(TAvg value, double? weighting) {
			this._history[this.NumUpdates % this.MaxHistoryLength] = value;
		}
	}

	public class IncrementalAverage : AIncrementalAverage<double> {
		public IncrementalAverage() : base() { }

		protected override void ApplyUpdate(double value, double? weighting) {
			double
				alpha = weighting ?? this.UpdateStrength,
				beta = 1d - alpha;
			this._current = alpha * value + this._current * beta;
		}
	}

	public class SimpleExponentialMovingAverage : IncrementalAverage {
		public readonly double Alpha;
		public double Beta { get { return 1d - this.Alpha; } }
		protected override double UpdateStrength =>
			this.Alpha >= base.UpdateStrength
				? this.Alpha
				: base.UpdateStrength;

		public SimpleExponentialMovingAverage(double? weighting) {
			this.Alpha = weighting ?? 1d;
		}
	}

	public class SmoothingIntervalTimeAverage : SimpleExponentialMovingAverage {
		public SmoothingIntervalTimeAverage(double oneSecondDuration, double? weighting)
		: base(weighting) { this.OneSecondDuration = oneSecondDuration; }

		public readonly double OneSecondDuration;

		protected override void ApplyUpdate(double value, double? weighting) {
			if (value > OneSecondDuration) base.ApplyUpdate(value, weighting);
			else base.ApplyUpdate(value, (weighting ?? this.Alpha) * Math.Pow((value - this.Current) / OneSecondDuration, 2d));
		}
	}
}