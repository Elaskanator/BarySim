using System;
using System.Linq;
using Generic.Extensions;
using Generic.Models;
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

		private static readonly Tuple<string, double, ConsoleColor>[] _statsHeaderValues;

		static PerfMon() {
			_statsHeaderValues = new Tuple<string, double, ConsoleColor>[
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
					Program.StepEval_Rasterize.Step.IterationTicksAverager.LastUpdate
					/ 10000d;

				_frameTimingMs.Update(currentFrameTimeMs);
				_fpsTimingMs.Update(currentIterationTimeMs);

				_currentColumnFrameTimeDataMs[frameIdx] = currentFrameTimeMs;
				_columnFrameTimeStatsMs[0] = new StatsInfo(_currentColumnFrameTimeDataMs.Take(frameIdx + 1));

				if (result.NumCompleted == 1)
					_currentColumnIterationTimeDataMs[0] = currentFrameTimeMs;
				else _currentColumnIterationTimeDataMs[frameIdx] = currentIterationTimeMs;
				_columnIterationTimeStatsMs[0] = new StatsInfo(_currentColumnIterationTimeDataMs.Take(frameIdx + 1));
			}
		}

		public static void DrawStatsOverlay(ConsoleExtensions.CharInfo[] frameBuffer) {
			RefreshStatsHedaer();

			int position = 0;
			string numberStr;
			for (int i = 0; i < _statsHeaderValues.Length; i++) {
				for (int j = 0; j < _statsHeaderValues[i].Item1.Length; j++)
					frameBuffer[position + j] = new ConsoleExtensions.CharInfo(_statsHeaderValues[i].Item1[j], ConsoleColor.White);
				position += _statsHeaderValues[i].Item1.Length;
				numberStr = _statsHeaderValues[i].Item2.ToStringBetter(Parameters.NUMBER_ACCURACY, Parameters.NUMBER_SPACING).PadCenter(Parameters.NUMBER_SPACING);
				for (int j = 0; j < numberStr.Length; j++)
					frameBuffer[position + j] = new ConsoleExtensions.CharInfo(numberStr[j], _statsHeaderValues[i].Item3);
				position += numberStr.Length;
			}
		}

		private static void RefreshStatsHedaer() {
			double
				fps = 10000000 / (Program.StepEval_Rasterize.Step.IterationTicksAverager.LastUpdate),
				smoothedFps = 10000000 / (Program.StepEval_Rasterize.Step.IterationTicksAverager.Current);
			_statsHeaderValues[0] = new("FPS", smoothedFps, ChooseFpsColor(fps));

			if (Parameters.PERF_STATS_ENABLE) {
				string label;
				double timeVal, colorVal;
				for (int i = 0; i < Program.Manager.Evaluators.Length; i++) {
					label = Program.Manager.Evaluators[i].Step.Name[0].ToString();
					timeVal = Program.Manager.Evaluators[i].Step.ExclusiveTicksAverager.Current / 10000d;
					colorVal = Program.Manager.Evaluators[i].Step.ExclusiveTicksAverager.LastUpdate / 10000d;
					_statsHeaderValues[i + 1] = new(label, timeVal, ChooseFrameIntervalColor(colorVal));
				}
			}
		}

		public static void DrawFpsGraph(ConsoleExtensions.CharInfo[] frameBuffer) {
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

			double decimals = (_currentMax - _currentMin).BaseExponent();
			string fmtStr = "0";
			if (decimals < 1) {
				double diff = (_currentMax - _currentMin) * Math.Pow(10, -Math.Floor(decimals));
				if (diff > Parameters.GRAPH_HEIGHT)
					decimals++;
				if (decimals < 1)
					fmtStr = "." + new string('0', (int)Math.Ceiling(Math.Abs(decimals)));
			}
			double
				frameTime = _frameTimingMs.Current,
				fullTime = _fpsTimingMs.Current;
			string
				label_min = _currentMin < 1000 ? _currentMin.ToString(fmtStr) + "ms" : (_currentMin / 1000).ToStringBetter(3) + "s",
				label_max = _currentMax < 1000 ? _currentMax.ToString(fmtStr) + "ms" : (_currentMax / 1000).ToStringBetter(3) + "s",
				label_frameTime = frameTime < 1000 ? frameTime.ToString(fmtStr) + "ms" : (frameTime / 1000).ToStringBetter(3) + "s",
				label_fullTime = fullTime < 1000 ? fullTime.ToString(fmtStr) + "ms" : (fullTime / 1000).ToStringBetter(3) + "s";

			for (int i = 0; i < label_max.Length; i++)
				result[i] = new ConsoleExtensions.CharInfo(label_max[i], ConsoleColor.DarkGray);

			for (int i = 0; i < label_min.Length; i++)
				result[i + GraphWidth * (Parameters.GRAPH_HEIGHT - 1)] = new ConsoleExtensions.CharInfo(label_min[i], ConsoleColor.DarkGray);

			ConsoleColor color_frameTime = ChooseFrameIntervalColor(_frameTimingMs.LastUpdate);
			int offset_frameTime = (int)(Parameters.GRAPH_HEIGHT * (_frameTimingMs.Current - _currentMin) / (_currentMax - _currentMin));
			offset_frameTime = offset_frameTime < 0 ? 0 : offset_frameTime < Parameters.GRAPH_HEIGHT ? offset_frameTime : Parameters.GRAPH_HEIGHT - 1;
			for (int i = 0; i < label_frameTime.Length; i++)
				result[i + GraphWidth * (Parameters.GRAPH_HEIGHT - 1 - offset_frameTime)] = new ConsoleExtensions.CharInfo(label_frameTime[i], ConsoleColor.Gray);

			ConsoleColor color_fps = ChooseFrameIntervalColor(_fpsTimingMs.LastUpdate);
			int offset_fps = (int)(Parameters.GRAPH_HEIGHT * (_fpsTimingMs.Current - _currentMin) / (_currentMax - _currentMin));
			offset_fps = offset_fps < 0 ? 0 : offset_fps < Parameters.GRAPH_HEIGHT ? offset_fps : Parameters.GRAPH_HEIGHT - 1;
			for (int i = 0; i < label_fullTime.Length; i++)
				result[i + GraphWidth * (Parameters.GRAPH_HEIGHT - 1 - offset_fps)] = new ConsoleExtensions.CharInfo(label_fullTime[i], color_frameTime);
			
			frameBuffer.RegionMerge(Parameters.WINDOW_WIDTH, result, GraphWidth, 0, 1, true);
		}

		private static void RerenderGraph() {
			StatsInfo[]
				frameTimeStats = _columnFrameTimeStatsMs.Without(s => s is null).ToArray(),
				iterationTimeStats = _columnIterationTimeStatsMs.Without(s => s is null).ToArray();
			StatsInfo rangeStats = new StatsInfo(frameTimeStats.Concat(iterationTimeStats).SelectMany(s => s.Data_asc));
			
			double
				minMean = frameTimeStats.Min(s => s.Mean),
				maxMean = frameTimeStats.Max(s => s.Mean),
				newMin = rangeStats.GetPercentileValue(Parameters.PERF_GRAPH_PERCENTILE_LOW_CUTOFF),
				newMax = rangeStats.GetPercentileValue(100d - Parameters.PERF_GRAPH_PERCENTILE_HIGH_CUTOFF);

			if (newMin > minMean)
				newMin = minMean;
			if (newMin < 1)
				newMin = 0;

			if (newMax < maxMean)
				newMax = maxMean;
			if (newMin >= newMax)
				newMax = newMin + 1;

			_currentMin = newMin;
			_currentMax = newMax;

			for (int i = 0; i < frameTimeStats.Length; i++)
				_graphColumns[i] = RenderGraphColumn(_columnFrameTimeStatsMs[i], _columnIterationTimeStatsMs[i]);

			_lastGraphRenderFrameUtc = DateTime.UtcNow;
		}

		private static void DrawGraphColumn(ConsoleExtensions.CharInfo[] buffer, ConsoleExtensions.CharInfo[] newColumn, int xIdx) {
			for (int yIdx = 0; yIdx < Parameters.GRAPH_HEIGHT; yIdx++)
				if (!Equals(newColumn[yIdx], default(ConsoleExtensions.CharInfo)))
					buffer[xIdx + (Parameters.GRAPH_HEIGHT - yIdx - 1)*GraphWidth] = newColumn[yIdx];
		}
		private static ConsoleExtensions.CharInfo[] RenderGraphColumn(StatsInfo frameTimeStats, StatsInfo iterationTimeStats) {
			ConsoleExtensions.CharInfo[] result = new ConsoleExtensions.CharInfo[Parameters.GRAPH_HEIGHT];

			double
				y000 = frameTimeStats.Min,
				y025 = frameTimeStats.GetPercentileValue(25),
				y050 = frameTimeStats.Mean,
				y075 = frameTimeStats.GetPercentileValue(75),
				y100 = frameTimeStats.Max,
				yMax = iterationTimeStats.Max > y100 ? iterationTimeStats.Max : y100;

			double
				y000Scaled = Parameters.GRAPH_HEIGHT * (y000 - _currentMin) / (_currentMax - _currentMin),
				y025Scaled = Parameters.GRAPH_HEIGHT * (y025 - _currentMin) / (_currentMax - _currentMin),
				y050Scaled = Parameters.GRAPH_HEIGHT * (y050 - _currentMin) / (_currentMax - _currentMin),
				y075Scaled = Parameters.GRAPH_HEIGHT * (y075 - _currentMin) / (_currentMax - _currentMin),
				y100Scaled = Parameters.GRAPH_HEIGHT * (y100 - _currentMin) / (_currentMax - _currentMin),
				yMaxScaled = Parameters.GRAPH_HEIGHT * (yMax - _currentMin) / (_currentMax - _currentMin);
			if (yMaxScaled >= Parameters.GRAPH_HEIGHT) yMaxScaled--;
				
			ConsoleColor color; char chr;
			for (int yIdx = (int)y000Scaled; yIdx <= yMaxScaled; yIdx++) {
				if ((int)yMaxScaled == yIdx) {//top pixel
					if (yMaxScaled % 1d < 0.5d)//bottom half
						chr = Parameters.CHAR_LOW;
					else if (y000Scaled >= yIdx && yMaxScaled >= yIdx + 0.5d)//top half
						chr = Parameters.CHAR_TOP;
					else chr = Parameters.CHAR_BOTH;
				} else if ((int)y000Scaled == yIdx) {//bottom pixel
					if (y000Scaled % 1d >= 0.5d)//top half
						chr = Parameters.CHAR_TOP;
					else if (yMaxScaled < yIdx + 0.5d)//bottom half
						chr = Parameters.CHAR_LOW;
					else chr = Parameters.CHAR_BOTH;
				} else chr = Parameters.CHAR_BOTH;

				if ((int)yMaxScaled == yIdx)
					color = ConsoleColor.DarkGreen;
				else if ((int)y050Scaled == yIdx)
					color = ConsoleColor.DarkBlue;
				else if ((int)y100Scaled <= yIdx)
					color = ConsoleColor.DarkMagenta;
				else if ((int)y075Scaled <= yIdx)
					color = ConsoleColor.DarkGray;
				else if ((int)y050Scaled <= yIdx)
					color = ConsoleColor.Gray;
				else if ((int)y025Scaled <= yIdx)
					color = ConsoleColor.White;
				else if ((int)y000Scaled <= yIdx)
					color = ConsoleColor.Gray;
				else color = ConsoleColor.DarkGray;

				result[yIdx] = new ConsoleExtensions.CharInfo(chr, color);
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