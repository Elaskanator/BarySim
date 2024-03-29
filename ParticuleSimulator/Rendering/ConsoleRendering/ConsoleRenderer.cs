﻿using System;
using System.Linq;
using System.Threading;
using Generic.Extensions;
using ParticleSimulator.Engine;
using ParticleSimulator.Engine.Threading;
using ParticleSimulator.Rendering.Rasterization;

namespace ParticleSimulator.Rendering.SystemConsole {
	public class ConsoleRenderer : ARenderer {
		public const char CHAR_LOW  = '\u2584';//▄
		public const char CHAR_BOTH = '\u2588';//█
		public const char CHAR_TOP  = '\u2580';//▀

		public ConsoleRenderer(RenderEngine engine) : base(engine) {
			this.NumChars = Parameters.WINDOW_WIDTH * Parameters.WINDOW_HEIGHT;
			this._lastFrame = new ConsoleExtensions.CharInfo[NumChars];
			this._perfMon = new PerfMon(this);
			this._perfMon.Init();
		}

		public static ConsoleExtensions.CharInfo BuildChar(ConsoleColor bottomColor, ConsoleColor topColor) =>
			topColor == bottomColor
				? new ConsoleExtensions.CharInfo(0, 0, bottomColor)
				: new ConsoleExtensions.CharInfo(CHAR_LOW, bottomColor, topColor);

		public readonly int NumChars;

		private readonly PerfMon _perfMon;
		private ConsoleExtensions.CharInfo[] _lastFrame;
		private DateTime _lastPunctualWrite = DateTime.UtcNow;

		public static ConsoleColor GetRankColor(float rank, float[] scaling) =>
			rank < 0f
				? ConsoleColor.Green
				: Parameters.COLORS[scaling.Drop(1).TakeWhile(ds => ds < rank).Count()];

		public override void Init() {
			//prepare the rendering area (abusing the System.Console window with p-invokes to flush frame buffers)
			Console.WindowWidth = Parameters.WINDOW_WIDTH;
			Console.WindowHeight = Parameters.WINDOW_HEIGHT;
			//these require p-invokes
			ConsoleExtensions.HideScrollbars();
			//rendering gets *really* messed up if the window gets resized by anything
			ConsoleExtensions.DisableResizing();//note this doesn't work to disable OS window snapping
			//ConsoleExtensions.SetWindowPosition(0, 0);//TODO
			Console.CursorVisible = false;

			Thread titleMon = new(this.ConsoleTitleUpdate);
			titleMon.Start();
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

		protected override void DrawOverlays(EvalResult prepResults, float[] scaling, object bufferData) {
			ConsoleExtensions.CharInfo[] buffer = (ConsoleExtensions.CharInfo[])bufferData;
			this.Watchdog(prepResults, buffer);
			if (this.Engine.OverlaysEnabled) {
				this._perfMon.DrawStatsOverlay(prepResults, buffer);
				if (Parameters.COLORING != ParticleColoringMethod.Random
				&& Parameters.COLORING != ParticleColoringMethod.Group)
					this.DrawLegend(scaling, buffer);

				ConsoleExtensions.CharInfo[] label;
				int position = 1 + this._perfMon.Graph.Width, keyLabelOffset;
				string keyStr;
				for (int i = 0; i < this.Engine.KeyListeners.Length; i++) {
					label = this.Engine.KeyListeners[i].ToConsoleCharString();
					keyStr = this.Engine.KeyListeners[i].Key.ToString();

					keyLabelOffset = (int)(label.Length/2d - keyStr.Length/2d);
					for (int j = 0; j < keyStr.Length; j++)
						buffer[position + j + keyLabelOffset] = new(keyStr[j], ConsoleColor.Gray, ConsoleColor.Black);

					for (int j = 0; j < label.Length; j++)
						buffer[position + j + Parameters.WINDOW_WIDTH] = label[j];

					position += (label.Length < 2 ? 2 : label.Length) + 1;
				}
			}
		}

		protected override void UpdateMonitor(int framesCompleted, TimeSpan frameTime, TimeSpan fpsTime) =>
			this._perfMon.Graph.Update(framesCompleted % Parameters.MON_GRAPH_COLUMN_FRAMES, frameTime, fpsTime);

		private void Watchdog(EvalResult prepResults, ConsoleExtensions.CharInfo[] buffer) {
			if (!this.Engine.IsPaused && !this.Engine.OverlaysEnabled) {
				if (prepResults.PrepPunctual)
					this._lastPunctualWrite = DateTime.UtcNow;
				TimeSpan timeSinceLastUpdate = DateTime.UtcNow.Subtract(this._lastPunctualWrite);

				if (timeSinceLastUpdate.TotalMilliseconds >= Parameters.MON_WARN_MS) {
					string message = "No update for " + (timeSinceLastUpdate.TotalSeconds.ToStringBetter(2) + "s") + " ";
					for (int i = 0; i < message.Length; i++)
						buffer[i] = new ConsoleExtensions.CharInfo(message[i], ConsoleColor.Red);
				}
			}
		}

		private void ConsoleTitleUpdate() {
			while (this.Engine.IsOpen) {
				string result = string.Format("Baryon Simulator {0}D - {1}",
					Parameters.DIM,
					this.Engine.Simulator.ParticleCount.Pluralize("Particle"));
				if (this.FpsTimings.NumUpdates > 1)
					result += string.Format(" ({0} FPS)", (1d / this.FpsTimings.Current.TotalSeconds).ToStringBetter(2));
				if (this.Engine.IsPaused)
					result += " (paused)";

				Console.Title = result;
				Thread.Sleep(500);
			}
		}

		private void DrawLegend(float[] scaling, ConsoleExtensions.CharInfo[] buffer) {
			if (!(scaling is null) && scaling.Length > 0) {
				int numColors = scaling.Length;
				bool isDiscrete = false;//Parameters.DIM < 3 && Parameters.SIM_TYPE == SimulationType.Boid;
				string header = Parameters.COLORING.ToString();

				int pixelIdx = Parameters.WINDOW_WIDTH * (Parameters.WINDOW_HEIGHT - numColors - 1);
				for (int i = 0; i < header.Length; i++)
					buffer[pixelIdx + i] = new ConsoleExtensions.CharInfo(header[i], ConsoleColor.Gray);
			
				string rowStringData;
				for (int cIdx = 0; cIdx < numColors; cIdx++) {
					pixelIdx += Parameters.WINDOW_WIDTH;

					buffer[pixelIdx] = new ConsoleExtensions.CharInfo(
						CHAR_BOTH,
						Parameters.COLORS[cIdx]);

					rowStringData = 
						(isDiscrete && cIdx == 0 ? "=" : "≤")
						+ (isDiscrete
							? ((int)scaling[cIdx]).ToString()
							: scaling[cIdx].ToStringBetter(2, true, 3));

					for (int i = 0; i < rowStringData.Length; i++)
						buffer[pixelIdx + i + 1] = new ConsoleExtensions.CharInfo(rowStringData[i], ConsoleColor.Gray);
				}
			}
		}
	}
}