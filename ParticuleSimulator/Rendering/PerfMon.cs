﻿using System;
using System.Collections.Generic;
using System.Linq;
using Generic.Extensions;
using Generic.Models;
using ParticleSimulator.Threading;

namespace ParticleSimulator.Rendering {
	internal static class PerfMon {
		public static int GraphWidth;

		private static int _framesSimulated = 0;
		private static int _framesCompleted = 0;
		private static SimpleExponentialMovingAverage _frameTimingMs = new SimpleExponentialMovingAverage(Parameters.PERF_SMA_ALPHA);
		private static SimpleExponentialMovingAverage _fpsTimingMs = new SimpleExponentialMovingAverage(Parameters.PERF_SMA_ALPHA);
		private static double[] _currentColumnFrameTimeDataMs;
		private static double[] _currentColumnFpsDataMs;
		private static double _currentMin = 0;
		private static double _currentMax = 0;
		private static StatsInfo[] _columnFrameTimeStatsMs;
		private static StatsInfo[] _columnFpsStatsMs;
		private static ConsoleExtensions.CharInfo[][] _graphColumns;
		private static DateTime _lastGraphRenderFrameUtc = DateTime.UtcNow;
		private static Tuple<string, double, ConsoleColor, ConsoleColor>[] _statsHeaderValues;

		private static readonly object _columnStatsLock = new();

		public static void Init() {
			_statsHeaderValues = new Tuple<string, double, ConsoleColor, ConsoleColor>[
				1 + (Parameters.PERF_STATS_ENABLE
					? 1 + Program.Manager.Evaluators.Length
					: 0)];

			int width =
				Parameters.PERF_STATS_ENABLE
					? Parameters.GRAPH_WIDTH > 0
						? Parameters.GRAPH_WIDTH
						: 3 + Parameters.NUMBER_SPACING
							+ (2 + Program.Manager.Evaluators.Length)
								* (1 + Parameters.NUMBER_SPACING)
					: Parameters.PERF_GRAPH_DEFAULT_WIDTH;
			GraphWidth = Console.WindowWidth > width ? width : Console.WindowWidth;

			_columnFrameTimeStatsMs = new StatsInfo[GraphWidth];
			_columnFpsStatsMs = new StatsInfo[GraphWidth];
			_graphColumns = new ConsoleExtensions.CharInfo[GraphWidth][];
		}
		
		public static void TitleUpdate(object[] parameters = null) {
			string result = string.Format("Baryon Simulator {0}D - ", Parameters.DIM);

			if (Program.Resource_Locations is null || Program.Resource_Locations.Current is null) {
				result += Program.Simulator.ParticleTree.Count.Pluralize("Particle");
			} else {
				IEnumerable<ParticleData> activeParticles = (IEnumerable<ParticleData>)Program.Resource_Locations.Current;
				result += string.Format("{0}/{1}",
					activeParticles.Count(),
					activeParticles.Count(p => p.IsVisible).Pluralize("Particle"));
				if (_fpsTimingMs.NumUpdates > 0)
					result += string.Format(" ({0} fps)", (1000d / _fpsTimingMs.Current).ToStringBetter(2, false));
			}

			Console.Title = result;
		}

		public static void AfterRender(StepEvaluator result) {
			if (result.IsPunctual.Value && _framesSimulated < Program.StepEval_Simulate.NumCompleted) {
				_framesSimulated = Program.StepEval_Simulate.NumCompleted;
				int frameIdx = _framesCompleted++ % Parameters.PERF_GRAPH_FRAMES_PER_COLUMN;
				lock (_columnStatsLock) {
					if (frameIdx == 0) {
						_currentColumnFpsDataMs = new double[Parameters.PERF_GRAPH_FRAMES_PER_COLUMN];
						_currentColumnFrameTimeDataMs = new double[Parameters.PERF_GRAPH_FRAMES_PER_COLUMN];
						_graphColumns = _graphColumns.RotateRight();
						_columnFpsStatsMs = _columnFpsStatsMs.RotateRight();
						_columnFrameTimeStatsMs = _columnFrameTimeStatsMs.RotateRight();
					}
					double currentFpsTimeMs = new double[] {
						result.Step.SynchronizationTicksAverager.LastUpdate + result.Step.ExclusiveTicksAverager.LastUpdate,
						Program.StepEval_Simulate.Step.ExclusiveTicksAverager.LastUpdate
					}.Max() / Parameters.TICKS_PER_MS;
					double currentFrameTimeMs = Program.StepEval_Simulate.Step.ExclusiveTicksAverager.LastUpdate / Parameters.TICKS_PER_MS;

					_fpsTimingMs.Update(currentFpsTimeMs);
					_currentColumnFpsDataMs[frameIdx] = currentFpsTimeMs;
					_columnFpsStatsMs[0] = new StatsInfo(_currentColumnFpsDataMs.Take(frameIdx + 1));

					_frameTimingMs.Update(currentFrameTimeMs);
					_currentColumnFrameTimeDataMs[frameIdx] = currentFrameTimeMs;
					_columnFrameTimeStatsMs[0] = new StatsInfo(_currentColumnFrameTimeDataMs.Take(frameIdx + 1));
				}
			}
		}

