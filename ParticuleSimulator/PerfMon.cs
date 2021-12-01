using System;
using System.Linq;
using Generic.Extensions;
using Generic.Models;
using ParticleSimulator.Simulation;
using ParticleSimulator.Threading;

namespace ParticleSimulator {
	internal static class PerfMon {
		public static readonly int GraphWidth;

		private static SampleSMA _frameTimingMs = new SampleSMA(Parameters.PERF_SMA_ALPHA);
		private static SampleSMA _fpsTimingMs = new SampleSMA(Parameters.PERF_SMA_ALPHA);
		private static double[] _currentColumnFrameTimeDataMs;
		private static double[] _currentColumnIterationTimeDataMs;
		private static double _currentMin = 0;
		private static double _currentMax = 0;
		private static StatsInfo[] _columnFrameTimeStatsMs;
		private static StatsInfo[] _columnIterationTimeStatsMs;
		private static ConsoleExtensions.CharInfo[][] _graphColumns;
		private static DateTime _lastGraphRenderFrameUtc = DateTime.UtcNow;
		private static readonly object _columnStatsLock = new();

		private static readonly Tuple<string, double, ConsoleColor, ConsoleColor>[] _statsHeaderValues;

		static PerfMon() {
			_statsHeaderValues = new Tuple<string, double, ConsoleColor, ConsoleColor>[
				1 + (Parameters.PERF_STATS_ENABLE
					? Program.Manager.Evaluators.Count()
					: 0)];

			int width =
				Parameters.PERF_STATS_ENABLE
					? Parameters.GRAPH_WIDTH > 0
						? Parameters.GRAPH_WIDTH
						: 3 + Parameters.NUMBER_SPACING
							+ Program.Manager.Evaluators.Sum(s =>
								Parameters.NUMBER_SPACING
								+ 1)
					: Parameters.PERF_GRAPH_DEFAULT_WIDTH;
			GraphWidth = Console.WindowWidth > width ? width : Console.WindowWidth;

			_columnFrameTimeStatsMs = new StatsInfo[GraphWidth];
			_columnIterationTimeStatsMs = new StatsInfo[GraphWidth];
			_graphColumns = new ConsoleExtensions.CharInfo[GraphWidth][];
		}

