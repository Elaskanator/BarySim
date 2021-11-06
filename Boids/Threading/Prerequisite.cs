using System;

namespace Boids {
	public struct Prerequisite {
		public readonly SynchronizedDataBuffer Resource;
		public readonly DataConsumptionType ConsumptionType;
		public readonly int ConsumptionReuse;
		public readonly TimeSpan? ReadTimeout;

		public Prerequisite(SynchronizedDataBuffer buffer, DataConsumptionType consumptionType, TimeSpan? timeout, int consumptionReuse = 1) {
			if (consumptionReuse < 1) throw new ArgumentOutOfRangeException(nameof(consumptionReuse), "Must be strictly positive");
			if (timeout.HasValue && timeout.Value.Ticks <= 0) throw new ArgumentOutOfRangeException(nameof(timeout), "Must be strictly positive");
			this.Resource = buffer;
			this.ConsumptionType = consumptionType;
			this.ReadTimeout = timeout;
			this.ConsumptionReuse = consumptionReuse;
		}
		public Prerequisite(SynchronizedDataBuffer buffer, DataConsumptionType consumptionType, int consumptionReuse = 1)
		: this(buffer, consumptionType, null, consumptionReuse) { }

		public override string ToString() {
			return string.Format("{0}<{1}{2}>{3}", nameof(Prerequisite),
				this.ConsumptionType,
				this.ConsumptionReuse > 1 ? string.Format(" Reuse: {0}", this.ConsumptionReuse) : "",
				this.Resource);
		}
	}
}