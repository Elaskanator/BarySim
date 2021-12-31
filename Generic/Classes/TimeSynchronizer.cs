using System;
using System.Threading;

namespace Generic.Models {
	public class TimeSynchronizer {
		//see https://docs.microsoft.com/en-us/windows/win32/api/synchapi/nf-synchapi-sleep
		public const double THREAD_PULSE_MS = 15;//TODO read exact value for your system

		public TimeSynchronizer(TimeSpan value, bool vSync) {
			this.Target = value;
			this.VSync = vSync;
		}
		public TimeSynchronizer (double fps, bool vSync) {
			this.Target = TimeSpan.FromSeconds(1d / fps);
			this.VSync = vSync;
		}
		
		private DateTime? _targetTimeUtc = null;
		public readonly TimeSpan Target;
		public readonly bool VSync;

		public TimeSpan? LastSyncDuration { get; private set; }

		public void Synchronize() {
			DateTime nowUtc = DateTime.UtcNow;

			this.LastSyncDuration = null;
			//optional rounding down to nearest second:
			this._targetTimeUtc ??= new DateTime(nowUtc.Year, nowUtc.Month, nowUtc.Day, nowUtc.Hour, nowUtc.Minute, nowUtc.Second, DateTimeKind.Utc);
			TimeSpan waitDuration = TimeSpan.Zero;

			if (this.VSync) {
				waitDuration = this._targetTimeUtc.Value - nowUtc;
				if (waitDuration.Ticks >= 0L) {
					this._targetTimeUtc += this.Target;
				} else {
					int slip = (int)Math.Ceiling(-waitDuration / this.Target);
					this._targetTimeUtc += this.Target * slip;
					waitDuration = this._targetTimeUtc.Value - nowUtc;
				}
			} else {
				waitDuration = this._targetTimeUtc.Value - nowUtc;
				if (waitDuration.Ticks > 0L)
					this._targetTimeUtc += this.Target;
				else this._targetTimeUtc = nowUtc + this.Target;//missed it (this does not preserve absolute synchronization and can de-phase from metered interval times)
			}

			if (waitDuration.TotalMilliseconds > THREAD_PULSE_MS/2d) {
				DateTime startUtc = DateTime.UtcNow;
				Thread.Sleep(waitDuration.Subtract(TimeSpan.FromMilliseconds(THREAD_PULSE_MS/2d)));
				this.LastSyncDuration = DateTime.UtcNow.Subtract(startUtc);
			} else this.LastSyncDuration = TimeSpan.Zero;
		}
	}
}