		public static void AfterRasterize(StepEvaluator result) {
			int frameIdx = (result.NumCompleted - 1) % Parameters.PERF_GRAPH_FRAMES_PER_COLUMN;
			lock (_columnStatsLock) {
				if (frameIdx == 0) {
					_currentColumnFrameTimeDataMs = new double[Parameters.PERF_GRAPH_FRAMES_PER_COLUMN];
					_currentColumnIterationTimeDataMs = new double[Parameters.PERF_GRAPH_FRAMES_PER_COLUMN];
					_graphColumns = _graphColumns.ShiftRight(false);
					_columnFrameTimeStatsMs = _columnFrameTimeStatsMs.ShiftRight(false);
					_columnIterationTimeStatsMs = _columnIterationTimeStatsMs.ShiftRight(false);
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

				_frameTimingMs.Update(currentFrameTimeMs);
				_fpsTimingMs.Update(currentIterationTimeMs);

				_currentColumnFrameTimeDataMs[frameIdx] = currentFrameTimeMs;
				_columnFrameTimeStatsMs[0] = new StatsInfo(_currentColumnFrameTimeDataMs.Take(frameIdx + 1));

				_currentColumnIterationTimeDataMs[frameIdx] = currentIterationTimeMs;
				_columnIterationTimeStatsMs[0] = new StatsInfo(_currentColumnIterationTimeDataMs.Take(frameIdx + 1));
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
				numberStr = _statsHeaderValues[i].Item2.ToStringBetter(Parameters.NUMBER_ACCURACY, true, Parameters.NUMBER_SPACING).PadCenter(Parameters.NUMBER_SPACING);
				for (int j = 0; j < numberStr.Length; j++)
					frameBuffer[position + j] = new ConsoleExtensions.CharInfo(numberStr[j], _statsHeaderValues[i].Item3, _statsHeaderValues[i].Item4);
				position += numberStr.Length;
			}

			if (Parameters.PERF_GRAPH_ENABLE) DrawFpsGraph(frameBuffer);
		}

		private static void RefreshStatsHedaer(bool isSlow) {
			if (Program.StepEval_Rasterize.Step.IterationTicksAverager.NumUpdates > 0) {
				double
					fps = 10000000 / (Program.StepEval_Rasterize.Step.IterationTicksAverager.LastUpdate),
					smoothedFps = 10000000 / (Program.StepEval_Rasterize.Step.IterationTicksAverager.Current);
				_statsHeaderValues[0] = new("FPS", smoothedFps, ChooseFpsColor(fps), ConsoleColor.Black);
			} else _statsHeaderValues[0] = new("FPS", 0, ChooseFrameIntervalColor(0), ConsoleColor.Black);

			if (Parameters.PERF_STATS_ENABLE) {
				string label;
				double timeVal, colorVal;
				for (int i = 0; i < Program.Manager.Evaluators.Length; i++) {
					label = Program.Manager.Evaluators[i].Step.Name[0].ToString();
					timeVal = Program.Manager.Evaluators[i].Step.ExclusiveTicksAverager.Current / 10000d;
					colorVal = Program.Manager.Evaluators[i].Step.ExclusiveTicksAverager.LastUpdate / 10000d;
					if (isSlow && Program.Manager.Evaluators[i].Id != Program.StepEval_Draw.Id && Program.Manager.Evaluators[i].IsComputing)
						_statsHeaderValues[i + 1] = new(label, DateTime.UtcNow.Subtract(Program.Manager.Evaluators[i].IterationReceiveUtc.Value).TotalMilliseconds, ConsoleColor.White, ConsoleColor.DarkRed);
					else _statsHeaderValues[i + 1] = new(label, timeVal, ChooseFrameIntervalColor(colorVal), ConsoleColor.Black);
		}}}

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
					label_min = _currentMin < 1000 ? _currentMin.ToStringBetter(Parameters.PERF_GRAPH_NUMBER_ACCURACY, false) + "ms" : (_currentMin / 1000).ToStringBetter(Parameters.PERF_GRAPH_NUMBER_ACCURACY, false) + "s",
					label_max = _currentMax < 1000 ? _currentMax.ToStringBetter(Parameters.PERF_GRAPH_NUMBER_ACCURACY, false) + "ms" : (_currentMax / 1000).ToStringBetter(Parameters.PERF_GRAPH_NUMBER_ACCURACY, false) + "s",
					label_frameTime = frameTime < 1000 ? frameTime.ToStringBetter(Parameters.PERF_GRAPH_NUMBER_ACCURACY, true) + "ms" : (frameTime / 1000).ToStringBetter(Parameters.PERF_GRAPH_NUMBER_ACCURACY, true) + "s",
					label_FpsTime = fpsTime < 1000 ? fpsTime.ToStringBetter(Parameters.PERF_GRAPH_NUMBER_ACCURACY, true) + "ms" : (fpsTime / 1000).ToStringBetter(Parameters.PERF_GRAPH_NUMBER_ACCURACY, true) + "s";

				for (int i = 0; i < label_max.Length; i++)
					result[i] = new ConsoleExtensions.CharInfo(label_max[i], ConsoleColor.DarkGray);

				for (int i = 0; i < label_min.Length; i++)
					result[i + GraphWidth * (Parameters.GRAPH_HEIGHT - 1)] = new ConsoleExtensions.CharInfo(label_min[i], ConsoleColor.DarkGray);

				int offset_fps = (int)(Parameters.GRAPH_HEIGHT * (fpsTime - _currentMin) / (_currentMax - _currentMin));
				offset_fps = offset_fps < 0 ? 0 : offset_fps < Parameters.GRAPH_HEIGHT ? offset_fps : Parameters.GRAPH_HEIGHT - 1;
				int offset_frameTime = (int)(Parameters.GRAPH_HEIGHT * (frameTime - _currentMin) / (_currentMax - _currentMin));
				offset_frameTime = offset_frameTime < 0 ? 0 : offset_frameTime < Parameters.GRAPH_HEIGHT ? offset_frameTime : Parameters.GRAPH_HEIGHT - 1;
			
				for (int i = 0; i < label_FpsTime.Length; i++)
					result[i + GraphWidth * (Parameters.GRAPH_HEIGHT - 1 - offset_fps)] = new ConsoleExtensions.CharInfo(label_FpsTime[i], ConsoleColor.Green);

				if (offset_frameTime != offset_fps)
					for (int i = 0; i < label_frameTime.Length; i++)
						result[i + GraphWidth * (Parameters.GRAPH_HEIGHT - 1 - offset_frameTime)] = new ConsoleExtensions.CharInfo(label_frameTime[i], ConsoleColor.Cyan);
			
				frameBuffer.RegionMerge(Parameters.WINDOW_WIDTH, result, GraphWidth, 0, 1, true);
			}
		}

