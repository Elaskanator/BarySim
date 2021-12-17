using System;
using System.Linq;
using Generic.Extensions;
using Generic.Models;
using ParticleSimulator.Threading;

namespace ParticleSimulator.Simulation {
	internal static class PerfMon {
		public static int GraphWidth;

		private static SampleSMA _frameTimingMs = new SampleSMA(Parameters.PERF_SMA_ALPHA);
		private static SampleSMA _fpsTimingMs = new SampleSMA(Parameters.PERF_SMA_ALPHA);
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
					? 1 + Program.Manager.Evaluators.Count()
					: 0)];

			int width =
				Parameters.PERF_STATS_ENABLE
					? Parameters.GRAPH_WIDTH > 0
						? Parameters.GRAPH_WIDTH
						: 3 + Parameters.NUMBER_SPACING
							+ (2 + Program.Manager.Evaluators.Count())
								* (1 + Parameters.NUMBER_SPACING)
					: Parameters.PERF_GRAPH_DEFAULT_WIDTH;
			GraphWidth = Console.WindowWidth > width ? width : Console.WindowWidth;

			_columnFrameTimeStatsMs = new StatsInfo[GraphWidth];
			_columnFpsStatsMs = new StatsInfo[GraphWidth];
			_graphColumns = new ConsoleExtensions.CharInfo[GraphWidth][];
		}
		
		public static void TitleUpdate(object[] parameters = null) {
			string result = string.Format("{0} Simulator {1}D - ",
				Parameters.SIM_TYPE,
				Parameters.DIM);

			if (Program.Resource_Locations is null || Program.Resource_Locations.Current is null) {
				result += Program.Simulator.Particles.Count().Pluralize("Particle");
			} else {
				ParticleData[] activeParticles = (ParticleData[])Program.Resource_Locations.Current;
				result += string.Format("{0}/{1}",
					activeParticles.Length.Pluralize("Particle"),
					activeParticles.Count(p => p.IsVisible));
				if (_fpsTimingMs.NumUpdates > 0)
					result += string.Format("({0} fps)", (1000d / _fpsTimingMs.Current).ToStringBetter(2, false));
			}

			Console.Title = result;
		}

		public static void AfterRasterize(StepEvaluator result) {
			int frameIdx = (result.NumCompleted - 1) % Parameters.PERF_GRAPH_FRAMES_PER_COLUMN;
			lock (_columnStatsLock) {
				if (frameIdx == 0) {
					_currentColumnFpsDataMs = new double[Parameters.PERF_GRAPH_FRAMES_PER_COLUMN];
					_currentColumnFrameTimeDataMs = new double[Parameters.PERF_GRAPH_FRAMES_PER_COLUMN];
					_graphColumns = _graphColumns.ShiftRight(false);
					_columnFpsStatsMs = _columnFpsStatsMs.ShiftRight(false);
					_columnFrameTimeStatsMs = _columnFrameTimeStatsMs.ShiftRight(false);
				}
				double currentFrameTimeMs = (
					new double[] {
						Program.StepEval_Simulate.Step.DataAssimilationTicksAverager.LastUpdate + Program.StepEval_Simulate.Step.ExclusiveTicksAverager.LastUpdate,
						Program.StepEval_Resample.Step.ExclusiveTicksAverager.LastUpdate,
						Program.StepEval_Rasterize.Step.ExclusiveTicksAverager.LastUpdate,
						Program.StepEval_Draw.Step.ExclusiveTicksAverager.LastUpdate,
					}.Max()
				) / 10000d;
				double currentIterationTimeMs =
					new double[] {
						Program.StepEval_Rasterize.Step.IterationTicksAverager.LastUpdate,
						Program.StepEval_Draw.Step.ExclusiveTicksAverager.LastUpdate,
					}.Max()
					/ 10000d;

				_fpsTimingMs.Update(currentIterationTimeMs);
				_currentColumnFpsDataMs[frameIdx] = currentIterationTimeMs;
				_columnFpsStatsMs[0] = new StatsInfo(_currentColumnFpsDataMs.Take(frameIdx + 1));

				_frameTimingMs.Update(currentFrameTimeMs);
				_currentColumnFrameTimeDataMs[frameIdx] = currentFrameTimeMs;
				_columnFrameTimeStatsMs[0] = new StatsInfo(_currentColumnFrameTimeDataMs.Take(frameIdx + 1));

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
						raw = Program.Manager.Evaluators[i].Step.ExclusiveTicksAverager.Current / 10000d;
						smoothed = Program.Manager.Evaluators[i].Step.ExclusiveTicksAverager.LastUpdate / 10000d;
						_statsHeaderValues[i + 2] = new(label, smoothed, ChooseFrameIntervalColor(raw), ConsoleColor.Black);
					} else _statsHeaderValues[i + 2] = new(label, 0, ConsoleColor.DarkGray, ConsoleColor.Black);
				}
			}
		}

		private static void DrawFpsGraph(ConsoleExtensions.CharInfo[] frameBuffer) {
			if (Program.StepEval_Rasterize.Step.IterationTicksAverager.NumUpdates > 0) {
				ConsoleExtensions.CharInfo[][] graphColumnsCopy;
				lock (_columnStatsLock) {
					if (_columnFrameTimeStatsMs[0] is null)
						return;
					else if (_graphColumns[0] is null || DateTime.UtcNow.Subtract(_lastGraphRenderFrameUtc).TotalMilliseconds >= Parameters.PERF_GRAPH_REFRESH_MS)
						RerenderGraph();
					graphColumnsCopy = _graphColumns.TakeUntil(s => s is null).ToArray();
				}
			
				ConsoleExtensions.CharInfo[] result = new ConsoleExtensions.CharInfo[GraphWidth * Parameters.GRAPH_HEIGHT];

				for (int i = 0; i < graphColumnsCopy.Length; i++)
					DrawGraphColumn(result, graphColumnsCopy[i], i);

				double
					frameTime = _frameTimingMs.Current,
					fpsTime = _fpsTimingMs.Current;
				string
					label_min = _currentMin < 1000 ? _currentMin.ToStringBetter(2, false) + "ms" : (_currentMin / 1000).ToStringBetter(2, false) + "s",
					label_max = _currentMax < 1000 ? _currentMax.ToStringBetter(2, false) + "ms" : (_currentMax / 1000).ToStringBetter(2, false) + "s",
					label_frameTime = frameTime < 1000 ? frameTime.ToStringBetter(2, false): (frameTime / 1000).ToStringBetter(2, false),
					label_FpsTime = fpsTime < 1000 ? fpsTime.ToStringBetter(2, false) : (fpsTime / 1000).ToStringBetter(2, false);

				for (int i = 0; i < label_max.Length; i++)
					result[i] = new ConsoleExtensions.CharInfo(label_max[i], ConsoleColor.Gray);
				for (int i = 0; i < label_min.Length; i++)
					result[i + GraphWidth * (Parameters.GRAPH_HEIGHT - 1)] = new ConsoleExtensions.CharInfo(label_min[i], ConsoleColor.Gray);

				int offset_fps = (int)(Parameters.GRAPH_HEIGHT * (fpsTime - _currentMin) / (_currentMax - _currentMin));
				offset_fps = offset_fps < 0 ? 0 : offset_fps < Parameters.GRAPH_HEIGHT ? offset_fps : Parameters.GRAPH_HEIGHT - 1;
				int offset_frameTime = (int)(Parameters.GRAPH_HEIGHT * (frameTime - _currentMin) / (_currentMax - _currentMin));
				offset_frameTime = offset_frameTime < 0 ? 0 : offset_frameTime < Parameters.GRAPH_HEIGHT ? offset_frameTime : Parameters.GRAPH_HEIGHT - 1;
			
				bool drawFps = true, drawFrameTime = true;
				if (offset_frameTime == offset_fps)
					if (frameTime < fpsTime)
						drawFps = false;
					else drawFrameTime = false;

				if (drawFps)
					for (int i = 0; i < label_FpsTime.Length; i++)
						result[i + GraphWidth * (Parameters.GRAPH_HEIGHT - 1 - offset_fps)] = new ConsoleExtensions.CharInfo(label_FpsTime[i], ConsoleColor.Green);
				if (drawFrameTime)
					for (int i = 0; i < label_frameTime.Length; i++)
						result[i + GraphWidth * (Parameters.GRAPH_HEIGHT - 1 - offset_frameTime)] = new ConsoleExtensions.CharInfo(label_frameTime[i], ConsoleColor.Cyan);

				frameBuffer.RegionMerge(Parameters.WINDOW_WIDTH, result, GraphWidth, 0, 1, true);
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
					max = rangeStats.GetPercentileValue(100d - Parameters.PERF_GRAPH_PERCENTILE_CUTOFF);

				min = min <= frameTimeStats.Concat(iterationTimeStats).Min(s => s.GetPercentileValue(50d)) ? min : frameTimeStats.Concat(iterationTimeStats).Min(s => s.GetPercentileValue(50d));
				max = max >= frameTimeStats.Concat(iterationTimeStats).Max(s => s.GetPercentileValue(50d)) ? max : frameTimeStats.Concat(iterationTimeStats).Max(s => s.GetPercentileValue(50d));

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
		private static ConsoleExtensions.CharInfo[] RenderGraphColumn(StatsInfo fpsStats, StatsInfo frameTimeStats) {
			ConsoleExtensions.CharInfo[] result = new ConsoleExtensions.CharInfo[Parameters.GRAPH_HEIGHT];

			double
				yFps000 = fpsStats.Min,
				yFps010 = fpsStats.GetPercentileValue(10d),
				yFps025 = fpsStats.GetPercentileValue(25d),
				yFps040 = fpsStats.GetPercentileValue(40d),
				yFps050 = fpsStats.GetPercentileValue(50d),
				yFps060 = fpsStats.GetPercentileValue(60d),
				yFps075 = fpsStats.GetPercentileValue(75d),
				yFps090 = fpsStats.GetPercentileValue(90d),
				yFps100 = fpsStats.Max,
				yTime000 = frameTimeStats.Min,
				yTime010 = frameTimeStats.GetPercentileValue(10d),
				yTime025 = frameTimeStats.GetPercentileValue(25d),
				yTime040 = frameTimeStats.GetPercentileValue(40d),
				yTime050 = frameTimeStats.GetPercentileValue(50d),
				yTime060 = frameTimeStats.GetPercentileValue(60d),
				yTime075 = frameTimeStats.GetPercentileValue(75d),
				yTime090 = frameTimeStats.GetPercentileValue(90d),
				yTime100 = frameTimeStats.Max;

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
				yTime100Scaled = Parameters.GRAPH_HEIGHT * (yTime100 - _currentMin) / (_currentMax - _currentMin);
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