using System;

namespace Generic.Models {
	public abstract class ASimpleExponentialMovingAverage<T> : AIncrementalAverage<T>
	where T : struct {
		public ASimpleExponentialMovingAverage(double weighting) { this.Alpha = weighting; }

		public double Alpha { get; private set; }
		protected override double? Weighting =>
			this.Alpha >= 1d / (this.NumUpdates + 1d)
				? this.Alpha
				: 1d / (this.NumUpdates + 1d);

		public override T ComputeNew(T newValue, double alpha) =>
			this.Add(this.Scale(this.Current, alpha), this.Scale(this.Current, 1d - alpha));

		protected abstract T Add(T first, T second);
		protected abstract T Scale(T value, double scalar);
	}

	public class SimpleExponentialMovingAverage : ASimpleExponentialMovingAverage<double> {
		public SimpleExponentialMovingAverage(double weighting = 1) : base(weighting) { }

		protected override double Add(double first, double second) => first + second;
		protected override double Scale(double value, double scalar) => value * scalar;
	}

	public class SimpleExponentialMovingTimeAverage : ASimpleExponentialMovingAverage<TimeSpan> {
		public SimpleExponentialMovingTimeAverage(double weighting = 1) : base(weighting) { }

		protected override TimeSpan Add(TimeSpan first, TimeSpan second) => first + second;
		protected override TimeSpan Scale(TimeSpan value, double scalar) => value * scalar;
	}
}