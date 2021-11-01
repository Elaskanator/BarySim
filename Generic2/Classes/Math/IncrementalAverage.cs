using System;
using System.Collections.Generic;

namespace Generic {
	public abstract class AIncrementalAverage {
		public virtual double? Current { get; protected set; }
		public virtual double NumUpdates { get; protected set; }
		public double? LastUpdate { get; protected set; }

		public AIncrementalAverage(double? init = null) {
			if (init.HasValue) this.Update(init.Value);
		}

		protected virtual double UpdateStrength { get { return 1d / this.NumUpdates; } }

		public virtual void Update(double value) {
			if (this.NumUpdates++ == 0) {
				this.Current = value;
			} else {
				double
					alpha = this.UpdateStrength > 1d / this.NumUpdates ? this.UpdateStrength : 1d / this.NumUpdates,
					beta = 1 - alpha;
				this.Current = (alpha * value) + (beta * this.Current.Value);
			}

			this.LastUpdate = value;
		}

		public virtual void Reset(int? init = null) {
			this.Current = null;
			this.LastUpdate = null;
			this.NumUpdates = 0;

			if (init.HasValue) this.Update(init.Value);
		}

		public override string ToString() {
			return string.Format("IncAvg[{0}]", this.Current);
		}
	}

	public class IncrementalAverage : AIncrementalAverage {
		public IncrementalAverage(double? init = null)
		: base(init) { }
	}

	public class WeightedIncrementalAverage : AIncrementalAverage {
		private double _sum = 0d;
		public override double? Current { get { return this.NumUpdates > 0 ? this._sum / this.NumUpdates : (double?)null; } }
		public override double NumUpdates { get; protected set; }

		public WeightedIncrementalAverage() : base(null) { }

		protected override double UpdateStrength { get { throw new Exception("Update strength is specified in the Update parameter, not by the class"); } }
		
		public virtual void Update(double value, double weight) {
			this._sum += value * weight;
			this.NumUpdates += weight;
			this.LastUpdate = value;
		}
		public override void Update(double value) {
			this.Update(value, 1);
		}
		public override void Reset(int? init = null) {
			this._sum = 0;
			this.NumUpdates = 0;
		}
	}
	public class WeightedTrackingIncrementalAverage : WeightedIncrementalAverage {
		public int HistoryLength { get; set; }

		public readonly List<double> History;

		public WeightedTrackingIncrementalAverage(int historyLength = 100) : base() {
			this.HistoryLength = historyLength;
			this.History = new List<double>();
		}

		protected override double UpdateStrength { get { throw new Exception("Update strength is specified in the Update parameter, not by the class"); } }
		
		public override void Update(double value, double weight) {
			base.Update(value, weight);

			if (this.History.Count >= this.HistoryLength - 1) {
				this.History.RemoveAt(0);
			}

			this.History.Add(value);
		}
		public override void Reset(int? init = null) {
			base.Reset();
			this.History.Clear();

			base.Reset(init);
		}
	}

	public class TrackingIncrementalAverage : AIncrementalAverage {
		public int HistoryLength { get; set; }

		public readonly List<double> History;

		public TrackingIncrementalAverage(double? init = null, int historyLength = 100)
		: base(init) {
			this.HistoryLength = historyLength;
			this.History = new List<double>();

			if (init.HasValue) this.Update(init.Value);
		}

		public override void Update(double value) {
			base.Update(value);
			
			if (this.History.Count >= this.HistoryLength - 1) {
				this.History.RemoveAt(0);
			}

			this.History.Add(value);
		}

		public override void Reset(int? init = null) {
			this.History.Clear();

			base.Reset(init);
		}
	}

	public class SampleSMA : AIncrementalAverage {
		public readonly double Alpha;
		public double Beta { get { return 1d - this.Alpha; } }

		public SampleSMA(double weighting, double? init = null)
		: base(init) {
			this.Alpha = weighting;
		}

		protected override double UpdateStrength { get { return this.Alpha; } }
	}

	public class SampleSMA_Tracking : TrackingIncrementalAverage {
		public readonly double Alpha;
		public double Beta { get { return 1d - this.Alpha; } }

		public SampleSMA_Tracking(double weighting, double? init = null, int historyLength = 100)
		: base(init, historyLength) {
			this.Alpha = weighting;
		}
		
		protected override double UpdateStrength { get { return this.Alpha; } }
	}
}