		private static void RerenderGraph() {
			StatsInfo[]
				frameTimeStats = _columnFrameTimeStatsMs.Without(s => s is null).ToArray(),
				iterationTimeStats = _columnIterationTimeStatsMs.Without(s => s is null).ToArray();
			if (frameTimeStats.Length + iterationTimeStats.Length > 0) {
				StatsInfo rangeStats = new(frameTimeStats.Concat(iterationTimeStats).SelectMany(s => s.Data_asc));
			
				double
					min = new StatsInfo(frameTimeStats.SelectMany(s => s.Data_asc)).GetPercentileValue(Parameters.PERF_GRAPH_PERCENTILE_CUTOFF),
					max = new StatsInfo(iterationTimeStats.SelectMany(s => s.Data_asc)).GetPercentileValue(100d - Parameters.PERF_GRAPH_PERCENTILE_CUTOFF);

				min = min <= frameTimeStats.Min(s => s.GetPercentileValue(50d)) ? min : frameTimeStats.Min(s => s.GetPercentileValue(50d));
				max = max >= iterationTimeStats.Max(s => s.GetPercentileValue(50d)) ? max : iterationTimeStats.Max(s => s.GetPercentileValue(50d));

				min = min >= 1d ? min : 0d;
				max = max >= min ? max : min + 1d;

				_currentMin = min;
				_currentMax = max;

				for (int i = 0; i < frameTimeStats.Length; i++)
					_graphColumns[i] = RenderGraphColumn(_columnIterationTimeStatsMs[i], _columnFrameTimeStatsMs[i]);

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
				yFps010 = fpsStats.GetPercentileValue(Parameters.PERF_GRAPH_PERCENTILE_CUTOFF),
				yFps025 = fpsStats.GetPercentileValue(25),
				yFps040 = fpsStats.GetPercentileValue(40),
				yFps050 = fpsStats.GetPercentileValue(50),
				yFps060 = fpsStats.GetPercentileValue(60),
				yFps075 = fpsStats.GetPercentileValue(75),
				yFps090 = fpsStats.GetPercentileValue(100d - Parameters.PERF_GRAPH_PERCENTILE_CUTOFF),
				yFps100 = fpsStats.Max,
				yTime000 = frameTimeStats.Min,
				yTime010 = frameTimeStats.GetPercentileValue(Parameters.PERF_GRAPH_PERCENTILE_CUTOFF),
				yTime025 = frameTimeStats.GetPercentileValue(25),
				yTime040 = frameTimeStats.GetPercentileValue(40),
				yTime050 = frameTimeStats.GetPercentileValue(50),
				yTime060 = frameTimeStats.GetPercentileValue(60),
				yTime075 = frameTimeStats.GetPercentileValue(75),
				yTime090 = frameTimeStats.GetPercentileValue(100d - Parameters.PERF_GRAPH_PERCENTILE_CUTOFF),
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
			for (int y = y000Scaled >= 0d ? (int)y000Scaled : 0; y < (y100Scaled <= Parameters.GRAPH_HEIGHT ? (int)y100Scaled : Parameters.GRAPH_HEIGHT); y++) {
				if (y == (int)y000Scaled) {//bottom pixel
					if (y000Scaled % 1d >= 0.5d)//top half
						chr = Parameters.CHAR_TOP;
					else if (y100Scaled < y + 0.5d)//bottom half
						chr = Parameters.CHAR_LOW;
					else chr = Parameters.CHAR_BOTH;
				} else if (y == (int)y100Scaled) {//top pixel
					if (y100Scaled % 1d < 0.5d)//bottom half
						chr = Parameters.CHAR_LOW;
					else if (y000Scaled >= y && y100Scaled >= y + 0.5d)//top half
						chr = Parameters.CHAR_TOP;
					else chr = Parameters.CHAR_BOTH;
				} else chr = Parameters.CHAR_BOTH;
				
				if (y == (int)yFps050Scaled)
					color = ConsoleColor.DarkGreen;
				else if (y >= (int)yFps040Scaled && y <= (int)yFps060Scaled)
					color = ConsoleColor.Green;

				else if (y == (int)yTime050Scaled)
					color = ConsoleColor.DarkCyan;
				else if (y >= (int)yTime040Scaled && y <= (int)yTime060Scaled)
					color = ConsoleColor.Cyan;

				else if (y >= (int)yFps025Scaled && y <= (int)yFps075Scaled)
					color = ConsoleColor.White;
				else if (y >= (int)yTime025Scaled && y <= (int)yTime075Scaled)
					color = ConsoleColor.White;

				else if (y >= (int)yFps010Scaled && y <= (int)yFps090Scaled)
					color = ConsoleColor.Gray;
				else if (y >= (int)yTime010Scaled && y <= (int)yTime090Scaled)
					color = ConsoleColor.Gray;
				
				else if (y >= (int)yFps000Scaled && y <= (int)yFps100Scaled)
					color = ConsoleColor.DarkGray;
				else if (y >= (int)yTime000Scaled && y <= (int)yTime100Scaled)
					color = ConsoleColor.DarkGray;

				else color = ConsoleColor.Black;

				result[y] = new ConsoleExtensions.CharInfo(chr, color, ConsoleColor.Black);
			}
			return result;
		}

		public static void WriteEnd() {
			TimeSpan totalDuration = Program.Manager.EndTimeUtc.Subtract(Program.Manager.StartTimeUtc);
			
			Console.SetCursorPosition(0, 1);
			Console.ForegroundColor = ConsoleColor.White;
			Console.BackgroundColor = ConsoleColor.Black;
			Console.WriteLine("---END--- Duration {0}s", Program.Manager.EndTimeUtc.Subtract(Program.Manager.StartTimeUtc).TotalSeconds.ToStringBetter(2));
			
			Console.Write("Evaluated ");

			int particleCount = Program.Simulator.AllParticles.Length;
			Console.Write(particleCount);

			Console.ForegroundColor = ConsoleColor.White;
			Console.Write(" {0} for {1} averaging ",
				"particle".Pluralize(particleCount),
				Program.StepEval_Rasterize.NumCompleted.Pluralize("rasters")
					+ (Parameters.SIMULATION_SKIPS < 2
						? ""
						: " and " + Program.StepEval_Simulate.NumCompleted.Pluralize("simulation steps")));

			double fps = (double)Program.StepEval_Resample.NumCompleted / totalDuration.TotalSeconds;
			Console.ForegroundColor = ChooseFpsColor(fps);
			Console.Write(fps.ToStringBetter(4));
			Console.ForegroundColor = ConsoleColor.White;
			Console.Write(" FPS");

			if (Parameters.TARGET_FPS > 0) {
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
			double ratioToDesired = fps / (Parameters.TARGET_FPS > 0 ? Parameters.TARGET_FPS : Parameters.TARGET_FPS_DEFAULT);
			return ChooseColor(ratioToDesired);
		}
		private static ConsoleColor ChooseFrameIntervalColor(double timeMs) {
			double ratioToDesired = 1000d / (Parameters.TARGET_FPS > 0 ? Parameters.TARGET_FPS : Parameters.TARGET_FPS_DEFAULT) / timeMs;
			return ChooseColor(ratioToDesired);
		}
	}
}