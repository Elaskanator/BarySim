using System;
using Generic.Extensions;

namespace ParticleSimulator.Engine {
	public class KeyListener {
		public KeyListener(ConsoleKey key, string label, Func<bool> getter, Action<bool> setter, Action resetter = null) {
			this.Key = key;
			this.Label = label;
			this.Getter = getter;
			this.Setter = setter;
			this.Resetter = resetter;
		}

		public ConsoleKey Key { get; private set; }
		public string Label { get; private set; }

		public readonly Func<bool> Getter;
		public readonly Action<bool> Setter;
		public readonly Action Resetter;

		public ConsoleColor ForegroundActive = ConsoleColor.Black;
		public ConsoleColor ForegroundInactive = ConsoleColor.Gray;
		public ConsoleColor ForegroundSuspended = ConsoleColor.Gray;

		public ConsoleColor BackgroundActive = ConsoleColor.DarkGreen;
		public ConsoleColor BackgroundInactive = ConsoleColor.Black;
		public ConsoleColor BackgroundSuspended = ConsoleColor.DarkYellow;

		public void Toggle() =>
			this.Setter(!this.Getter());

		public ConsoleExtensions.CharInfo[] ToConsoleCharString() {
			bool state = this.Getter();
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