using System;
using Generic.Extensions;
using ParticleSimulator.Engine;

namespace ParticleSimulator.Rendering.SystemConsole {
	public class PerfMon {
		public PerfMon(ARenderer renderer) {
			this._engine = renderer.Engine;
		}

		public PerfGraph Graph { get; private set; }
		
		private readonly RenderEngine _engine;
		private HeaderValue[] _statsHeaderValues;

		public void Init() {
			_statsHeaderValues = new HeaderValue[
				1 + (Parameters.PERF_STATS_ENABLE
					? 1 + this._engine.Evaluators.Length
					: 0)];
			this.Graph = new PerfGraph(this._engine.Evaluators.Length);
		}

		public void DrawStatsOverlay(ConsoleExtensions.CharInfo[] frameBuffer, bool wasPunctual) {
			this.RefreshStatsHeader(wasPunctual);

			int position = 0;
			string numberStr;
			for (int i = 0; i < _statsHeaderValues.Length; i++) {
				for (int j = 0; j < _statsHeaderValues[i].Label.Length; j++)
					frameBuffer[position + j] = new ConsoleExtensions.CharInfo(_statsHeaderValues[i].Label[j], ConsoleColor.White);
				position += _statsHeaderValues[i].Label.Length;
				numberStr = _statsHeaderValues[i].Value.ToStringBetter(Parameters.NUMBER_ACCURACY, false, Parameters.NUMBER_SPACING).PadCenter(Parameters.NUMBER_SPACING);
				for (int j = 0; j < numberStr.Length; j++)
					frameBuffer[position + j] = new ConsoleExtensions.CharInfo(numberStr[j], _statsHeaderValues[i].ForegroundColor, _statsHeaderValues[i].BackgroundColor);
				position += numberStr.Length;
			}

			if (Parameters.PERF_GRAPH_ENABLE && this._engine.StepEval_Render.ExclusiveTime.NumUpdates > 0)
				this.Graph.DrawFpsGraph(frameBuffer, this._engine.Renderer.FrameTimings, this._engine.Renderer.FpsTimings);
		}

		private void RefreshStatsHeader(bool wasPunctual) {
			if (this._engine.Renderer.FpsTimings.NumUpdates > 0)
				_statsHeaderValues[0] = new("FPS",
					1d / this._engine.Renderer.FpsTimings.Current.TotalSeconds,
					ChooseFrameIntervalColor(this._engine.Renderer.FpsTimings.LastUpdate.TotalMilliseconds),
					ConsoleColor.Black);
			else _statsHeaderValues[0] = new("FPS", 0, ConsoleColor.DarkGray, ConsoleColor.Black);
			if (this._engine.Renderer.FrameTimings.NumUpdates > 0)
				_statsHeaderValues[1] = new("Time(ms)",
					this._engine.Renderer.FrameTimings.Current.TotalMilliseconds,
					ChooseFrameIntervalColor(this._engine.Renderer.FrameTimings.LastUpdate.TotalMilliseconds),
					ConsoleColor.Black);
			else _statsHeaderValues[1] = new("Time(ms)", 0, ConsoleColor.DarkGray, ConsoleColor.Black);

			if (Parameters.PERF_STATS_ENABLE) {
				string label;
				for (int i = 0; i < this._engine.Evaluators.Length; i++) {
					label = this._engine.Evaluators[i].Name[0].ToString();
					if (!wasPunctual && this._engine.Evaluators[i].Id != this._engine.StepEval_Render.Id && this._engine.Evaluators[i].IsComputing)
						_statsHeaderValues[i + 2] = new(label,
							DateTime.UtcNow.Subtract(this._engine.Evaluators[i].LastComputeStartUtc.Value).TotalMilliseconds,
							ConsoleColor.White,
							ConsoleColor.DarkRed);
					else if (this._engine.Evaluators[i].ExclusiveTime.NumUpdates > 0)
						_statsHeaderValues[i + 2] = new(label,
							this._engine.Evaluators[i].ExclusiveTime.LastUpdate.TotalMilliseconds,
							ChooseFrameIntervalColor(this._engine.Evaluators[i].ExclusiveTime.Current.TotalMilliseconds),
							ConsoleColor.Black);
					else _statsHeaderValues[i + 2] = new(label, 0, ConsoleColor.DarkGray, ConsoleColor.Black);
				}
			}
		}

		private struct HeaderValue {
			public readonly string Label;
			public readonly double Value;
			public readonly ConsoleColor ForegroundColor;
			public readonly ConsoleColor BackgroundColor;
			public HeaderValue(string label, double value, ConsoleColor fg, ConsoleColor bg) {
				this.Label = label;
				this.Value = value;
				this.ForegroundColor = fg;
				this.BackgroundColor = bg;
			}

			public override string ToString() => string.Format("{0} {1}", this.Label, this.Value);
		}

		private ConsoleColor ChooseFrameIntervalColor(double timeMs) {
			double ratioToDesired = 1000d / (Parameters.TARGET_FPS > 0f ? Parameters.TARGET_FPS : Parameters.TARGET_FPS_DEFAULT) / timeMs;
			for (int i = 0; i < ColoringScales.RatioColors.Length; i++)
				if (ratioToDesired >= ColoringScales.RatioColors[i].Item1)
					return ColoringScales.RatioColors[i].Item2;
			return ConsoleColor.White;
		}
	}
}