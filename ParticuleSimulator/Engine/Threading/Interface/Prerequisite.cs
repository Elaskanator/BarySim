using System;

namespace ParticleSimulator.Engine {
	public interface IPrerequisite {
		ISynchronousConsumedResource Resource { get; }

		bool DoConsume { get; }
		bool OnChange { get; }
		bool AllowDirtyRead { get; }
		TimeSpan? ReadTimeout { get; }
		int ReuseAmount { get; }
		int ReuseTolerance { get; }

		string ToString() => string.Format("Prerequisite<{0}>[Consume {1} OnChange {2} DirtyRead {3} Timeout {4} Reuses {5} Slip {6}]",
			this.Resource,
			this.DoConsume,
			this.OnChange,
			this.AllowDirtyRead,
			this.ReadTimeout,
			this.ReuseAmount,
			this.ReuseTolerance);
	}

	public struct Prerequisite<T> : IPrerequisite {
		public SynchronousBuffer<T> Resource { get; set; }
		ISynchronousConsumedResource IPrerequisite.Resource => this.Resource;

		public bool DoConsume { get; set; }
		public bool OnChange { get; set; }
		public bool AllowDirtyRead { get; set; }
		public TimeSpan? ReadTimeout { get; set; }
		//negative means unlimited
		public int ReuseAmount { get; set; }
		public int ReuseTolerance { get; set; }
	}
}