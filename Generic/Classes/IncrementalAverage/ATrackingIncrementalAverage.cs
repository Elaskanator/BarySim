using System.Collections.Generic;

namespace Generic.Classes {
	public abstract class ATrackingIncrementalAverage<T> : AIncrementalAverage<T>
	where T : struct {
		public ATrackingIncrementalAverage(int historyLength) { this._history = new T[historyLength]; }

		public int MaxHistoryLength { get { return this._history.Length; } }
		public IEnumerable<T> History { get {
			for (int i = 0; i < this.MaxHistoryLength && i < this.NumUpdates; i++)
				yield return this._history[(i + (this.NumUpdates-1 % this.MaxHistoryLength)) % this.MaxHistoryLength]; }}

		private readonly T[] _history;

		sealed protected override void PreUpdate(T value, double? alpha) {
			this._history[this.NumUpdates % this.MaxHistoryLength] = value;
		}
	}

	public class TrackingIncrementalAverage<T> : AIncrementalAverage<T>
	where T : struct {
		public TrackingIncrementalAverage(AIncrementalAverage<T> averager, int historyLength = 100)
		: base(historyLength) { this._averager = averager; }

		private AIncrementalAverage<T> _averager;

		public override T ComputeNew(T newValue, double alpha) => this._averager.ComputeNew(newValue, alpha);
	}
}