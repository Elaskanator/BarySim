using System;
using System.Collections.Generic;
using Generic.Extensions;

namespace ParticleSimulator.Engine;

public class KeyListener {
	public KeyListener(ConsoleKey key, string label, Func<bool> getter, Action<bool> setter, Action resetter = null, Func<bool> suspendStateGetter = null) {
		this.Key = key;
		this.Label = label;
		this.Getter = getter;
		this.Setter = setter;
		this.Resetter = resetter;//bugged still
		this.SuspendStateGetter = suspendStateGetter;
	}

	public ConsoleKey Key { get; private set; }
	public string Label { get; private set; }

	public bool IsToggle = true;
	public readonly Func<bool> Getter;
	public readonly Action<bool> Setter;
	public readonly Func<bool> SuspendStateGetter;
	public readonly Action Resetter;

	public ConsoleColor ForegroundActive = ConsoleColor.Black;
	public ConsoleColor ForegroundInactive = ConsoleColor.Gray;
	public ConsoleColor ForegroundSuspended = ConsoleColor.Gray;

	public ConsoleColor BackgroundActive = ConsoleColor.DarkGreen;
	public ConsoleColor BackgroundInactive = ConsoleColor.Black;
	public ConsoleColor BackgroundSuspended = ConsoleColor.DarkYellow;

	public void Toggle() => this.Setter(!this.Getter());
	public void SetState(bool state) => this.Setter(state);

	public ConsoleExtensions.CharInfo[] ToConsoleCharString() {
		bool state = this.Getter();
		ConsoleColor foreground = state || (!(this.SuspendStateGetter is null) && this.SuspendStateGetter())
			? Program.Engine.IsPaused
				? this.ForegroundSuspended
				: this.ForegroundActive
			: this.ForegroundInactive;
		ConsoleColor background = state || (!(this.SuspendStateGetter is null) && this.SuspendStateGetter())
			? Program.Engine.IsPaused
				? this.BackgroundSuspended
				: this.BackgroundActive
			: this.BackgroundInactive;

		ConsoleExtensions.CharInfo[] result = new ConsoleExtensions.CharInfo[this.Label.Length];
		for (int i = 0; i < this.Label.Length; i++)
			result[i] = new(this.Label[i], foreground, background);
		return result;
	}

	public static void HandleConsoleInputs(KeyListener[] listeners) {
		HashSet<ConsoleKey> pressed = new();
		HashSet<ConsoleKey> reset = new();

		while (Console.KeyAvailable) {
			ConsoleKeyInfo keyInfo = Console.ReadKey(true);

			foreach (KeyListener listener in listeners)
				if (listener.Key != keyInfo.Key)
					continue;
				else if (listener.Resetter is not null && (keyInfo.Modifiers & ConsoleModifiers.Shift) != 0)
					reset.Add(listener.Key);
				else pressed.Add(keyInfo.Key);
		}

		foreach (KeyListener listener in listeners)
			if (reset.Contains(listener.Key))
				listener.Resetter();
			else if (!listener.IsToggle)
				listener.SetState(pressed.Contains(listener.Key));
			else if (pressed.Contains(listener.Key))
				listener.Toggle();
	}
}