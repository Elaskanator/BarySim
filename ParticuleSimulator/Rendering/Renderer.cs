﻿using System;
using System.Linq;
using Generic.Extensions;
using Generic.Models;

namespace ParticleSimulator.Rendering {
	public static class Renderer {
		public static readonly int RenderWidthOffset = 0;
		public static readonly int RenderHeightOffset = 0;
		public static readonly int RenderWidth;
		public static readonly int RenderHeight;
		public static readonly int MaxX;
		public static readonly int MaxY;

		static Renderer() {
			MaxX = Parameters.WINDOW_WIDTH;
			MaxY = Parameters.WINDOW_HEIGHT * 2;

			if (Parameters.DOMAIN_DOUBLE.Length > 1) {
				double aspectRatio = Parameters.DOMAIN_DOUBLE[0] / Parameters.DOMAIN_DOUBLE[1];
				double consoleAspectRatio = (double)MaxX / (double)MaxY;
				if (aspectRatio > consoleAspectRatio) {//wide
					RenderWidth = MaxX;
					RenderHeight = (int)(MaxX * Parameters.DOMAIN_DOUBLE[1] / Parameters.DOMAIN_DOUBLE[0]);
					if (RenderHeight < 1) RenderHeight = 1;
					RenderHeightOffset = (MaxY - RenderHeight) / 4;
				} else {//tall
					RenderWidth = (int)(MaxY * Parameters.DOMAIN_DOUBLE[0] / Parameters.DOMAIN_DOUBLE[1]);
					RenderHeight = MaxY;
					RenderWidthOffset = (MaxX - RenderWidth) / 2;
				}
			} else {
				RenderWidth = MaxX;
				RenderHeight = 1;
				RenderHeightOffset = MaxY / 4;
			}
		}

		public static ConsoleExtensions.CharInfo[] Render(object[] p) {
			if (Program.ENABLE_DEBUG_LOGGING) DebugExtensions.DebugWriteline("Render - Start");
			Tuple<char, double>[] rasterization = (Tuple<char, double>[])p[0];

			ConsoleExtensions.CharInfo[] frame = new ConsoleExtensions.CharInfo[Parameters.WINDOW_WIDTH * Parameters.WINDOW_HEIGHT];
			if (!(rasterization is null)) {
				for (int i = 0; i < rasterization.Length; i++)
					frame[i] = rasterization[i] is null ? default :
						new ConsoleExtensions.CharInfo(
							rasterization[i].Item1,
							ChooseDensityColor(rasterization[i].Item2));

				if (Parameters.LEGEND_ENABLE) DrawLegend(frame, Program.Simulator.DensityScale);
				if (Parameters.PERF_GRAPH_ENABLE) frame.RegionMerge(Parameters.WINDOW_WIDTH, PerfMon.GetFpsGraph(), PerfMon.GraphWidth, 0, 1, true);
			}
			if (Parameters.PERF_ENABLE) PerfMon.DrawStatsOverlay(frame);

			if (Program.ENABLE_DEBUG_LOGGING) DebugExtensions.DebugWriteline("Render - End");
			return frame;
		}

		public static void FlushScreenBuffer(ConsoleExtensions.CharInfo[] buffer) {
			if (Program.ENABLE_DEBUG_LOGGING) DebugExtensions.DebugWriteline("Draw - Start");

			int xOffset = Parameters.PERF_ENABLE ? 4 : 0,
				yOffset = Parameters.PERF_ENABLE ? 1 : 0;

			TimeSpan timeSinceLastUpdate = DateTime.UtcNow.Subtract(Program.Step_Rasterizer.LastIerationEndUtc ?? Program.Manager.StartTimeUtc);
			if (timeSinceLastUpdate.TotalMilliseconds >= Parameters.PERF_WARN_MS) {
				buffer ??= new ConsoleExtensions.CharInfo[Parameters.WINDOW_WIDTH * Parameters.WINDOW_HEIGHT];
				string message = "No update for " + (timeSinceLastUpdate.TotalSeconds.ToStringBetter(2) + "s").PadRight(6);
				for (int i = 0; i < message.Length; i++)
					buffer[i + xOffset + Parameters.WINDOW_WIDTH*yOffset] = new ConsoleExtensions.CharInfo(message[i], ConsoleColor.Red);
			}

			if (!(buffer is null)) ConsoleExtensions.WriteConsoleOutput(buffer);
			if (Program.ENABLE_DEBUG_LOGGING) DebugExtensions.DebugWriteline("Draw - End");
		}

		public static void DrawLegend(ConsoleExtensions.CharInfo[] buffer, SampleSMA[] densityScale) {
			int pixelIdx = Parameters.WINDOW_WIDTH * (Parameters.WINDOW_HEIGHT - Parameters.DENSITY_COLORS.Length);
			Func<double, string> rounding = Program.Simulator.IsDiscrete ? x => ((int)x).ToString() : x => x.ToStringBetter(2);
			string strData;
			for (int cIdx = 0; cIdx < Parameters.DENSITY_COLORS.Length; cIdx++) {
				buffer[pixelIdx] = new ConsoleExtensions.CharInfo(
					Parameters.CHAR_BOTH,
					Parameters.DENSITY_COLORS[cIdx]);

				if (cIdx == 0) strData = (!Program.Simulator.IsDiscrete || densityScale[cIdx].Current >= 2 ? "≤" : "=") + rounding(densityScale[cIdx].Current);
				else if (cIdx < densityScale.Length) strData = "=" + rounding(densityScale[cIdx].Current);
				else strData = ">";

				for (int sIdx = 0; sIdx < strData.Length; sIdx++)
					buffer[pixelIdx + sIdx + 1] = new ConsoleExtensions.CharInfo(strData[sIdx], ConsoleColor.White);

				pixelIdx += Parameters.WINDOW_WIDTH;
			}
		}

		public static ConsoleColor ChooseDensityColor(double count) {
			Predicate<double> comparer = Program.Simulator.IsDiscrete
				? a => (int)(2d * a) < (int)(2d * count)
				: a => a < count;
			int rank = Program.Simulator.DensityScale.TakeWhile(a => comparer(a.Current)).Count();
			return Parameters.DENSITY_COLORS[rank];
		}
	}
}