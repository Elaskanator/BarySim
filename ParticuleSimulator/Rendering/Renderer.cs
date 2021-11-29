using System;
using Generic.Extensions;

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

			if (Parameters.DOMAIN.Length > 1) {
				double aspectRatio = Parameters.DOMAIN[0] / Parameters.DOMAIN[1];
				double consoleAspectRatio = (double)MaxX / (double)MaxY;
				if (aspectRatio > consoleAspectRatio) {//wide
					RenderWidth = MaxX;
					RenderHeight = (int)(MaxX * Parameters.DOMAIN[1] / Parameters.DOMAIN[0]);
					if (RenderHeight < 1) RenderHeight = 1;
					RenderHeightOffset = (MaxY - RenderHeight) / 4;
				} else {//tall
					RenderWidth = (int)(MaxY * Parameters.DOMAIN[0] / Parameters.DOMAIN[1]);
					RenderHeight = MaxY;
					RenderWidthOffset = (MaxX - RenderWidth) / 2;
				}
			} else {
				RenderWidth = MaxX;
				RenderHeight = 1;
				RenderHeightOffset = MaxY / 4;
			}
		}

		public static ConsoleExtensions.CharInfo[] Rasterize(object[] parameters) {
			Tuple<char, AParticle[]>[] rasterization = (Tuple<char, AParticle[]>[])parameters[0];

			ConsoleExtensions.CharInfo[] frameBuffer = new ConsoleExtensions.CharInfo[Parameters.WINDOW_WIDTH * Parameters.WINDOW_HEIGHT];
			if (!(rasterization is null)) {
				for (int i = 0; i < rasterization.Length; i++)
					frameBuffer[i] = rasterization[i] is null ? default :
						new ConsoleExtensions.CharInfo(
							rasterization[i].Item1,
							Program.Simulator.ChooseColor(rasterization[i].Item2));

				if (Parameters.LEGEND_ENABLE && (Parameters.COLOR_SCHEME != ParticleColoringMethod.Depth || Parameters.DOMAIN.Length > 2)) DrawLegend(frameBuffer);
				if (Parameters.PERF_GRAPH_ENABLE) PerfMon.DrawFpsGraph(frameBuffer);
			}
			if (Parameters.PERF_ENABLE) PerfMon.DrawStatsOverlay(frameBuffer);

			return frameBuffer;
		}

		private static DateTime? _lastUpdateUtc = null;
		public static void FlushScreenBuffer(object[] parameters) {
			ConsoleExtensions.CharInfo[] buffer = (ConsoleExtensions.CharInfo[])parameters[0];

			int xOffset = Parameters.PERF_ENABLE ? 6 : 0,
				yOffset = Parameters.PERF_ENABLE ? 1 : 0;

			if (Program.StepEval_Draw.IsPunctual ?? false) _lastUpdateUtc = DateTime.UtcNow;
			TimeSpan timeSinceLastUpdate = DateTime.UtcNow.Subtract(_lastUpdateUtc ?? Program.Manager.StartTimeUtc);
			if (timeSinceLastUpdate.TotalMilliseconds >= Parameters.PERF_WARN_MS) {
				buffer ??= new ConsoleExtensions.CharInfo[Parameters.WINDOW_WIDTH * Parameters.WINDOW_HEIGHT];
				string message = "No update for " + (timeSinceLastUpdate.TotalSeconds.ToStringBetter(2) + "s").PadRight(6);
				for (int i = 0; i < message.Length; i++)
					buffer[i + xOffset + Parameters.WINDOW_WIDTH*yOffset] = new ConsoleExtensions.CharInfo(message[i], ConsoleColor.Red);
			}

			if (!(buffer is null)) ConsoleExtensions.WriteConsoleOutput(buffer);
		}

		public static void DrawLegend(ConsoleExtensions.CharInfo[] buffer) {
			int numColors = Parameters.COLOR_ARRAY.Length;
			string header = Parameters.COLOR_SCHEME.ToString();
			switch (Parameters.COLOR_SCHEME) {
				case ParticleColoringMethod.Density:
					if (Parameters.DOMAIN.Length > 2)
						header += " (depth scaled)";
					break;
				case ParticleColoringMethod.Group:
					if (Parameters.NUM_PARTICLE_GROUPS > numColors)
						header += " (ID modulo " + numColors + ")";
					if (Parameters.NUM_PARTICLE_GROUPS < Parameters.COLOR_ARRAY.Length)
						numColors = Parameters.NUM_PARTICLE_GROUPS;
					break;
			}

			int pixelIdx = Parameters.WINDOW_WIDTH * (Parameters.WINDOW_HEIGHT - numColors - 1);
			for (int i = 0; i < header.Length; i++)
				buffer[pixelIdx + i] = new ConsoleExtensions.CharInfo(header[i], ConsoleColor.White);
			
			string rowStringData;
			for (int cIdx = 0; cIdx < numColors; cIdx++) {
				pixelIdx += Parameters.WINDOW_WIDTH;

				buffer[pixelIdx] = new ConsoleExtensions.CharInfo(
					Parameters.CHAR_BOTH,
					Parameters.COLOR_ARRAY[cIdx]);

				if (Parameters.COLOR_SCHEME == ParticleColoringMethod.Group) {
					rowStringData = "=" + cIdx.ToString();
				} else if (cIdx < numColors - 1){
					rowStringData =
						(Parameters.DOMAIN.Length < 3 && cIdx == 0 ? "=" : "≤")
						+ (Parameters.DOMAIN.Length < 3
							? ((int)Program.Simulator.DensityScale[cIdx]).ToString()
							: Program.Simulator.DensityScale[cIdx].ToStringBetter(2));
				} else rowStringData = ">";

				for (int i = 0; i < rowStringData.Length; i++)
					buffer[pixelIdx + i + 1] = new ConsoleExtensions.CharInfo(rowStringData[i], ConsoleColor.White);
			}
		}
	}
}