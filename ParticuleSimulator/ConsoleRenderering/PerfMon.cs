using System;
using System.Collections.Generic;
using System.Linq;
using Generic.Extensions;
using Generic.Models;
using ParticleSimulator.Engine;

namespace ParticleSimulator.ConsoleRendering {
	public class PerfMon {
		public PerfMon() {
			_statsHeaderValues = new HeaderValue[
				1 + (Parameters.PERF_STATS_ENABLE
					? 1 + Program.Manager.Evaluators.Length
					: 0)];
			this._graph = new PerfGraph();
		}
		
		private PerfGraph _graph;
		private HeaderValue[] _statsHeaderValues;
		private SimpleExponentialMovingAverage _frameTimingMs = new SimpleExponentialMovingAverage(Parameters.PERF_SMA_ALPHA);
		private SimpleExponentialMovingAverage _fpsTimingMs = new SimpleExponentialMovingAverage(Parameters.PERF_SMA_ALPHA);
		private int _framesCompleted = 0;

		public void AfterRender(bool wasPunctual) {
			if (wasPunctual) {
				int frameIdx = _framesCompleted++ % Parameters.PERF_GRAPH_FRAMES_PER_COLUMN;

				double currentFpsTimeMs = new TimeSpan[] {
					Program.StepEval_Render.Synchronizer.LastSyncDuration.Value + Program.StepEval_Render.ExclusiveTimeTicks.LastUpdate,
					Program.StepEval_Simulate.ExclusiveTimeTicks.LastUpdate
				}.Max().TotalMilliseconds;
				double currentFrameTimeMs = Program.StepEval_Simulate.ExclusiveTimeTicks.LastUpdate.TotalMilliseconds;

				_fpsTimingMs.Update(currentFpsTimeMs);
				_frameTimingMs.Update(currentFrameTimeMs);
				this._graph.Update(frameIdx, currentFpsTimeMs, currentFrameTimeMs);
			}
		}
		
		public void TitleUpdate(object[] parameters = null) {
			string result = string.Format("Baryon Simulator {0}D - ", Parameters.DIM);

			if (Program.Resource_ParticleData is null || Program.Resource_ParticleData.Current is null) {
				result += Program.Simulator.ParticleTree.Count.Pluralize("Particle");
			} else {
				IEnumerable<ParticleData> activeParticles = (IEnumerable<ParticleData>)Program.Resource_ParticleData.Current;
				result += string.Format("{0}/{1}",
					activeParticles.Count(),
					activeParticles.Count(p => p.IsVisible).Pluralize("Particle"));
				if (_fpsTimingMs.NumUpdates > 0)
					result += string.Format(" ({0} fps)", (1000d / _fpsTimingMs.Current).ToStringBetter(2, false));
			}

			Console.Title = result;
		}

		public void DrawStatsOverlay(ConsoleExtensions.CharInfo[] frameBuffer, bool isSlow) {
			RefreshStatsHedaer(isSlow);

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

			if (Parameters.PERF_GRAPH_ENABLE)
				this._graph.DrawFpsGraph(frameBuffer, this._frameTimingMs, this._fpsTimingMs);
		}

