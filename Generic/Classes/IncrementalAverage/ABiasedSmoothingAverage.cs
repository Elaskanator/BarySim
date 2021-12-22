using System;

namespace Generic.Models {
	public abstract class ABiasedSmoothingAverage<T> : ASimpleExponentialMovingAverage<T>
	where T : struct, IComparable<T> {
		public ABiasedSmoothingAverage(T normalUpdateThreshold, double weighting)
		: base(weighting) { this.NormalUpdateThreshold = normalUpdateThreshold; }

		public T NormalUpdateThreshold { get; private set; }
		
		public override T ComputeNew(T newValue, double alpha) {
			if (newValue.CompareTo(this.NormalUpdateThreshold) > 0) return base.ComputeNew(newValue, alpha);
			else {
				double relativeDifference = Math.Pow(
					this.Ratio(
						this.Add(newValue, this.Scale(this.Current, -1d)),
						this.NormalUpdateThreshold),
					2d);//eliminates the sign from the relative difference
				return base.ComputeNew(
					newValue,
					alpha * relativeDifference);
			}
		}

		protected abstract double Ratio(T first, T second);
	}

	public class BiasedSmoothingAverage : ABiasedSmoothingAverage<double> {
		public BiasedSmoothingAverage(double normalUpdateThreshold, double weighting = 1d) : base(normalUpdateThreshold, weighting) { }

		protected override double Add(double first, double second) => first + second;
		protected override double Ratio(double first, double second) => first / second;
		protected override double Scale(double value, double scalar) => value * scalar;
	}

	public class BiasedSmoothingTimeAverage : ABiasedSmoothingAverage<TimeSpan> {
		public BiasedSmoothingTimeAverage(TimeSpan normalUpdateThreshold, double weighting = 1d) : base(normalUpdateThreshold, weighting) { }

		protected override TimeSpan Add(TimeSpan first, TimeSpan second) => first + second;
		protected override TimeSpan Scale(TimeSpan value, double scalar) => value * scalar;
		protected override double Ratio(TimeSpan first, TimeSpan second) => first / second;
	}
}