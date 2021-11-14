using System;

namespace ParticleSimulator.Rendering {
	public enum ParticleColoringMethod {
		Group,
		Density,
		Depth
	}
	public enum ParticleColorScale {
		DefaultConsoleColors,
		Grayscale,
		RadarColors,
		ReducedColors
	}

	public static class ParticleColoringScales {
		public static readonly ConsoleColor[] DEFAULT_CONSOLE_COLORS = new ConsoleColor[] {
			ConsoleColor.DarkBlue,
			ConsoleColor.DarkGreen,
			ConsoleColor.DarkCyan,
			ConsoleColor.DarkRed,
			ConsoleColor.DarkMagenta,
			ConsoleColor.DarkYellow,
			ConsoleColor.Gray,
			ConsoleColor.DarkGray,
			ConsoleColor.Blue,
			ConsoleColor.Green,
			ConsoleColor.Cyan,
			ConsoleColor.Red,
			ConsoleColor.Magenta,
			ConsoleColor.Yellow,
			ConsoleColor.White
		};
		public static readonly ConsoleColor[] Radar = new ConsoleColor[] {
			ConsoleColor.DarkGray,
			ConsoleColor.Gray,
			ConsoleColor.Cyan,
			ConsoleColor.White,
			ConsoleColor.DarkCyan,
			ConsoleColor.Blue,
			ConsoleColor.DarkBlue,
			ConsoleColor.DarkGreen,
			ConsoleColor.Green,
			ConsoleColor.Yellow,
			ConsoleColor.Red,
			ConsoleColor.DarkRed,
			ConsoleColor.Magenta,
			ConsoleColor.DarkMagenta,
		};
		public static readonly ConsoleColor[] Reduced = new ConsoleColor[] {
			ConsoleColor.DarkGray,
			ConsoleColor.Gray,
			ConsoleColor.White,
			ConsoleColor.Cyan,
			ConsoleColor.Yellow,
			ConsoleColor.Red,
		};
		public static readonly ConsoleColor[] Grayscale = new ConsoleColor[] {
			ConsoleColor.DarkGray,
			ConsoleColor.Gray,
			ConsoleColor.White,
		};
	}
}