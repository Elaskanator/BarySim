using System;
using System.Threading;
using Generic;

namespace Boids {
	public static class Renderer {
		private static DateTime _targetStartDrawTime = DateTime.Now;
		public static ConsoleExtensions.CharInfo[] Render(Tuple<char, double>[] rasterization, SampleSMA[] bands) {
			if (Program.ENABLE_DEBUG_LOGGING) Generic.DebugExtensions.DebugWriteline("Render - Start");
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

			return frame;
			if (Program.ENABLE_DEBUG_LOGGING) Generic.DebugExtensions.DebugWriteline("Render - End");
		}

		public static void Draw(ConsoleExtensions.CharInfo[] buffer, SampleSMA[] bands) {
			if (Program.ENABLE_DEBUG_LOGGING) Generic.DebugExtensions.DebugWriteline("Draw - Start");
			Synchronize();

			int xOffset = Parameters.DEBUG_ENABLE ? 4 : 0,
				yOffset = Parameters.DEBUG_ENABLE && Parameters.PERF_STATS_ENABLE ? 1 : 0;

			TimeSpan timeSinceLastUpdate = DateTime.Now.Subtract(Program.Step_Rasterizer.LastIerationEnd ?? Program.Manager.StartTime);
			if (timeSinceLastUpdate.TotalMilliseconds >= Parameters.PERF_WARN_MS) {
				buffer ??= new ConsoleExtensions.CharInfo[Parameters.WINDOW_WIDTH * Parameters.WINDOW_HEIGHT];
				string message = "No update for " + (timeSinceLastUpdate.TotalSeconds.ToStringBetter(2) + "s").PadRight(5);
				for (int i = 0; i < message.Length; i++)
					buffer[i + xOffset + Parameters.WINDOW_WIDTH*yOffset] = new ConsoleExtensions.CharInfo(message[i], ConsoleColor.Red);
			}

			if (!(buffer is null)) ConsoleExtensions.WriteConsoleOutput(buffer);
			if (Program.ENABLE_DEBUG_LOGGING) Generic.DebugExtensions.DebugWriteline("Draw - End");
		}

		private static void Synchronize() {
			if (Parameters.MAX_FPS <= 0 && Parameters.TARGET_FPS <= 0) return;

			DateTime now = DateTime.Now;
			TimeSpan
				waitDuration = _targetStartDrawTime.Subtract(now),
				targetFrameInterval = TimeSpan.FromSeconds(1d / (Parameters.TARGET_FPS > 0 ? Parameters.TARGET_FPS : Parameters.MAX_FPS));

			if (waitDuration.Ticks >= 0) _targetStartDrawTime += targetFrameInterval;
			else {//missed it
				if (Parameters.SYNC_FRAMERATE) {
					int slip = (int)Math.Ceiling(-waitDuration.TotalSeconds / Parameters.TARGET_FPS);
					_targetStartDrawTime = _targetStartDrawTime.Add(targetFrameInterval * slip);
				} else _targetStartDrawTime = now.Add(targetFrameInterval);
				waitDuration = _targetStartDrawTime.Subtract(now);
			}
			if (waitDuration >= Parameters.MinSleepDuration) Thread.Sleep(waitDuration - Parameters.MinSleepDuration);
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
