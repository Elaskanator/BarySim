using System.Numerics;
using Generic.Models;

namespace Generic.Vectors {
	public class VectorIncrementalAverage : AIncrementalAverage<Vector<float>> {
		public override Vector<float> ComputeNew(Vector<float> newValue, double alpha) =>
			((float)alpha * newValue) + ((1f - (float)alpha) * this.Current);
	}

	public class VectorSmoothedIncrementalAverage : ASimpleExponentialMovingAverage<Vector<float>> {
		public VectorSmoothedIncrementalAverage(float weighting)
		: base(weighting) { }

		protected override Vector<float> Add(Vector<float> first, Vector<float> second) =>
			first + second;

		protected override Vector<float> Scale(Vector<float> value, double scalar) =>
			(float)scalar * value;
	}

	public class VectorIncrementalWeightedAverage : AIncrementalAverage<Vector<float>> {
		public override Vector<float> Current => this._runningTotal * (1f / this.TotalWeight);
		public float TotalWeight { get; private set; }

		private Vector<float> _runningTotal = Vector<float>.Zero;

		public override void Reset() {
			base.Reset();
			this.TotalWeight = 0f;
			this._runningTotal = Vector<float>.Zero;
		}

		public override Vector<float> ComputeNew(Vector<float> newValue, double alpha) {
			float weight = (float)alpha;
			this.TotalWeight += weight;
			return this.Current + (newValue * weight);
		}
	}
}