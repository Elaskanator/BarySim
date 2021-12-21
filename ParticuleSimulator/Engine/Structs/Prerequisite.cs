using System;

namespace ParticleSimulator.Engine {
	public struct Prerequisite {
		public SynchronizedDataBuffer Resource;

		public bool DoConsume;
		public bool OnChange;
		public bool AllowDirtyRead;
		public TimeSpan? ReadTimeout;
		//negative means unlimited
		public int ReuseAmount;
		public int ReuseTolerance;

		public override string ToString() {
			return string.Format("{0}<{1}>[Consume {2} OnChange {3} DirtyRead {4} Timeout {5} Reuses {6} Slip {7}]", nameof(Prerequisite), this.Resource.Name,
				this.DoConsume,
				this.OnChange,
				this.AllowDirtyRead,
				this.ReadTimeout,
				this.ReuseAmount,
				this.ReuseTolerance);
		}
	}
}