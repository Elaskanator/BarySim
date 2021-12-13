using System;

namespace ParticleSimulator.Threading {
	public struct Prerequisite {
		public SynchronizedDataBuffer Resource;

		public bool DoConsume;
		public bool OnChange;
		public bool DoHold;
		public bool AllowDirtyRead;
		public TimeSpan? ReadTimeout;
		//negative means unlimited
		public int ReuseAmount;
		public int ReuseTolerance;

		public override string ToString() {
			return string.Format("{0}<{1}>[{2} {3} {4} {5} {6} {7} {8}]", nameof(Prerequisite), this.Resource.Name,
				this.DoConsume,
				this.OnChange,
				this.DoHold,
				this.AllowDirtyRead,
				this.ReadTimeout,
				this.ReuseAmount,
				this.ReuseTolerance);
		}
	}
}