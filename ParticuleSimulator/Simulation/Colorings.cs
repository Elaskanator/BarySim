using System;

namespace ParticleSimulator.Simulation {
	public enum ParticleColoringMethod {
		Group,
		Density,
		Depth
	}

	public static class ColoringScales {
		public static readonly Tuple<double, ConsoleColor>[] RatioColors = new Tuple<double, ConsoleColor>[] {
			new Tuple<double, ConsoleColor>(1.10d, ConsoleColor.Cyan),
			new Tuple<double, ConsoleColor>(1.00d, ConsoleColor.DarkGreen),
			new Tuple<double, ConsoleColor>(0.90d, ConsoleColor.Green),
			new Tuple<double, ConsoleColor>(0.75d, ConsoleColor.Gray),
			new Tuple<double, ConsoleColor>(0.50d, ConsoleColor.Yellow),
			new Tuple<double, ConsoleColor>(0.33d, ConsoleColor.DarkYellow),
			new Tuple<double, ConsoleColor>(0.25d, ConsoleColor.Magenta),
			new Tuple<double, ConsoleColor>(0.10d, ConsoleColor.Red),
			new Tuple<double, ConsoleColor>(0.00d, ConsoleColor.DarkRed),
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
		public static readonly ConsoleColor[] Radar = new ConsoleColor[] {
			ConsoleColor.DarkGray,
			ConsoleColor.DarkBlue,
			ConsoleColor.Blue,
			ConsoleColor.DarkCyan,
			ConsoleColor.Cyan,
			ConsoleColor.Green,
			ConsoleColor.DarkYellow,
			ConsoleColor.Yellow,
			ConsoleColor.Red,
			ConsoleColor.DarkRed,
			ConsoleColor.Magenta,
			ConsoleColor.DarkMagenta,
			//ConsoleColor.White,
		};
		public static readonly ConsoleColor[] Grayscale = new ConsoleColor[] {
			ConsoleColor.DarkGray,
			ConsoleColor.Gray,
			ConsoleColor.White,
		};
	}
}