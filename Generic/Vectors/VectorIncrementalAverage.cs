using Generic.Models;

namespace Generic.Vectors {
	public class VectorIncrementalAverage : AIncrementalAverage<double[]> {
		protected override double[] Multiply(double[] a, double b) { return a.Multiply(b); }
		protected override double[] Add(double[] a, double[] b) { return a.Add(b); }
	}
	public class VectorIncrementalWeightedAverage : VectorIncrementalAverage {
		public double TotalWeight { get; private set; }
		public override double[] Current => this._current.Divide(this.TotalWeight);

		protected override void ApplyUpdate(double[] value, double? weighting) {
			this._current ??= new double[value.Length];
			this._current = this._current.Add(value.Multiply(weighting ?? 1d));
			this.TotalWeight += weighting ?? 1d;
		}

		public override void Reset() {
			base.Reset();
			this.TotalWeight = 0d;
		}
	}
}