		public static void DrawStatsOverlay(ConsoleExtensions.CharInfo[] frameBuffer, bool isSlow) {
			RefreshStatsHedaer(isSlow);

			int position = 0;
			string numberStr;
			for (int i = 0; i < _statsHeaderValues.Length; i++) {
				for (int j = 0; j < _statsHeaderValues[i].Item1.Length; j++)
					frameBuffer[position + j] = new ConsoleExtensions.CharInfo(_statsHeaderValues[i].Item1[j], ConsoleColor.White);
				position += _statsHeaderValues[i].Item1.Length;
				numberStr = _statsHeaderValues[i].Item2.ToStringBetter(Parameters.NUMBER_ACCURACY, false, Parameters.NUMBER_SPACING).PadCenter(Parameters.NUMBER_SPACING);
				for (int j = 0; j < numberStr.Length; j++)
					frameBuffer[position + j] = new ConsoleExtensions.CharInfo(numberStr[j], _statsHeaderValues[i].Item3, _statsHeaderValues[i].Item4);
				position += numberStr.Length;
			}

			if (Parameters.PERF_GRAPH_ENABLE) DrawFpsGraph(frameBuffer);
		}

		private static void RefreshStatsHedaer(bool isSlow) {
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
					label = Program.Manager.Evaluators[i].Step.Name[0].ToString();
					if (isSlow && Program.Manager.Evaluators[i].Id != Program.StepEval_Draw.Id && Program.Manager.Evaluators[i].IsComputing) {
						_statsHeaderValues[i + 2] = new(label, DateTime.UtcNow.Subtract(Program.Manager.Evaluators[i].IterationReceiveUtc.Value).TotalMilliseconds, ConsoleColor.White, ConsoleColor.DarkRed);
					} else if (Program.Manager.Evaluators[i].Step.ExclusiveTicksAverager.NumUpdates > 0) {
						raw = Program.Manager.Evaluators[i].Step.ExclusiveTicksAverager.Current / Parameters.TICKS_PER_MS;
						smoothed = Program.Manager.Evaluators[i].Step.ExclusiveTicksAverager.LastUpdate / Parameters.TICKS_PER_MS;
						_statsHeaderValues[i + 2] = new(label, smoothed, ChooseFrameIntervalColor(raw), ConsoleColor.Black);
					} else _statsHeaderValues[i + 2] = new(label, 0, ConsoleColor.DarkGray, ConsoleColor.Black);
				}
			}
		}

		private static void DrawFpsGraph(ConsoleExtensions.CharInfo[] frameBuffer) {
			if (Program.StepEval_Draw.Step.IterationTicksAverager.NumUpdates > 0) {
				ConsoleExtensions.CharInfo[][] graphColumnsCopy;
				lock (_columnStatsLock) {
					if (_columnFrameTimeStatsMs[0] is null)
						return;
					else if (_graphColumns[0] is null || DateTime.UtcNow.Subtract(_lastGraphRenderFrameUtc).TotalMilliseconds >= Parameters.PERF_GRAPH_REFRESH_MS)
						RerenderGraph();
					graphColumnsCopy = _graphColumns.TakeUntil(s => s is null).ToArray();
				}
			
				ConsoleExtensions.CharInfo[] graphData = new ConsoleExtensions.CharInfo[GraphWidth * Parameters.GRAPH_HEIGHT];

				for (int i = 0; i < graphColumnsCopy.Length; i++)
					DrawGraphColumn(graphData, graphColumnsCopy[i], i);

				double
					frameTime = _frameTimingMs.Current,
					fpsTime = _fpsTimingMs.Current,
					targetTime = 1000d / (Parameters.TARGET_FPS > 0 ? Parameters.TARGET_FPS : Parameters.MAX_FPS);
				string
					label_min = _currentMin < 1000 ? _currentMin.ToStringBetter(2, false) : (_currentMin / 1000).ToStringBetter(2, false),
					label_target = targetTime < 1000 ? targetTime.ToStringBetter(3, true): (targetTime / 1000).ToStringBetter(3, true),
					label_max = _currentMax < 1000 ? _currentMax.ToStringBetter(2, false) : (_currentMax / 1000).ToStringBetter(2, false),
					label_frameTime = frameTime < 1000 ? frameTime.ToStringBetter(2, false): (frameTime / 1000).ToStringBetter(2, false),
					label_FpsTime = fpsTime < 1000 ? fpsTime.ToStringBetter(3, false) + "ms" : (fpsTime / 1000).ToStringBetter(3, false) + "s";
				
				int yOffset;
				Func<int, int, ConsoleColor> colorResolver = (x, yOffset) =>
					graphData[x + GraphWidth * (Parameters.GRAPH_HEIGHT - 1 - yOffset)].ForegroundColor;

				int offset_fps = (int)(Parameters.GRAPH_HEIGHT * (fpsTime - _currentMin) / (_currentMax - _currentMin));
				offset_fps = offset_fps < 0 ? 0 : offset_fps < Parameters.GRAPH_HEIGHT ? offset_fps : Parameters.GRAPH_HEIGHT - 1;
				int offset_targetTime = (int)(Parameters.GRAPH_HEIGHT * (targetTime - _currentMin) / (_currentMax - _currentMin));
				int offset_frameTime = (int)(Parameters.GRAPH_HEIGHT * (frameTime - _currentMin) / (_currentMax - _currentMin));
				offset_frameTime = offset_frameTime < 0 ? 0 : offset_frameTime < Parameters.GRAPH_HEIGHT ? offset_frameTime : Parameters.GRAPH_HEIGHT - 1;
			
				bool drawFps = true,
					drawFrameTime = true,
					drawtargetTime = offset_targetTime != offset_frameTime && offset_targetTime != offset_fps
						&& offset_targetTime >= 0 && offset_targetTime < Parameters.GRAPH_HEIGHT,
					drawMinTime = 0 != offset_frameTime && 0 != offset_fps && 0 != offset_targetTime,
					drawMaxTime = Parameters.GRAPH_HEIGHT - 1 != offset_frameTime && Parameters.GRAPH_HEIGHT - 1 != offset_fps && Parameters.GRAPH_HEIGHT - 1 != offset_targetTime;
				if (offset_frameTime == offset_fps)
					if (frameTime < fpsTime)
						drawFps = false;
					else drawFrameTime = false;

				if (drawMinTime) for (int x = 0; x < label_min.Length; x++) {
					yOffset = 0;
					graphData[x + GraphWidth * (Parameters.GRAPH_HEIGHT - 1 - yOffset)] = new ConsoleExtensions.CharInfo(
						label_min[x], ConsoleColor.DarkBlue, colorResolver(x, yOffset));
				}

				if (drawFrameTime) for (int x = 0; x < label_frameTime.Length; x++) {
					yOffset = offset_frameTime;
					graphData[x + GraphWidth * (Parameters.GRAPH_HEIGHT - 1 - yOffset)] = new ConsoleExtensions.CharInfo(
						label_frameTime[x], ConsoleColor.DarkBlue, colorResolver(x, yOffset));
				}

				if (drawtargetTime) for (int x = 0; x < label_target.Length; x++) {
					yOffset = offset_targetTime;
					graphData[x + GraphWidth * (Parameters.GRAPH_HEIGHT - 1 - yOffset)] = new ConsoleExtensions.CharInfo(
						label_target[x], ConsoleColor.Black, ConsoleColor.DarkYellow);
				}

				if (drawFps) for (int x = 0; x < label_FpsTime.Length; x++) {
					yOffset = offset_fps;
					graphData[x + GraphWidth * (Parameters.GRAPH_HEIGHT - 1 - yOffset)] = new ConsoleExtensions.CharInfo(
						label_FpsTime[x], ConsoleColor.DarkGreen, ConsoleColor.Black);
				}

				if (drawMaxTime) for (int x = 0; x < label_max.Length; x++) {
					yOffset = Parameters.GRAPH_HEIGHT - 1;
					graphData[x + GraphWidth * (Parameters.GRAPH_HEIGHT - 1 - yOffset)] = new ConsoleExtensions.CharInfo(
						label_max[x], ConsoleColor.DarkBlue, colorResolver(x, yOffset));
				}

				frameBuffer.RegionMerge(Parameters.WINDOW_WIDTH, graphData, GraphWidth, 0, 1, true);
			}
		}

		private static void RerenderGraph() {
			StatsInfo[]
				frameTimeStats = _columnFrameTimeStatsMs.Without(s => s is null).ToArray(),
				iterationTimeStats = _columnFpsStatsMs.Without(s => s is null).ToArray();
			if (frameTimeStats.Length + iterationTimeStats.Length > 0) {
				StatsInfo rangeStats = new(frameTimeStats.Concat(iterationTimeStats).SelectMany(s => s.Data_asc));
			
				double
					min = rangeStats.GetPercentileValue(Parameters.PERF_GRAPH_PERCENTILE_CUTOFF),
					max = rangeStats.GetPercentileValue(100d - Parameters.PERF_GRAPH_PERCENTILE_CUTOFF),
					avg = frameTimeStats.Concat(iterationTimeStats).Average(s => s.GetPercentileValue(50d));

				min = min <= avg ? min : avg;
				max = max >= avg ? max : avg;

				min = min >= 1d ? min : 0f;
				max = max >= min ? max : min + 1d;

				if (Math.Abs(min - _currentMin) / _currentMin > 0.05d)
					_currentMin = min;
				if (Math.Abs(max - _currentMax) / _currentMax > 0.05d)
					_currentMax = max;

				for (int i = 0; i < frameTimeStats.Length || i < iterationTimeStats.Length; i++)
					_graphColumns[i] = RenderGraphColumn(_columnFpsStatsMs[i], _columnFrameTimeStatsMs[i]);

			}
			_lastGraphRenderFrameUtc = DateTime.UtcNow;
		}

		private static void DrawGraphColumn(ConsoleExtensions.CharInfo[] buffer, ConsoleExtensions.CharInfo[] newColumn, int xIdx) {
			for (int yIdx = 0; yIdx < Parameters.GRAPH_HEIGHT; yIdx++)
				if (!Equals(newColumn[yIdx], default(ConsoleExtensions.CharInfo)))
					buffer[xIdx + (Parameters.GRAPH_HEIGHT - yIdx - 1)*GraphWidth] = newColumn[yIdx];
		}
		private static ConsoleExtensions.CharInfo[] RenderGraphColumn(StatsInfo fpsStatsMs, StatsInfo simTimeStatsMs) {
			ConsoleExtensions.CharInfo[] result = new ConsoleExtensions.CharInfo[Parameters.GRAPH_HEIGHT];

			double
				yFps000 = fpsStatsMs.Min,
				yFps010 = fpsStatsMs.GetPercentileValue(10d),
				yFps025 = fpsStatsMs.GetPercentileValue(25d),
				yFps040 = fpsStatsMs.GetPercentileValue(40d),
				yFps050 = fpsStatsMs.GetPercentileValue(50d),
				yFps060 = fpsStatsMs.GetPercentileValue(60d),
				yFps075 = fpsStatsMs.GetPercentileValue(75d),
				yFps090 = fpsStatsMs.GetPercentileValue(90d),
				yFps100 = fpsStatsMs.Max,
				yTime000 = simTimeStatsMs.Min,
				yTime010 = simTimeStatsMs.GetPercentileValue(10d),
				yTime025 = simTimeStatsMs.GetPercentileValue(25d),
				yTime040 = simTimeStatsMs.GetPercentileValue(40d),
				yTime050 = simTimeStatsMs.GetPercentileValue(50d),
				yTime060 = simTimeStatsMs.GetPercentileValue(60d),
				yTime075 = simTimeStatsMs.GetPercentileValue(75d),
				yTime090 = simTimeStatsMs.GetPercentileValue(90d),
				yTime100 = simTimeStatsMs.Max;
			double fps = Parameters.TARGET_FPS > 0
				? Parameters.TARGET_FPS
				: Parameters.MAX_FPS;
			double
				yFps000Scaled = Parameters.GRAPH_HEIGHT * (yFps000 - _currentMin) / (_currentMax - _currentMin),
				yFps010Scaled = Parameters.GRAPH_HEIGHT * (yFps010 - _currentMin) / (_currentMax - _currentMin),
				yFps025Scaled = Parameters.GRAPH_HEIGHT * (yFps025 - _currentMin) / (_currentMax - _currentMin),
				yFps040Scaled = Parameters.GRAPH_HEIGHT * (yFps040 - _currentMin) / (_currentMax - _currentMin),
				yFps050Scaled = Parameters.GRAPH_HEIGHT * (yFps050 - _currentMin) / (_currentMax - _currentMin),
				yFps060Scaled = Parameters.GRAPH_HEIGHT * (yFps060 - _currentMin) / (_currentMax - _currentMin),
				yFps075Scaled = Parameters.GRAPH_HEIGHT * (yFps075 - _currentMin) / (_currentMax - _currentMin),
				yFps090Scaled = Parameters.GRAPH_HEIGHT * (yFps090 - _currentMin) / (_currentMax - _currentMin),
				yFps100Scaled = Parameters.GRAPH_HEIGHT * (yFps100 - _currentMin) / (_currentMax - _currentMin),
				yTime000Scaled = Parameters.GRAPH_HEIGHT * (yTime000 - _currentMin) / (_currentMax - _currentMin),
				yTime010Scaled = Parameters.GRAPH_HEIGHT * (yTime010 - _currentMin) / (_currentMax - _currentMin),
				yTime025Scaled = Parameters.GRAPH_HEIGHT * (yTime025 - _currentMin) / (_currentMax - _currentMin),
				yTime040Scaled = Parameters.GRAPH_HEIGHT * (yTime040 - _currentMin) / (_currentMax - _currentMin),
				yTime050Scaled = Parameters.GRAPH_HEIGHT * (yTime050 - _currentMin) / (_currentMax - _currentMin),
				yTime060Scaled = Parameters.GRAPH_HEIGHT * (yTime060 - _currentMin) / (_currentMax - _currentMin),
				yTime075Scaled = Parameters.GRAPH_HEIGHT * (yTime075 - _currentMin) / (_currentMax - _currentMin),
				yTime090Scaled = Parameters.GRAPH_HEIGHT * (yTime090 - _currentMin) / (_currentMax - _currentMin),
				yTime100Scaled = Parameters.GRAPH_HEIGHT * (yTime100 - _currentMin) / (_currentMax - _currentMin),
				yTargetTimeScaled = Parameters.GRAPH_HEIGHT * ((1000d / fps) - _currentMin) / (_currentMax - _currentMin);
			double
				y000Scaled = yFps000Scaled <= yTime000Scaled ? yFps000Scaled : yTime000Scaled,
				y100Scaled = yFps100Scaled >= yTime100Scaled ? yFps100Scaled : yTime100Scaled;
				
			ConsoleColor color; char chr;
			for (int yIdx = y000Scaled >= 0f ? (int)y000Scaled : 0; yIdx < (y100Scaled <= Parameters.GRAPH_HEIGHT ? y100Scaled : (double)Parameters.GRAPH_HEIGHT); yIdx++) {
				if (yIdx == (int)y000Scaled) {//bottom pixel
					if (y000Scaled % 1d >= 0.5d)//top half
						chr = Parameters.CHAR_TOP;
					else if (y100Scaled < yIdx + 0.5d)//bottom half
						chr = Parameters.CHAR_LOW;
					else chr = Parameters.CHAR_BOTH;
				} else if (yIdx == (int)y100Scaled) {//top pixel
					if (y100Scaled % 1d < 0.5d)//bottom half
						chr = Parameters.CHAR_LOW;
					else if (y000Scaled >= yIdx && y100Scaled >= yIdx + 0.5d)//top half
						chr = Parameters.CHAR_TOP;
					else chr = Parameters.CHAR_BOTH;
				} else chr = Parameters.CHAR_BOTH;

				if (yIdx == (int)yFps050Scaled)
					color = ConsoleColor.DarkGreen;
				else if (yIdx >= (int)yFps040Scaled && yIdx <= (int)yFps060Scaled)
					color = ConsoleColor.Green;

				else if (yIdx == (int)yTime050Scaled)
					color = ConsoleColor.DarkCyan;
				else if (yIdx >= (int)yTime040Scaled && yIdx <= (int)yTime060Scaled)
					color = ConsoleColor.Cyan;

				else if (fps > 0d && yIdx == (int)yTargetTimeScaled)
					color = ConsoleColor.DarkYellow;
				else if (fps > 0d && yIdx > yTargetTimeScaled && yIdx > (int)yTime100Scaled && yIdx < (int)yFps050Scaled)
					if (yIdx > (int)yFps040Scaled) color = ConsoleColor.DarkMagenta;
					else if (yIdx > (int)yFps025Scaled) color = ConsoleColor.Magenta;
					else if (yIdx > (int)yFps010Scaled) color = ConsoleColor.DarkRed;
					else color = ConsoleColor.Red;

				else if (yIdx >= (int)yFps025Scaled && yIdx <= (int)yFps075Scaled)
					color = ConsoleColor.White;
				else if (yIdx >= (int)yTime025Scaled && yIdx <= (int)yTime075Scaled)
					color = ConsoleColor.White;

				else if (yIdx >= (int)yFps010Scaled && yIdx <= (int)yFps090Scaled)
					color = ConsoleColor.Gray;
				else if (yIdx >= (int)yTime010Scaled && yIdx <= (int)yTime090Scaled)
					color = ConsoleColor.Gray;
				
				else if (yIdx >= (int)yFps000Scaled && yIdx <= (int)yFps100Scaled)
					color = ConsoleColor.DarkGray;
				else if (yIdx >= (int)yTime000Scaled && yIdx <= (int)yTime100Scaled)
					color = ConsoleColor.DarkGray;

				else color = ConsoleColor.Black;

				result[yIdx] = new ConsoleExtensions.CharInfo(chr, color, ConsoleColor.Black);
			}
			return result;
		}

		public static void WriteEnd() {
			TimeSpan totalDuration = Program.Manager.EndTimeUtc.Subtract(Program.Manager.StartTimeUtc);
			
			Console.SetCursorPosition(0, 1);
			Console.ForegroundColor = ConsoleColor.White;
			Console.BackgroundColor = ConsoleColor.Black;
			Console.WriteLine("---END--- Duration {0}s", Program.Manager.EndTimeUtc.Subtract(Program.Manager.StartTimeUtc).TotalSeconds.ToStringBetter(2));
			
			Console.Write("Evaluated {0}{1}",
				Program.StepEval_Simulate.NumCompleted.Pluralize("time"),
				Parameters.SIMULATION_SKIPS > 0
					? " and " + Program.StepEval_Rasterize.NumCompleted.Pluralize("rasters")
					: "");

			double fps = (double)Program.StepEval_Resample.NumCompleted / totalDuration.TotalSeconds;
			Console.ForegroundColor = ChooseFpsColor(fps);
			Console.Write(fps.ToStringBetter(4));
			Console.ForegroundColor = ConsoleColor.White;
			Console.Write(" FPS");

			if (Parameters.TARGET_FPS > 0f) {
				double expectedFramesRendered = Parameters.TARGET_FPS * (double)totalDuration.TotalSeconds;
				double fpsRatio = (double)Program.StepEval_Rasterize.NumCompleted / ((int)(1 + expectedFramesRendered));

				Console.Write(" ({0:G3}% of {1} fps)",
					100 * fpsRatio,
					Parameters.TARGET_FPS);
			}

			Console.WriteLine();
			Console.ResetColor();
		}

		private static ConsoleColor ChooseColor(double ratioToDesired) {
			foreach (Tuple<double, ConsoleColor> rank in ColoringScales.RatioColors) {
				if (ratioToDesired >= rank.Item1) return rank.Item2;
			}
			return ConsoleColor.White;
		}
		private static ConsoleColor ChooseFpsColor(double fps) {
			double ratioToDesired = fps / (Parameters.TARGET_FPS > 0f ? Parameters.TARGET_FPS : Parameters.TARGET_FPS_DEFAULT);
			return ChooseColor(ratioToDesired);
		}
		private static ConsoleColor ChooseFrameIntervalColor(double timeMs) {
			double ratioToDesired = 1000d / (Parameters.TARGET_FPS > 0f ? Parameters.TARGET_FPS : Parameters.TARGET_FPS_DEFAULT) / timeMs;
			return ChooseColor(ratioToDesired);
		}
	}
}