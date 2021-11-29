using System;
using System.Threading;

namespace Generic.Models {
	public class TimeSynchronizer {
		public const int THREAD_PULSE_MS = 15;//what is the exact value?

		public TimeSynchronizer(TimeSpan? value, TimeSpan? min = null) {
			if (min.HasValue && min.Value.Ticks > 0L) this.Min = min.Value;
			if (value.HasValue && value.Value.Ticks > 0L) this.Interval = value.Value;
		}
		public static TimeSynchronizer FromFps(double? value, double? max = null) {
			return new TimeSynchronizer(
				value.HasValue && value.Value > 0d ? TimeSpan.FromSeconds(1d / value.Value) : null,
				max.HasValue && max.Value > 0d ? TimeSpan.FromSeconds(1d / max.Value) : null);
		}
		
		private DateTime? _targetTimeUtc = null;
		public readonly TimeSpan Min = TimeSpan.Zero;
		public readonly TimeSpan Interval = TimeSpan.Zero;

		public void Synchronize() {
			DateTime nowUtc = DateTime.UtcNow;
			this._targetTimeUtc ??= new DateTime(nowUtc.Year, nowUtc.Month, nowUtc.Day, nowUtc.Hour, nowUtc.Minute, nowUtc.Second, DateTimeKind.Utc);
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

			if (waitDuration.TotalMilliseconds > THREAD_PULSE_MS)
				Thread.Sleep(waitDuration.Subtract(TimeSpan.FromMilliseconds(waitDuration.TotalMilliseconds % THREAD_PULSE_MS)));
		}
	}
}