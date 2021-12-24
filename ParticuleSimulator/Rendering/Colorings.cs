using System;

namespace ParticleSimulator.Rendering {
	public enum ParticleColoringMethod {
		Random,
		Count,
		Density,
		Luminosity,
		Depth,
		Group,
	}

	public static class ColoringScales {
		public static readonly Tuple<double, ConsoleColor>[] RatioColors = new Tuple<double, ConsoleColor>[] {
			new Tuple<double, ConsoleColor>(1.25d, ConsoleColor.Blue),
			new Tuple<double, ConsoleColor>(1.10d, ConsoleColor.Cyan),
			new Tuple<double, ConsoleColor>(1.02d, ConsoleColor.DarkCyan),
			new Tuple<double, ConsoleColor>(0.98d, ConsoleColor.Green),
			new Tuple<double, ConsoleColor>(0.90d, ConsoleColor.DarkGreen),
			new Tuple<double, ConsoleColor>(0.75d, ConsoleColor.Gray),
			new Tuple<double, ConsoleColor>(0.50d, ConsoleColor.DarkYellow),
			new Tuple<double, ConsoleColor>(0.33d, ConsoleColor.Yellow),
			new Tuple<double, ConsoleColor>(0.25d, ConsoleColor.Magenta),
			new Tuple<double, ConsoleColor>(0.10d, ConsoleColor.DarkRed),
			new Tuple<double, ConsoleColor>(0.00d, ConsoleColor.Red),
			new Tuple<double, ConsoleColor>(double.NegativeInfinity, ConsoleColor.White)
		};

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
		public static readonly ConsoleColor[] Grayscale = new ConsoleColor[] {
			ConsoleColor.DarkGray,
			ConsoleColor.Gray,
			ConsoleColor.White,
		};
		public static readonly ConsoleColor[] Radar = new ConsoleColor[] {
			ConsoleColor.DarkGray,
			ConsoleColor.Cyan,
			ConsoleColor.DarkCyan,
			ConsoleColor.Blue,
			ConsoleColor.Green,
			ConsoleColor.DarkGreen,
			ConsoleColor.DarkYellow,
			ConsoleColor.Red,
			ConsoleColor.DarkRed,
			ConsoleColor.DarkMagenta,
			ConsoleColor.Magenta,
			ConsoleColor.Gray,
			ConsoleColor.White,
		};
		public static readonly ConsoleColor[] StarColors = new ConsoleColor[] {
			ConsoleColor.DarkGray,
			ConsoleColor.DarkMagenta,
			ConsoleColor.Magenta,
			ConsoleColor.DarkRed,
			ConsoleColor.Red,
			ConsoleColor.DarkYellow,
			ConsoleColor.Yellow,
			ConsoleColor.White,
			ConsoleColor.Cyan,
			ConsoleColor.DarkCyan,
			ConsoleColor.Blue,
			ConsoleColor.DarkBlue,
		};
	}
}