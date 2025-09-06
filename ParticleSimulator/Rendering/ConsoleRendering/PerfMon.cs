using System;
using Generic.Extensions;
using ParticleSimulator.Engine;
using ParticleSimulator.Engine.Threading;

namespace ParticleSimulator.Rendering.SystemConsole {
	public class PerfMon {
		public PerfMon(ARenderer renderer) {
			this._engine = renderer.Engine;
		}

		public PerfGraph Graph { get; private set; }
		public int HeaderWidth { get; private set; }
		
		private readonly RenderEngine _engine;
		private HeaderValue[] _statsHeaderValues;

		public void Init() {
			this._statsHeaderValues = new HeaderValue[1 + this._engine.Evaluators.Length];
			this.HeaderWidth = (this._engine.Evaluators.Length * (1 + Parameters.MON_NUMBER_SPACING)) + 8;
			this.Graph = new PerfGraph(this.HeaderWidth);
		}

		public void DrawStatsOverlay(EvalResult prepResults, ConsoleExtensions.CharInfo[] frameBuffer) {
			this.RefreshStatsHeader(prepResults);

			int position = 0;
			string numberStr;
			for (int i = 0; i < _statsHeaderValues.Length; i++) {
				for (int j = 0; j < _statsHeaderValues[i].Label.Length; j++)
					frameBuffer[position + j] = new ConsoleExtensions.CharInfo(_statsHeaderValues[i].Label[j], ConsoleColor.Gray);
				position += _statsHeaderValues[i].Label.Length;
				numberStr = _statsHeaderValues[i].Value.ToStringBetter(Parameters.MON_NUMBER_ACCURACY, false, Parameters.MON_NUMBER_SPACING).PadCenter(Parameters.MON_NUMBER_SPACING);
				for (int j = 0; j < numberStr.Length; j++)
					frameBuffer[position + j] = new ConsoleExtensions.CharInfo(numberStr[j], _statsHeaderValues[i].ForegroundColor, _statsHeaderValues[i].BackgroundColor);
				position += numberStr.Length;
			}

			this.Graph.DrawFpsGraph(frameBuffer, this._engine.Renderer.SimTimings, this._engine.Renderer.FpsTimings);
		}

		private void RefreshStatsHeader(EvalResult prepResults) {
			if (this._engine.Renderer.FpsTimings.NumUpdates > 0)
				_statsHeaderValues[0] = new("FPS",
					1d / this._engine.Renderer.FpsTimings.Current.TotalSeconds,
					ChooseFrameIntervalColor(this._engine.Renderer.FpsTimings.LastUpdate.TotalMilliseconds),
					ConsoleColor.Black);
			else _statsHeaderValues[0] = new("FPS", 0, ConsoleColor.DarkGray, ConsoleColor.Black);

			string label;
			TimeSpan duration;
			for (int i = 0; i < this._engine.Evaluators.Length; i++) {
				label = this._engine.Evaluators[i].Name[0].ToString();
				duration = TimeSpan.Zero;

				if (this._engine.Evaluators[i].IsComputing) {
					duration = DateTime.UtcNow.Subtract(this._engine.Evaluators[i].LastIterationStartUtc ?? this._engine.StartTimeUtc.Value);
					duration = this._engine.Evaluators[i].ExclusiveTime.NumUpdates > 0 && this._engine.Evaluators[i].ExclusiveTime.LastUpdate > duration
						? this._engine.Evaluators[i].ExclusiveTime.LastUpdate
						: duration;
				} else if (this._engine.Evaluators[i].ExclusiveTime.NumUpdates > 0)
					duration = this._engine.Evaluators[i].ExclusiveTime.LastUpdate;

				if (duration.Ticks == 0L)
					_statsHeaderValues[i + 1] = new(label, 0, ConsoleColor.DarkGray, ConsoleColor.Black);
				else if (duration.TotalMilliseconds >= Parameters.MON_WARN_MS)
					if (this._engine.Evaluators[i].IsComputing)
						_statsHeaderValues[i + 1] = new(label,
							duration.TotalMilliseconds,
							ConsoleColor.White,
							ConsoleColor.DarkRed);
					else _statsHeaderValues[i + 1] = new(label,
							duration.TotalMilliseconds,
							ConsoleColor.White,
							ConsoleColor.DarkYellow);
				else _statsHeaderValues[i + 1] = new(label,
						duration.TotalMilliseconds,
						ChooseFrameIntervalColor(this._engine.Evaluators[i].ExclusiveTime.Current.TotalMilliseconds),
						ConsoleColor.Black);
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
			double ratioToDesired = 1000d / (Parameters.TARGET_FPS > 0f ? Parameters.TARGET_FPS : Parameters.MON_FPS_DEFAULT) / timeMs;
			for (int i = 0; i < ColoringScales.RatioColors.Length; i++)
				if (ratioToDesired >= ColoringScales.RatioColors[i].Item1)
					return ColoringScales.RatioColors[i].Item2;
			return ConsoleColor.White;
		}
	}
}