﻿using System;

namespace ParticleSimulator.Threading {
	public struct Prerequisite {
		public readonly SynchronizedDataBuffer Resource;
		public readonly DataConsumptionType ConsumptionType;
		public readonly int ConsumptionReuse;
		public readonly int ReuseSlipTolerance;
		public readonly bool AllowDirtyRead;
		public readonly TimeSpan? ReadTimeout;

		public Prerequisite(SynchronizedDataBuffer buffer, DataConsumptionType consumptionType, TimeSpan? timeout, bool allowDirty, int consumptionReuse = 1, int reuseSlipTolerance = 0) {
			if (consumptionReuse < 1) throw new ArgumentOutOfRangeException(nameof(consumptionReuse), "Must be strictly positive");
			if (timeout.HasValue && timeout.Value.Ticks < 0) throw new ArgumentOutOfRangeException(nameof(timeout), "Must be nonzero");
			this.Resource = buffer;
			this.ConsumptionType = consumptionType;
			this.AllowDirtyRead = allowDirty;
			this.ReadTimeout = timeout;
			this.ConsumptionReuse = consumptionReuse;
			this.ReuseSlipTolerance = reuseSlipTolerance;
		}
		public Prerequisite(SynchronizedDataBuffer buffer, DataConsumptionType consumptionType, TimeSpan? timeout, int consumptionReuse = 1, int reuseSlipTolerance = 0)
		: this(buffer, consumptionType, timeout, false, consumptionReuse, reuseSlipTolerance) { }
		public Prerequisite(SynchronizedDataBuffer buffer, DataConsumptionType consumptionType, bool allowDirty, int consumptionReuse = 1, int reuseSlipTolerance = 0)
		: this(buffer, consumptionType, null, allowDirty, consumptionReuse, reuseSlipTolerance) { }
		public Prerequisite(SynchronizedDataBuffer buffer, DataConsumptionType consumptionType, int consumptionReuse = 1, int reuseSlipTolerance = 0)
		: this(buffer, consumptionType, null, false, consumptionReuse, reuseSlipTolerance) { }

		public override string ToString() {
			return string.Format("{0}<{1}{2}>{3}", nameof(Prerequisite),
				this.ConsumptionType,
				this.ConsumptionReuse > 1 ? string.Format(" Reuse: {0}", this.ConsumptionReuse) : "",
				this.Resource);
		}
	}
}