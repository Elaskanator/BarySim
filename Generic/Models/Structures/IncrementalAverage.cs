using System.Collections.Generic;

namespace Generic.Models {
	public abstract class AIncrementalAverage<T> {
		protected T _current = default;
		public virtual T Current { get { return this._current; } }
		public virtual int NumUpdates { get; private set; }
		public T LastUpdate { get; private set; }

		protected virtual double UpdateStrength { get { return 1d / this.NumUpdates; } }

		public AIncrementalAverage() { }
		public AIncrementalAverage(T init) {
			this.Update(init);
		}

		public void Update(T value, double? weighting = null) {
			this.LastUpdate = value;
			this.NumUpdates++;
			this.ApplyUpdate(value, weighting);
		}

		protected virtual void ApplyUpdate(T value, double? weighting) {
			if (this.NumUpdates == 0)
				this._current = value;
			else {
				double
					alpha = (weighting ?? this.UpdateStrength) >= 1d / this.NumUpdates ? (weighting ?? this.UpdateStrength) : 1d / this.NumUpdates,
					beta = 1 - alpha;
				this._current = this.Add(this.Multiply(value, alpha), this.Multiply(this.Current, beta));
			}
		}
		protected abstract T Multiply(T a, double b);
		protected abstract T Add(T a, T b);

		public virtual void Reset() {
			this._current = this.LastUpdate = default;
			this.NumUpdates = 0;
		}

		public override string ToString() { return string.Format("{0}[{1}]", nameof(AIncrementalAverage<T>), this.Current); }
	}
	public class IncrementalAverage : AIncrementalAverage<double> {
		public IncrementalAverage() : base() { }
		public IncrementalAverage(double init) : base(init) { }

		protected override double Multiply(double a, double b) { return a * b; }
		protected override double Add(double a, double b) { return a + b; }
	}

	public class TrackingIncrementalAverage : IncrementalAverage {
		public int MaxHistoryLength { get { return this._history.Length; } }
		private readonly double[] _history;

		public TrackingIncrementalAverage() : base() { }
		public TrackingIncrementalAverage(double init) : base(init) { }

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

		public SampleSMA() : base() { }
		public SampleSMA(double weighting) {
			this.Alpha = weighting;
		}

		public SampleSMA(double weighting, double init)
		: base(init) {
			this.Alpha = weighting;
		}
	}

	public class SampleSMA_Tracking : TrackingIncrementalAverage {
		public readonly double Alpha;
		public double Beta { get { return 1d - this.Alpha; } }

		public SampleSMA_Tracking() : base() { }
		public SampleSMA_Tracking(double init) : base(init) { }

		public SampleSMA_Tracking(double weighting, int historyLength = 100, double? init = null) : base(historyLength, init) {
			this.Alpha = weighting;
		}
		
		protected override double UpdateStrength { get { return this.Alpha; } }
	}
}
