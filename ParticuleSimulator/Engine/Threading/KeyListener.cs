using System;
using Generic.Extensions;

namespace ParticleSimulator.Engine {
	public class KeyListener {
		public KeyListener(ConsoleKey key, string label, Func<bool> getter, Action<bool> setter) {
			this.Key = key;
			this.Label = label;
			this._getter = getter;
			this._setter = setter;
		}

		public ConsoleKey Key { get; private set; }
		public string Label { get; private set; }

		private readonly Func<bool> _getter;
		private readonly Action<bool> _setter;

		public ConsoleColor ForegroundActive = ConsoleColor.Black;
		public ConsoleColor ForegroundInactive = ConsoleColor.Gray;
		public ConsoleColor ForegroundSuspended = ConsoleColor.Gray;

		public ConsoleColor BackgroundActive = ConsoleColor.DarkGreen;
		public ConsoleColor BackgroundInactive = ConsoleColor.Black;
		public ConsoleColor BackgroundSuspended = ConsoleColor.DarkYellow;

		public void Toggle() =>
			this._setter(!this._getter());

		public ConsoleExtensions.CharInfo[] ToConsoleCharString() {
			bool state = this._getter();
			ConsoleColor foreground = state
				? Program.Engine.IsPaused
					? this.ForegroundSuspended
					: this.ForegroundActive
				: this.ForegroundInactive;
			ConsoleColor background = state
				? Program.Engine.IsPaused
					? this.BackgroundSuspended
					: this.BackgroundActive
				: this.BackgroundInactive;

			ConsoleExtensions.CharInfo[] result = new ConsoleExtensions.CharInfo[this.Label.Length];
			for (int i = 0; i < this.Label.Length; i++)
				result[i] = new(this.Label[i], foreground, background);
			return result;
		}
	}
}