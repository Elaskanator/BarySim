using System;
using System.Linq;
using Generic.Extensions;

namespace ParticleSimulator.Simulation {
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

			if (Parameters.DIM > 1) {
				double aspectRatio = Parameters.DOMAIN_SIZE[0] / Parameters.DOMAIN_SIZE[1];
				double consoleAspectRatio = (double)MaxX / (double)MaxY;
				if (aspectRatio > consoleAspectRatio) {//wide
					RenderWidth = MaxX;
					RenderHeight = (int)(MaxX * Parameters.DOMAIN_SIZE[1] / Parameters.DOMAIN_SIZE[0]);
					if (RenderHeight < 1) RenderHeight = 1;
					RenderHeightOffset = (MaxY - RenderHeight) / 4;
				} else {//tall
					RenderWidth = (int)(MaxY * Parameters.DOMAIN_SIZE[0] / Parameters.DOMAIN_SIZE[1]);
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
			Tuple<char, AParticle[], double>[] sampling = (Tuple<char, AParticle[], double>[])parameters[0];

			ConsoleExtensions.CharInfo[] frameBuffer = new ConsoleExtensions.CharInfo[Parameters.WINDOW_WIDTH * Parameters.WINDOW_HEIGHT];
			if (!(sampling is null)) {
				for (int i = 0; i < sampling.Length; i++)
					frameBuffer[i] = sampling[i] is null ? default :
						new ConsoleExtensions.CharInfo(
							sampling[i].Item1,
							Program.Simulator.ChooseColor(sampling[i]));

				if (Parameters.LEGEND_ENABLE && (Parameters.COLOR_SCHEME != ParticleColoringMethod.Depth || Parameters.DIM > 2))
					DrawLegend(frameBuffer);
			}

			return frameBuffer;
		}
		
		public static void TitleUpdate(object[] parameters) {
			int visibleParticles = Program.Simulator.AllParticles.Count(p => p.IsVisible);

			Console.Title = string.Format("{0} Simulator - {1}{2} - {3}D",
				Parameters.SimType,
				Program.Simulator.AllParticles.Length.Pluralize("particle"),
				visibleParticles < Program.Simulator.AllParticles.Length
					? string.Format(" ({0} visible)", visibleParticles)
					: "",
				Parameters.DIM);
		}

		private static DateTime? _lastUpdateUtc = null;
		public static void FlushScreenBuffer(object[] parameters) {
			ConsoleExtensions.CharInfo[] buffer = (ConsoleExtensions.CharInfo[])parameters[0]
				?? new ConsoleExtensions.CharInfo[Parameters.WINDOW_WIDTH * Parameters.WINDOW_HEIGHT];

			int xOffset = Parameters.PERF_ENABLE ? 6 : 0,
				yOffset = Parameters.PERF_ENABLE ? 1 : 0;

			if (Program.StepEval_Draw.IsPunctual ?? false) _lastUpdateUtc = DateTime.UtcNow;
			TimeSpan timeSinceLastUpdate = DateTime.UtcNow.Subtract(_lastUpdateUtc ?? Program.Manager.StartTimeUtc);
			bool isSlow = timeSinceLastUpdate.TotalMilliseconds >= Parameters.PERF_WARN_MS;
			if (isSlow) {
				string message = "No update for " + (timeSinceLastUpdate.TotalSeconds.ToStringBetter(2) + "s").PadRight(6);
				for (int i = 0; i < message.Length; i++)
					buffer[i + xOffset + Parameters.WINDOW_WIDTH*yOffset] = new ConsoleExtensions.CharInfo(message[i], ConsoleColor.Red);
			}

			if (Parameters.PERF_ENABLE)
				PerfMon.DrawStatsOverlay(buffer, isSlow);

			if (!(buffer is null)) ConsoleExtensions.WriteConsoleOutput(buffer);
		}

		public static void DrawLegend(ConsoleExtensions.CharInfo[] buffer) {
			int numColors = Program.Simulator.DensityScale.Length;
			if (numColors > 0) {
				bool isDiscrete = Parameters.DIM < 3 && Parameters.SimType == SimulationType.Boid;
				string header = Parameters.COLOR_SCHEME.ToString();
				if (Parameters.COLOR_SCHEME == ParticleColoringMethod.Group) {
					if (Parameters.PARTICLES_GROUP_COUNT > numColors)
						header += " (mod " + numColors + ")";
					if (Parameters.PARTICLES_GROUP_COUNT < Parameters.COLOR_ARRAY.Length)
						numColors = Parameters.PARTICLES_GROUP_COUNT;
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

					if (Parameters.COLOR_SCHEME == ParticleColoringMethod.Group)
						rowStringData = "=" + cIdx.ToString();
					else rowStringData =
							(isDiscrete && cIdx == 0 ? "=" : "≤")
							+ (isDiscrete
								? ((int)Program.Simulator.DensityScale[cIdx]).ToString()
								: Program.Simulator.DensityScale[cIdx].ToStringBetter(2));

					for (int i = 0; i < rowStringData.Length; i++)
						buffer[pixelIdx + i + 1] = new ConsoleExtensions.CharInfo(rowStringData[i], ConsoleColor.White);
				}
			}
		}
	}
}