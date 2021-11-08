using System;
using System.Collections.Generic;

namespace Generic {
	public class IncrementalAverage {
		protected double? _current = null;
		public virtual double? Current { get { return this._current; } }
		public virtual int NumUpdates { get; private set; }
		public double? LastUpdate { get; private set; }

		protected virtual double UpdateStrength { get { return 1d / this.NumUpdates; } }

		public IncrementalAverage(double? init = null) {
			if (init.HasValue) this.Update(init.Value);
		}

		public void Update(double value, double? weighting = null) {
			this.LastUpdate = value;
			this.ApplyUpdate(value, weighting);
			this.NumUpdates++;
		}

		protected virtual void ApplyUpdate(double value, double? weighting) {
			if (this.NumUpdates == 0)
				this._current = value;
			else {
				double
					alpha = (weighting ?? this.UpdateStrength) >= 1d / this.NumUpdates ? (weighting ?? this.UpdateStrength) : 1d / this.NumUpdates,
					beta = 1 - alpha;
				this._current = (alpha * value) + (beta * this.Current.Value);
			}
		}

		public virtual void Reset() {
			this._current = null;
			this.LastUpdate = null;
			this.NumUpdates = 0;
		}

		public override string ToString() {
			return string.Format("IncAvg[{0}]", this.Current);
		}
	}

	public class TrackingIncrementalAverage : IncrementalAverage {
		public int MaxHistoryLength { get { return this._history.Length; } }
		private readonly double[] _history;

		public IEnumerable<double> History {
			get {
				for (int i = 0; i < this.MaxHistoryLength && i < this.NumUpdates; i++) {
					yield return this._history[(i + (this.NumUpdates % this.MaxHistoryLength)) % this.MaxHistoryLength];
				}
			}
		}

		public TrackingIncrementalAverage(int historyLength = 100, double? init = null) {
			this._history = new double[historyLength];

			if (init.HasValue) this.Update(init.Value);
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

		public SampleSMA(double weighting, double? init = null) {
			this.Alpha = weighting;

			if (init.HasValue) this.Update(init.Value);
		}
	}

	public class SampleSMA_Tracking : TrackingIncrementalAverage {
		public readonly double Alpha;
		public double Beta { get { return 1d - this.Alpha; } }

		public SampleSMA_Tracking(double weighting, int historyLength = 100, double? init = null) : base(historyLength, init) {
			this.Alpha = weighting;
		}
		
		protected override double UpdateStrength { get { return this.Alpha; } }
	}

	public class WeightedIncrementalAverage : IncrementalAverage {
		private double _sum = 0d;
		public override double? Current { get { return this.NumUpdates > 0 ? this._sum / this.NumUpdates : (double?)null; } }
		public double Weight { get; protected set; }

		protected override double UpdateStrength { get { throw new Exception("Update strength is specified in the Update parameter, not by the class"); } }
		
		protected override void ApplyUpdate(double value, double? weight) {
			double w = weight ?? 1d;
			this._sum += value * w;
			this.Weight += w;
		}
		public override void Reset() {
			this._sum = 0;
			this.Weight = 0;
		}
	}
}
