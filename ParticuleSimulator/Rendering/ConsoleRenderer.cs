using System;
using System.Linq;
using Generic.Extensions;

namespace ParticleSimulator.Rendering {
	public partial class ConsoleRenderer {
		public ConsoleRenderer() {
			this.NumChars = Parameters.WINDOW_WIDTH * Parameters.WINDOW_HEIGHT;
			this.Rasterizer = new(Parameters.WINDOW_WIDTH, Parameters.WINDOW_HEIGHT * 2);

			this._lastFrame = new ConsoleExtensions.CharInfo[NumChars];
		}

		public readonly int NumChars;
		public readonly Rasterizer Rasterizer;

		private ConsoleExtensions.CharInfo[] _lastFrame;

		public static ConsoleExtensions.CharInfo BuildChar(ConsoleColor bottomColor, ConsoleColor topColor) {
			if (topColor == bottomColor)
				return new ConsoleExtensions.CharInfo(0, 0, bottomColor);
			else return new ConsoleExtensions.CharInfo(Parameters.CHAR_LOW, bottomColor, topColor);
		}

		public void FlushScreenBuffer(object[] parameters) {
			Pixel[] resampling = (Pixel[])parameters[0];
			ConsoleExtensions.CharInfo[] buffer;
			if (resampling is null) {
				buffer = this._lastFrame;
			} else {
				buffer = new ConsoleExtensions.CharInfo[this.NumChars];
				int col = 0, row = 0, i1, i2;
				for (int i = 0; i < this.NumChars; i++) {
					i1 = col + (1 + (row << 1)) * Parameters.WINDOW_WIDTH;
					i2 = col + (row << 1) * Parameters.WINDOW_WIDTH;
					buffer[i] = BuildChar(
						resampling[i1].IsNotNull ? this.GetRankColor(resampling[i1].Rank) : ConsoleColor.Black,
						resampling[i2].IsNotNull ? this.GetRankColor(resampling[i2].Rank) : ConsoleColor.Black);
					col++;
					if (col >= Parameters.WINDOW_WIDTH) {
						col = 0;
						row++;
					}
				}
				this._lastFrame = buffer;
			}

			bool isSlow = Watchdog(buffer);

			if (Parameters.LEGEND_ENABLE)
				this.DrawLegend(buffer);

			if (Parameters.PERF_ENABLE)
				Program.Monitor.DrawStatsOverlay(buffer, isSlow);

			ConsoleExtensions.WriteConsoleOutput(buffer);
		}

		private bool Watchdog(ConsoleExtensions.CharInfo[] buffer) {
			int xOffset = Parameters.PERF_ENABLE ? 6 : 0,
				yOffset = Parameters.PERF_ENABLE ? 1 : 0;

			TimeSpan timeSinceLastUpdate = DateTime.UtcNow.Subtract(Program.StepEval_Rasterize.LastComputeStartUtc ?? Program.Engine.StartTimeUtc.Value);
			bool isSlow = timeSinceLastUpdate.TotalMilliseconds >= Parameters.PERF_WARN_MS;
			if (isSlow) {
				string message = "No update for " + (timeSinceLastUpdate.TotalSeconds.ToStringBetter(2) + "s") + " ";
				for (int i = 0; i < message.Length; i++)
					buffer[i + xOffset + Parameters.WINDOW_WIDTH*yOffset] = new ConsoleExtensions.CharInfo(message[i], ConsoleColor.Red);
			}

			return isSlow;
		}

		public void DrawLegend(ConsoleExtensions.CharInfo[] buffer) {
			int numColors = this.Rasterizer.Scaling.Values.Length;
			if (numColors > 0) {
				bool isDiscrete = false;//Parameters.DIM < 3 && Parameters.SIM_TYPE == SimulationType.Boid;
				string header = Parameters.COLOR_METHOD.ToString();

				int pixelIdx = Parameters.WINDOW_WIDTH * (Parameters.WINDOW_HEIGHT - numColors - 1);
				for (int i = 0; i < header.Length; i++)
					buffer[pixelIdx + i] = new ConsoleExtensions.CharInfo(header[i], ConsoleColor.White);
			
				string rowStringData;
				for (int cIdx = 0; cIdx < numColors; cIdx++) {
					pixelIdx += Parameters.WINDOW_WIDTH;

					buffer[pixelIdx] = new ConsoleExtensions.CharInfo(
						Parameters.CHAR_BOTH,
						Parameters.COLOR_ARRAY[cIdx]);

					rowStringData = 
						(isDiscrete && cIdx == 0 ? "=" : "≤")
						+ (isDiscrete
							? ((int)this.Rasterizer.Scaling.Values[cIdx]).ToString()
							: this.Rasterizer.Scaling.Values[cIdx].ToStringBetter(2, true, 5));

					for (int i = 0; i < rowStringData.Length; i++)
						buffer[pixelIdx + i + 1] = new ConsoleExtensions.CharInfo(rowStringData[i], ConsoleColor.White);
				}
			}
		}

		public ConsoleColor GetRankColor(float rank) =>
			Parameters.COLOR_ARRAY[this.Rasterizer.Scaling.Values.Drop(1).TakeWhile(ds => ds < rank).Count()];
	}
}