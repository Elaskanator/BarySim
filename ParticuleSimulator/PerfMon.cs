using System;
using System.Linq;
using Generic.Extensions;
using Generic.Models;
using ParticleSimulator.Threading;

namespace ParticleSimulator {
	internal static class PerfMon {
		private static SampleSMA _frameTimingMs = new SampleSMA(Parameters.PERF_SMA_ALPHA);
		private static SampleSMA _fpsTimingMs = new SampleSMA(Parameters.PERF_SMA_ALPHA);
		public static readonly int GraphWidth;

		private static double[] _currentColumnFrameTimeDataMs;
		private static double[] _currentColumnIterationTimeDataMs;
		private static double _currentMin = 0;
		private static double _currentMax = 0;
		private static BasicStatisticsInfo[] _columnFrameTimeStatsMs;
		private static BasicStatisticsInfo[] _columnIterationTimeStatsMs;
		private static ConsoleExtensions.CharInfo[][] _graph_columns;
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

			_columnFrameTimeStatsMs = new BasicStatisticsInfo[GraphWidth];
			_columnIterationTimeStatsMs = new BasicStatisticsInfo[GraphWidth];
			_graph_columns = new ConsoleExtensions.CharInfo[GraphWidth][];
		}

		public static void AfterRasterize(StepEvaluator result) {
			int frameIdx = (result.NumCompleted - 1) % Parameters.PERF_GRAPH_FRAMES_PER_COLUMN;
			lock (_columnStatsLock) {
				if (frameIdx == 0) {
					if (result.NumCompleted - 1 == Parameters.PERF_GRAPH_FRAMES_PER_COLUMN || result.NumCompleted < 4) {//ignore the first couple junk timings
						_currentColumnFrameTimeDataMs = new double[Parameters.PERF_GRAPH_FRAMES_PER_COLUMN];
						_currentColumnIterationTimeDataMs = new double[Parameters.PERF_GRAPH_FRAMES_PER_COLUMN];
					} else {
						_currentColumnFrameTimeDataMs = new double[Parameters.PERF_GRAPH_FRAMES_PER_COLUMN];
						_currentColumnIterationTimeDataMs = new double[Parameters.PERF_GRAPH_FRAMES_PER_COLUMN];
						_graph_columns = _graph_columns.ShiftRight(false);
						_graph_columns[0] = _graph_columns[1];
						_columnFrameTimeStatsMs = _columnFrameTimeStatsMs.ShiftRight(false);
						_columnIterationTimeStatsMs = _columnIterationTimeStatsMs.ShiftRight(false);
					}
				}
				double currentFrameTimeMs = new double[] {
					Program.StepEval_Simulate.Step.ExclusiveTicksAverager.Current,
					Program.StepEval_Rasterize.Step.ExclusiveTicksAverager.Current,
					Program.StepEval_Resample.Step.ExclusiveTicksAverager.Current,
				}.Max() / 10000d;

				_frameTimingMs.Update(currentFrameTimeMs);
				_fpsTimingMs.Update(Program.StepEval_Draw.Step.IterationTicksAverager.Current / 10000d);

				_currentColumnFrameTimeDataMs[frameIdx] = currentFrameTimeMs;
				_columnFrameTimeStatsMs[0] = new BasicStatisticsInfo(_currentColumnFrameTimeDataMs.Take(frameIdx + 1));

				_currentColumnIterationTimeDataMs[frameIdx] = Program.StepEval_Draw.Step.IterationTicksAverager.Current / 10000d;
				_columnIterationTimeStatsMs[0] = new BasicStatisticsInfo(_currentColumnIterationTimeDataMs.Take(frameIdx + 1));
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
				fps = 10000000 / Program.StepEval_Draw.Step.IterationTicksAverager.LastUpdate,
				smoothedFps = 10000000 / Program.StepEval_Draw.Step.IterationTicksAverager.Current;
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

		public static ConsoleExtensions.CharInfo[] RenderFpsGraph() {
			BasicStatisticsInfo[] frameTimeStats, iterationTimeStats;
			lock (_columnStatsLock) {
				if (_columnFrameTimeStatsMs[0] is null)
					return null;
				frameTimeStats = _columnFrameTimeStatsMs.TakeUntil(s => s is null).ToArray();
				iterationTimeStats = _columnIterationTimeStatsMs.TakeUntil(s => s is null).ToArray();
			}

			ConsoleExtensions.CharInfo[] result = new ConsoleExtensions.CharInfo[GraphWidth * Parameters.GRAPH_HEIGHT];
			BasicStatisticsInfo rangeStats = new BasicStatisticsInfo(frameTimeStats.Concat(iterationTimeStats).Where(s => !(s is null)).SelectMany(s => s.Data_asc));
			
			double
				minMean = frameTimeStats.Min(s => s.Mean),
				maxMean = frameTimeStats.Max(s => s.Mean),
				newMin = rangeStats.GetPercentileValue(Parameters.PERF_GRAPH_PERCENTILE_LOW_CUTOFF),
				newMax = rangeStats.GetPercentileValue(100 - Parameters.PERF_GRAPH_PERCENTILE_HIGH_CUTOFF),
				frameTime = frameTimeStats[0].Mean,
				fps = iterationTimeStats[0].Mean;

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

			for (int i = 0; i < frameTimeStats.Length; i++) {
				_graph_columns[i] = RenderGraphColumn(frameTimeStats[i], iterationTimeStats[i]);
				DrawGraphColumn(result, _graph_columns[i], i);
			}

			double decimals = (_currentMax - _currentMin).BaseExponent();
			string fmtStr = "0";
			if (decimals < 1) {
				double diff = (_currentMax - _currentMin) * Math.Pow(10, -Math.Floor(decimals));
				if (diff > Parameters.GRAPH_HEIGHT) decimals++;
				if (decimals < 1)
					fmtStr = "." + new string('0', (int)Math.Ceiling(Math.Abs(decimals)));
			}

			string
				label_min = _currentMin.ToString(fmtStr) + "ms",
				label_max = _currentMax.ToString(fmtStr) + "ms",
				label_frameTime = frameTime.ToString(fmtStr) + "ms",
				label_fps = fps.ToString(fmtStr) + "ms";

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
			for (int i = 0; i < label_fps.Length; i++)
				result[i + GraphWidth * (Parameters.GRAPH_HEIGHT - 1 - offset_fps)] = new ConsoleExtensions.CharInfo(label_fps[i], color_frameTime);

			return result;
		}

		private static void DrawGraphColumn(ConsoleExtensions.CharInfo[] buffer, ConsoleExtensions.CharInfo[] newColumn, int xIdx) {
			for (int yIdx = 0; yIdx < Parameters.GRAPH_HEIGHT; yIdx++)
				if (!Equals(newColumn[yIdx], default(ConsoleExtensions.CharInfo)))
					buffer[xIdx + (Parameters.GRAPH_HEIGHT - yIdx - 1)*GraphWidth] = newColumn[yIdx];
		}
		private static ConsoleExtensions.CharInfo[] RenderGraphColumn(BasicStatisticsInfo frameTimeStats, BasicStatisticsInfo iterationTimeStats) {
			ConsoleExtensions.CharInfo[] result = new ConsoleExtensions.CharInfo[Parameters.GRAPH_HEIGHT];

			double
				y000 = frameTimeStats.Min,
				y010 = frameTimeStats.GetPercentileValue(10),
				y025 = frameTimeStats.GetPercentileValue(25),
				y050 = frameTimeStats.Mean,
				y075 = frameTimeStats.GetPercentileValue(75),
				y090 = frameTimeStats.GetPercentileValue(90),
				y100 = iterationTimeStats.Mean > frameTimeStats.Max ? iterationTimeStats.Mean : frameTimeStats.Max;

			double
				y000Scaled = Parameters.GRAPH_HEIGHT * (y000 - _currentMin) / (_currentMax - _currentMin),
				y010Scaled = Parameters.GRAPH_HEIGHT * (y010 - _currentMin) / (_currentMax - _currentMin),
				y0205caled = Parameters.GRAPH_HEIGHT * (y025 - _currentMin) / (_currentMax - _currentMin),
				y050Scaled = Parameters.GRAPH_HEIGHT * (y050 - _currentMin) / (_currentMax - _currentMin),
				Y075Scaled = Parameters.GRAPH_HEIGHT * (y075 - _currentMin) / (_currentMax - _currentMin),
				Y090Scaled = Parameters.GRAPH_HEIGHT * (y090 - _currentMin) / (_currentMax - _currentMin),
				yMaxScaled = Parameters.GRAPH_HEIGHT * (y100 - _currentMin) / (_currentMax - _currentMin);
				
			int
				minY = y000Scaled < 0 ? 0 : (int)Math.Floor(y000Scaled),
				maxY = yMaxScaled >= Parameters.GRAPH_HEIGHT ? Parameters.GRAPH_HEIGHT - 1 : (int)Math.Ceiling(yMaxScaled);
			ConsoleColor color; char chr;
			for (int yIdx = minY; yIdx < maxY; yIdx++) {
				if ((int)yMaxScaled == yIdx) {//top pixel
					if (yMaxScaled % 1d < 0.5d)//bottom half
						chr = Parameters.CHAR_LOW;
					else if (y000Scaled >= yIdx + 0.5d)//top half
						chr = Parameters.CHAR_TOP;
					else chr = Parameters.CHAR_BOTH;
				} else if ((int)y000Scaled == yIdx) {//bottom pixel
					if (y000Scaled % 1d >= 0.5d)//top half
							chr = Parameters.CHAR_TOP;
					else if (yMaxScaled < yIdx + 0.5d)//bottom half
						chr = Parameters.CHAR_LOW;
					else chr = Parameters.CHAR_BOTH;
				} else chr = Parameters.CHAR_BOTH;

				switch (yIdx.CompareTo((int)y050Scaled)) {
					case -1://bottom stat
						if ((int)y010Scaled > yIdx)
							color = ConsoleColor.DarkGray;
						else if ((int)y0205caled > yIdx)
							color = ConsoleColor.Gray;
						else color = ConsoleColor.White;
						break;
					case 0://average frame time
						color = ConsoleColor.Blue;
						break;
					case 1://top stat
						if ((int)Y090Scaled < yIdx)
							color = ConsoleColor.DarkGray;
						else if ((int)Y075Scaled < yIdx)
							color = ConsoleColor.Gray;
						else color = ConsoleColor.White;
						break;
					default:
						throw new ImpossibleCompareToException();
				}
				if (yIdx == (int)yMaxScaled)
					color = ConsoleColor.DarkBlue;

				result[yIdx] = new ConsoleExtensions.CharInfo(chr, color);
			}
			return result;
		}

		public static void WriteEnd() {
			TimeSpan totalDuration = Program.Manager.EndTimeUtc.Subtract(Program.Manager.StartTimeUtc);
			
			Console.SetCursorPosition(0, 1);
			Console.ForegroundColor = ConsoleColor.White;
			Console.BackgroundColor = ConsoleColor.Black;
			Console.WriteLine("---END--- Duration {0:G3}s", Program.Manager.EndTimeUtc.Subtract(Program.Manager.StartTimeUtc).TotalSeconds);
			
			Console.Write("Evaluated ");

			int particleCount = Program.Simulator.AllParticles.Count();
			Console.Write(particleCount);

			Console.ForegroundColor = ConsoleColor.White;
			Console.Write(" {0} for {1} averaging ",
				"particle".Pluralize(particleCount),
				(Program.StepEval_Resample.NumCompleted - Program.Resource_Resamplings.QueueLength).Pluralize("rasters")
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
			foreach (Tuple<double, ConsoleColor> rank in Parameters.RatioColors) {
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