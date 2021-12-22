using System;

namespace Generic.Models {
	public abstract class AIncrementalAverage<T>
	where T : struct {
		public AIncrementalAverage() { }
		public AIncrementalAverage(double weighting = 1d) { this._weighting = weighting; }
		public override string ToString() => string.Format("{0}[{1}]", nameof(AIncrementalAverage<T>), this.Current);

		public virtual T Current { get; private set; }
		public T LastUpdate { get; private set; }
		public int NumUpdates { get; private set; }

		protected double? _weighting;
		protected virtual double? Weighting => this._weighting ?? 1d / (this.NumUpdates + 1);

		public T Update(T value, double? weighting = null) {
			this.LastUpdate = value;
			this.PreUpdate(value, weighting);
			this.Current = this.ComputeNew(value, weighting ?? this.Weighting ?? 1d);
			this.NumUpdates++;
			return this.Current;
		}
		public abstract T ComputeNew(T newValue, double alpha);

		public virtual void Reset() {
			this.Current = this.LastUpdate = default;
			this.NumUpdates = 0;
		}

		protected virtual void PreUpdate(T value, double? alpha) { }
	}

	public class IncrementalAverage : AIncrementalAverage<double> {
		public override double ComputeNew(double newValue, double alpha) {
			return alpha * newValue + this.Current * (1d - alpha);
		}
	}

	public class IncrementalTimeAverage : AIncrementalAverage<TimeSpan> {
		public override TimeSpan ComputeNew(TimeSpan newValue, double alpha) {
			return alpha * newValue + this.Current * (1d - alpha);
		}
	}
}