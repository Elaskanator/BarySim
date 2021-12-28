using System;
using Generic.Extensions;

namespace ParticleSimulator.Engine.Interaction {
	public class KeyListener {
		public KeyListener(ConsoleKey key, string label, Func<bool> getter, Action<bool> setter) {
			this.Key = key;
			this.Label = label;
			this.Getter = getter;
			this.Setter = setter;
		}

		public ConsoleKey Key { get; private set; }
		public string Label { get; private set; }
		public bool State => this.Getter();

		public Func<bool> Getter { get; private set; }
		public Action<bool> Setter { get; private set; }

		public ConsoleColor? ForegroundShared { get; set; }
		public ConsoleColor? ForegroundActive { get; set; }
		public ConsoleColor? ForegroundInactive { get; set; }

		public ConsoleColor? BackgroundShared { get; set; }
		public ConsoleColor? BackgroundActive { get; set; }
		public ConsoleColor? BackgroundInactive { get; set; }

		public void Toggle() =>
			this.Setter(!this.Getter());

		public ConsoleExtensions.CharInfo[] ToConsoleCharString() {
			ConsoleColor foreground = this.ForegroundShared ?? (this.State ? this.ForegroundActive : this.ForegroundInactive).Value;
			ConsoleColor background = this.BackgroundShared ?? (this.State ? this.BackgroundActive : this.BackgroundInactive).Value;

			ConsoleExtensions.CharInfo[] result = new ConsoleExtensions.CharInfo[this.Label.Length];
			for (int i = 0; i < this.Label.Length; i++)
				result[i] = new(this.Label[i], foreground, background);
			return result;
		}
	}
}