		private void RefreshStatsHedaer(bool isSlow) {
			double raw, smoothed;
			if (_fpsTimingMs.NumUpdates > 0) {
				raw = 1000d / _fpsTimingMs.LastUpdate;
				smoothed = 1000d / _fpsTimingMs.Current;
				_statsHeaderValues[0] = new("FPS", smoothed, ChooseFpsColor(raw), ConsoleColor.Black);
			} else _statsHeaderValues[0] = new("FPS", 0, ConsoleColor.DarkGray, ConsoleColor.Black);
			if (_frameTimingMs.NumUpdates > 0) {
				raw = _frameTimingMs.LastUpdate;
				smoothed = _frameTimingMs.Current;
				_statsHeaderValues[1] = new("Time(ms)", smoothed, ChooseFrameIntervalColor(raw), ConsoleColor.Black);
			} else _statsHeaderValues[1] = new("Time(ms)", 0, ConsoleColor.DarkGray, ConsoleColor.Black);

			if (Parameters.PERF_STATS_ENABLE) {
				string label;
				for (int i = 0; i < Program.Manager.Evaluators.Length; i++) {
					label = Program.Manager.Evaluators[i].Name[0].ToString();
					if (isSlow && Program.Manager.Evaluators[i].Id != Program.StepEval_Render.Id && Program.Manager.Evaluators[i].IsComputing) {
						_statsHeaderValues[i + 2] = new(label, DateTime.UtcNow.Subtract(Program.Manager.Evaluators[i].ComputeStartUtc.Value).TotalMilliseconds, ConsoleColor.White, ConsoleColor.DarkRed);
					} else if (Program.Manager.Evaluators[i].ExclusiveTimeTicks.NumUpdates > 0) {
						raw = Program.Manager.Evaluators[i].ExclusiveTimeTicks.Current.TotalMilliseconds;
						smoothed = Program.Manager.Evaluators[i].ExclusiveTimeTicks.LastUpdate.TotalMilliseconds;
						_statsHeaderValues[i + 2] = new(label, smoothed, ChooseFrameIntervalColor(raw), ConsoleColor.Black);
					} else _statsHeaderValues[i + 2] = new(label, 0, ConsoleColor.DarkGray, ConsoleColor.Black);
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
		}

		public void WriteEnd() {
			TimeSpan totalDuration = Program.Manager.EndTimeUtc.Value.Subtract(Program.Manager.StartTimeUtc.Value);
			
			Console.SetCursorPosition(0, 1);
			Console.ForegroundColor = ConsoleColor.White;
			Console.BackgroundColor = ConsoleColor.Black;
			Console.WriteLine("---END--- Duration {0}s", Program.Manager.EndTimeUtc.Value.Subtract(Program.Manager.StartTimeUtc.Value).TotalSeconds.ToStringBetter(2));
			
			Console.Write("Evaluated {0}{1}",
				Program.StepEval_Simulate.IterationCount.Pluralize("time"),
				Parameters.SIMULATION_SKIPS > 0
					? " and " + Program.StepEval_Rasterize.IterationCount.Pluralize("frame")
					: "");

			double fps = Program.StepEval_Rasterize.IterationCount / totalDuration.TotalSeconds;
			Console.ForegroundColor = ChooseFpsColor(fps);
			Console.Write(fps.ToStringBetter(4));
			Console.ForegroundColor = ConsoleColor.White;
			Console.Write(" FPS");

			if (Parameters.TARGET_FPS > 0f) {
				double expectedFramesRendered = Parameters.TARGET_FPS * (double)totalDuration.TotalSeconds;
				double fpsRatio = (double)Program.StepEval_Rasterize.IterationCount / ((int)(1 + expectedFramesRendered));

				Console.Write(" ({0:G3}% of {1} fps)",
					100 * fpsRatio,
					Parameters.TARGET_FPS);
			}

			Console.WriteLine();
			Console.ResetColor();
		}

		private ConsoleColor ChooseColor(double ratioToDesired) {
			foreach (Tuple<double, ConsoleColor> rank in ColoringScales.RatioColors) {
				if (ratioToDesired >= rank.Item1) return rank.Item2;
			}
			return ConsoleColor.White;
		}
		private ConsoleColor ChooseFpsColor(double fps) {
			double ratioToDesired = fps / (Parameters.TARGET_FPS > 0f ? Parameters.TARGET_FPS : Parameters.TARGET_FPS_DEFAULT);
			return ChooseColor(ratioToDesired);
		}
		private ConsoleColor ChooseFrameIntervalColor(double timeMs) {
			double ratioToDesired = 1000d / (Parameters.TARGET_FPS > 0f ? Parameters.TARGET_FPS : Parameters.TARGET_FPS_DEFAULT) / timeMs;
			return ChooseColor(ratioToDesired);
		}
	}
}