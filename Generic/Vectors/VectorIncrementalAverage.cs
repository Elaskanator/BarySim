using System.Numerics;
using Generic.Models;

namespace Generic.Vectors {
	public class VectorIncrementalWeightedAverage : AIncrementalAverage<Vector<float>> {
		public float TotalWeight { get; private set; }
		public override Vector<float> Current => this._current * (1f / this.TotalWeight);

		protected override void ApplyUpdate(Vector<float> value, double? weighting) {
			float w = (float)(weighting ?? 1d);
			this._current += value * w;
			this.TotalWeight += w;
		}

		public override void Reset() {
			base.Reset();
			this.TotalWeight = default;
		}
	}
}