using System;
using System.Threading;

namespace Generic.Models {
	public class TimeSynchronizer {
		public static readonly TimeSpan THREAD_SLEEP_OVERHEAD = TimeSpan.FromMilliseconds(15);

		public TimeSynchronizer(TimeSpan? value, TimeSpan? min = null) {
			if (min.HasValue && min.Value.Ticks > 0L) this.Min = min.Value;
			if (value.HasValue && value.Value.Ticks > 0L) this.Interval = value.Value;
		}
		public static TimeSynchronizer FromFps(double? value, double? max = null) {
			return new TimeSynchronizer(
				value.HasValue && value.Value > 0d ? TimeSpan.FromSeconds(1d / value.Value) : null,
				max.HasValue && max.Value > 0d ? TimeSpan.FromSeconds(1d / max.Value) : null); }
		
		private DateTime? _targetTimeUtc = null;
		public readonly TimeSpan Min = TimeSpan.Zero;
		public readonly TimeSpan Interval = TimeSpan.Zero;

		public void Synchronize() {
			DateTime nowUtc = DateTime.UtcNow;
			this._targetTimeUtc ??= nowUtc;
			TimeSpan waitDuration = TimeSpan.Zero;

			if (this.Interval.Ticks > 0L) {
				waitDuration = this._targetTimeUtc.Value - nowUtc;
				if (waitDuration.Ticks >= 0L) {
					this._targetTimeUtc += this.Interval;
				} else {
					int slip = (int)Math.Ceiling(-waitDuration / this.Interval);
					this._targetTimeUtc += this.Interval * slip;
					waitDuration = this._targetTimeUtc.Value - nowUtc;
				}
			} else if (this.Min.Ticks > 0L) {
				waitDuration = this._targetTimeUtc.Value - nowUtc;
				if (waitDuration.Ticks > 0L)
					this._targetTimeUtc += this.Min;
				else this._targetTimeUtc = nowUtc + this.Min;
			}

			if (waitDuration >= THREAD_SLEEP_OVERHEAD) Thread.Sleep(waitDuration - THREAD_SLEEP_OVERHEAD);
			Generic.DebugExtensions.DebugWriteline("Sync - End");
		}
	}
}