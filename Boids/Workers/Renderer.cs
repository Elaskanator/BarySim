using System;
using Generic;

namespace Boids {
	public static class Renderer {
		public static readonly Generic.Classes.TimeSynchronizer SYNC = Generic.Classes.TimeSynchronizer.FromFps(Parameters.TARGET_FPS, Parameters.MAX_FPS);

		public static ConsoleExtensions.CharInfo[] Render(object[] p) {
			if (Program.ENABLE_DEBUG_LOGGING) Generic.DebugExtensions.DebugWriteline("Render - Start");
			Tuple<char, double>[] rasterization = (Tuple<char, double>[])p[0];
			SampleSMA[] bands = (SampleSMA[])p[1];

			ConsoleExtensions.CharInfo[] frame = new ConsoleExtensions.CharInfo[Parameters.WINDOW_WIDTH * Parameters.WINDOW_HEIGHT];
			if (!(rasterization is null) && !(bands is null)) for (int i = 0; i < rasterization.Length; i++)
				frame[i] = rasterization[i] is null ? default :
					new ConsoleExtensions.CharInfo(
						rasterization[i].Item1,
						Rasterizer.ChooseDensityColor(rasterization[i].Item2, bands));

			if (Parameters.LEGEND_ENABLE && !(bands is null)) DrawLegend(frame, bands);
			if (Parameters.DEBUG_ENABLE) {
				if (Parameters.PERF_STATS_ENABLE) PerfMon.DrawStatsHeader(frame);
				if (Parameters.PERF_GRAPH_ENABLE && !(rasterization is null)) frame.RegionMerge(Parameters.WINDOW_WIDTH, PerfMon.GetFpsGraph(), Parameters.GRAPH_WIDTH, 0, Parameters.PERF_STATS_ENABLE ? 1 : 0, true);
			}

			if (Program.ENABLE_DEBUG_LOGGING) Generic.DebugExtensions.DebugWriteline("Render - End");
			return frame;
		}

		public static void Draw(object[] p) {
			if (Program.ENABLE_DEBUG_LOGGING) Generic.DebugExtensions.DebugWriteline("Draw - Start");
			ConsoleExtensions.CharInfo[] buffer = (ConsoleExtensions.CharInfo[])p[0];
			SampleSMA[] bands = (SampleSMA[])p[1];

			SYNC.Synchronize();

			int xOffset = Parameters.DEBUG_ENABLE ? 4 : 0,
				yOffset = Parameters.DEBUG_ENABLE && Parameters.PERF_STATS_ENABLE ? 1 : 0;

			TimeSpan timeSinceLastUpdate = DateTime.UtcNow.Subtract(Program.Step_Rasterizer.LastIerationEndUtc ?? Program.Manager.StartTimeUtc);
			if (timeSinceLastUpdate.TotalMilliseconds >= Parameters.PERF_WARN_MS) {
				buffer ??= new ConsoleExtensions.CharInfo[Parameters.WINDOW_WIDTH * Parameters.WINDOW_HEIGHT];
				string message = "No update for " + (timeSinceLastUpdate.TotalSeconds.ToStringBetter(2) + "s").PadRight(6);
				for (int i = 0; i < message.Length; i++)
					buffer[i + xOffset + Parameters.WINDOW_WIDTH*yOffset] = new ConsoleExtensions.CharInfo(message[i], ConsoleColor.Red);
			}

			if (!(buffer is null)) ConsoleExtensions.WriteConsoleOutput(buffer);
			if (Program.ENABLE_DEBUG_LOGGING) Generic.DebugExtensions.DebugWriteline("Draw - End");
		}

		private static void DrawLegend(ConsoleExtensions.CharInfo[] buffer, SampleSMA[] bands) {
			int pixelIdx = Parameters.WINDOW_WIDTH * (Parameters.WINDOW_HEIGHT - Parameters.DENSITY_COLORS.Length);
			string strData;
			for (int cIdx = 0; cIdx < Parameters.DENSITY_COLORS.Length; cIdx++) {
				buffer[pixelIdx] = new ConsoleExtensions.CharInfo(
					Rasterizer.CHAR_BOTH,
					Parameters.DENSITY_COLORS[cIdx]);

				if (cIdx < bands.Length) strData = "=" + ((int)bands[cIdx].Current.Value).ToString("G4");
				else strData = ">";

				for (int sIdx = 0; sIdx < strData.Length; sIdx++)
					buffer[pixelIdx + sIdx + 1] = new ConsoleExtensions.CharInfo(strData[sIdx], ConsoleColor.White);

				pixelIdx += Parameters.WINDOW_WIDTH;
			}
		}
	}
}
