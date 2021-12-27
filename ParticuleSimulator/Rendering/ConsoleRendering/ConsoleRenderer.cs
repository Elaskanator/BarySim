using System;
using System.Linq;
using Generic.Extensions;
using ParticleSimulator.Engine;
using ParticleSimulator.Rendering.Rasterization;

namespace ParticleSimulator.Rendering.SystemConsole {
	public class ConsoleRenderer : ARenderer {
		public ConsoleRenderer(RenderEngine engine) : base(engine) {
			this.NumChars = Parameters.WINDOW_WIDTH * Parameters.WINDOW_HEIGHT;
			this._lastFrame = new ConsoleExtensions.CharInfo[NumChars];
			this._perfMon = new PerfMon(this);
		}

		public readonly int NumChars;
		private readonly PerfMon _perfMon;

		private ConsoleExtensions.CharInfo[] _lastFrame;

		public static ConsoleExtensions.CharInfo BuildChar(ConsoleColor bottomColor, ConsoleColor topColor) {
			if (topColor == bottomColor)
				return new ConsoleExtensions.CharInfo(0, 0, bottomColor);
			else return new ConsoleExtensions.CharInfo(Parameters.CHAR_LOW, bottomColor, topColor);
		}

		public override void Init() {
			string result = string.Format("Baryon Simulator {0}D - {1}",
				Parameters.DIM,
				this.Engine.Simulator.ParticleCount.Pluralize("Particle"));
			Console.Title = result;

			//prepare the rendering area (abusing the System.Console window with p-invokes to flush frame buffers)
			Console.WindowWidth = Parameters.WINDOW_WIDTH;
			Console.WindowHeight = Parameters.WINDOW_HEIGHT;
			Console.CursorVisible = false;
			//these require p-invokes
			ConsoleExtensions.HideScrollbars();
			//rendering gets *really* messed up if the window gets resized by anything
			ConsoleExtensions.DisableResizing();//note this doesn't work to disable OS window snapping
			//ConsoleExtensions.SetWindowPosition(0, 0);//TODO

			this._perfMon.Init();
		}

		protected override void Flush(object buffer) =>
			ConsoleExtensions.WriteConsoleOutput((ConsoleExtensions.CharInfo[])buffer);

		protected override object PrepareBuffer(float[] scaling, Pixel[] buffer) {
			if (!(buffer is null)) {
				ConsoleExtensions.CharInfo[] chars = new ConsoleExtensions.CharInfo[this.NumChars];
				int col = 0, row = 0, i1, i2;
				for (int i = 0; i < this.NumChars; i++) {
					i1 = col + (1 + (row << 1)) * Parameters.WINDOW_WIDTH;
					i2 = col + (row << 1) * Parameters.WINDOW_WIDTH;
					chars[i] = BuildChar(
						buffer[i1].IsNotNull
							? GetRankColor(buffer[i1].Rank, scaling)
							: ConsoleColor.Black,
						buffer[i2].IsNotNull
							? GetRankColor(buffer[i2].Rank, scaling)
							: ConsoleColor.Black);
					col++;
					if (col >= Parameters.WINDOW_WIDTH) {
						col = 0;
						row++;
					}
				}
				this._lastFrame = chars;
			}
			return this._lastFrame;
		}

		protected override void DrawOverlays(bool isPaused, bool wasPunctual, float[] scaling, object bufferData) {
			ConsoleExtensions.CharInfo[] buffer = (ConsoleExtensions.CharInfo[])bufferData;
			this.Watchdog(buffer);
			if (Parameters.PERF_ENABLE)
				this._perfMon.DrawStatsOverlay(buffer, wasPunctual);
			if (Parameters.LEGEND_ENABLE
			&& Parameters.COLOR_METHOD != ParticleColoringMethod.Random
			&& Parameters.COLOR_METHOD != ParticleColoringMethod.Group)
				this.DrawLegend(scaling, buffer);
			if (isPaused) {
				string message = "Paused";
				for (int i = 0; i < message.Length; i++)
					buffer[i + 50] = new ConsoleExtensions.CharInfo(message[i], ConsoleColor.Yellow, ConsoleColor.Black);
			}
		}

		protected override void UpdateMonitor(int framesCompleted, TimeSpan frameTime, TimeSpan fpsTime) =>
			this._perfMon.Graph.Update(framesCompleted % Parameters.PERF_GRAPH_FRAMES_PER_COLUMN, frameTime, fpsTime);

		private void Watchdog(ConsoleExtensions.CharInfo[] buffer) {
			int xOffset = Parameters.PERF_ENABLE ? 6 : 0,
				yOffset = Parameters.PERF_ENABLE ? 1 : 0;

			TimeSpan timeSinceLastUpdate = DateTime.UtcNow.Subtract(this.Engine.StepEval_Rasterize.LastComputeStartUtc ?? Program.Engine.StartTimeUtc.Value);
			bool isSlow = timeSinceLastUpdate.TotalMilliseconds >= Parameters.PERF_WARN_MS;
			if (isSlow) {
				string message = "No update for " + (timeSinceLastUpdate.TotalSeconds.ToStringBetter(2) + "s") + " ";
				for (int i = 0; i < message.Length; i++)
					buffer[i + xOffset + Parameters.WINDOW_WIDTH*yOffset] = new ConsoleExtensions.CharInfo(message[i], ConsoleColor.Red);
			}
		}

		private void DrawLegend(float[] scaling, ConsoleExtensions.CharInfo[] buffer) {
			int numColors = scaling.Length;
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
							? ((int)scaling[cIdx]).ToString()
							: scaling[cIdx].ToStringBetter(2, true, 5));

					for (int i = 0; i < rowStringData.Length; i++)
						buffer[pixelIdx + i + 1] = new ConsoleExtensions.CharInfo(rowStringData[i], ConsoleColor.White);
				}
			}
		}

		public static ConsoleColor GetRankColor(float rank, float[] scaling) =>
			Parameters.COLOR_ARRAY[scaling.Drop(1).TakeWhile(ds => ds < rank).Count()];
	}
}