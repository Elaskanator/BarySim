using System;
using System.Linq;
using Generic.Extensions;
using Generic.Models;
using ParticleSimulator.Threading;

namespace ParticleSimulator {
	internal static class PerfMon {
		private static SampleSMA _frameTiming = new SampleSMA(Parameters.PERF_SMA_ALPHA);
		public static readonly int GraphWidth;

		private static double[] _currentColumnData;
		private static SampleSMA _currentMin = new SampleSMA(Parameters.PERF_GRA_SMA_ALPHA);
		private static SampleSMA _currentMax = new SampleSMA(Parameters.PERF_GRA_SMA_ALPHA);
		private static BasicStatisticsInfo[] _columnStats;
		private static ConsoleExtensions.CharInfo[][] _columns;
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

			_columnStats = new BasicStatisticsInfo[GraphWidth];
			_columns = new ConsoleExtensions.CharInfo[GraphWidth][];
		}

		public static readonly Tuple<double, ConsoleColor>[] RatioColors = new Tuple<double, ConsoleColor>[] {
			new Tuple<double, ConsoleColor>(1.05d, ConsoleColor.Cyan),
			new Tuple<double, ConsoleColor>(0.80d, ConsoleColor.DarkGreen),
			new Tuple<double, ConsoleColor>(0.95d, ConsoleColor.Green),
			new Tuple<double, ConsoleColor>(0.67d, ConsoleColor.Yellow),
			new Tuple<double, ConsoleColor>(0.50d, ConsoleColor.DarkYellow),
			new Tuple<double, ConsoleColor>(0.33d, ConsoleColor.Magenta),
			new Tuple<double, ConsoleColor>(0.25d, ConsoleColor.Red),
			new Tuple<double, ConsoleColor>(0.10d, ConsoleColor.DarkRed),
			new Tuple<double, ConsoleColor>(0.00d, ConsoleColor.DarkRed),
			new Tuple<double, ConsoleColor>(double.NegativeInfinity, ConsoleColor.White)
		};

		public static void AfterRasterize(StepEvaluator result) {
			//IterationCount is not yet updated
			int frameIdx = (result.NumCompleted - 1) % Parameters.PERF_GRAPH_FRAMES_PER_COLUMN;
			if (frameIdx == 0)
				lock (_columnStatsLock) {
					_currentColumnData = new double[Parameters.PERF_GRAPH_FRAMES_PER_COLUMN];
					_columns = _columns.ShiftRight(false);
					_columns[0] = _columns[1];
					_columnStats = _columnStats.ShiftRight(false);
				}

			long currentFrameTimeTicks = result.IterationEndUtc.Value
				.Subtract(result.IterationStartUtc.Value).Ticks;
			_frameTiming.Update(currentFrameTimeTicks);
			_currentColumnData[frameIdx] = currentFrameTimeTicks / 10000d;
			_columnStats[0] = new BasicStatisticsInfo(_currentColumnData.Take(frameIdx + 1));
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
				double timeVal;
				for (int i = 0; i < Program.Manager.Evaluators.Length; i++) {
					label = Program.Manager.Evaluators[i].Step.Name[0].ToString();
					timeVal = Program.Manager.Evaluators[i].Step.ExclusiveTimeAverage.Current / 10000d;
					_statsHeaderValues[i + 1] = new(label, timeVal, ChooseFrameIntervalColor(timeVal));
				}
			}
		}

		public static ConsoleExtensions.CharInfo[] GetFpsGraph() {
			ConsoleExtensions.CharInfo[] result;
			BasicStatisticsInfo rangeStatsLow, rangeStatsHigh;
			double dataMin, dataAvg, dataMax;
			lock (_columnStatsLock) {
				if (_columnStats[0] is null) return null;

				result = new ConsoleExtensions.CharInfo[GraphWidth * Parameters.GRAPH_HEIGHT];

				rangeStatsLow = new BasicStatisticsInfo(_columnStats.Where(s => !(s is null)).Select(s => s.GetPercentileValue(Parameters.PERF_GRAPH_PERCENTILE_LOW_CUTOFF)));
				rangeStatsHigh = new BasicStatisticsInfo(_columnStats.Where(s => !(s is null)).Select(s => s.GetPercentileValue(100 - Parameters.PERF_GRAPH_PERCENTILE_HIGH_CUTOFF)));
				dataMin = rangeStatsLow.GetPercentileValue(Parameters.PERF_GRAPH_PERCENTILE_LOW_CUTOFF);
				dataAvg = _columnStats.Where(s => !(s is null)).Average(s => s.Mean);//faster than true average calculation
				dataMax = rangeStatsHigh.GetPercentileValue(100 - Parameters.PERF_GRAPH_PERCENTILE_HIGH_CUTOFF);
				if (dataMin < 1)
					dataMin = 0;
				if (dataMin >= dataMax)
					dataMax = dataMin + 1;

				//autoscaling - ignore small anomalies
				bool recompute = _currentMin.NumUpdates == 0
					|| 0.1 < (Math.Abs(dataMin - _currentMin.Current) + Math.Abs(dataMax - _currentMax.Current)) / (dataMax - dataMin);
				if (recompute) {
					_currentMin.Update(dataMin);
					_currentMax.Update(dataMax);
				}

				for (int i = 0; i < _columnStats.Length; i++) {
					if (!(_columnStats[i] is null)) {
						if (i == 0 || recompute) _columns[i] = ComputeGraphColumn(i, _columnStats[i]);
						DrawGraphColumn(result, _columns[i], i);
					}
				}
			}

			string
				label_current = (_frameTiming.LastUpdate / 10000d).ToStringBetter(2) + "ms",
				label_min = _currentMin.Current.ToStringBetter(2) + "ms",
				label_avg = dataAvg.ToStringBetter(2),
				label_max = _currentMax.Current.ToStringBetter(2) + "ms";

			for (int i = 0; i < label_max.Length; i++)
				result[i] = new ConsoleExtensions.CharInfo(label_max[i], ConsoleColor.Gray);
			for (int i = 0; i < label_min.Length; i++)
				result[i + GraphWidth * (Parameters.GRAPH_HEIGHT - 1)] = new ConsoleExtensions.CharInfo(label_min[i], ConsoleColor.Gray);

			int offset_avg = (int)(Parameters.GRAPH_HEIGHT * (dataAvg - _currentMin.Current) / (_currentMax.Current - _currentMin.Current));
			if (offset_avg >= 0 && offset_avg < Parameters.GRAPH_HEIGHT)
				for (int i = 0; i < label_avg.Length; i++)
					result[i + GraphWidth * (Parameters.GRAPH_HEIGHT - 1 - offset_avg)] = new ConsoleExtensions.CharInfo(label_avg[i], ConsoleColor.Gray);

			int offset_current = (int)(Parameters.GRAPH_HEIGHT * ((_frameTiming.Current / 10000d) - _currentMin.Current) / (_currentMax.Current - _currentMin.Current));
			if (offset_current < 0) offset_current = 0;
			else if (offset_current >= Parameters.GRAPH_HEIGHT) offset_current = Parameters.GRAPH_HEIGHT - 1;
			ConsoleColor color_current = ChooseFrameIntervalColor(_frameTiming.LastUpdate / 10000d);
			for (int i = 0; i < label_current.Length; i++)
				result[i + GraphWidth * (Parameters.GRAPH_HEIGHT - 1 - offset_current)] = new ConsoleExtensions.CharInfo(label_current[i], color_current);

			return result;
		}

		private static void DrawGraphColumn(ConsoleExtensions.CharInfo[] buffer, ConsoleExtensions.CharInfo[] newColumn, int xIdx) {
			for (int yIdx = 0; yIdx < Parameters.GRAPH_HEIGHT; yIdx++)
				if (!Equals(newColumn[yIdx], default(ConsoleExtensions.CharInfo)))
					buffer[xIdx + (Parameters.GRAPH_HEIGHT - yIdx - 1)*GraphWidth] = newColumn[yIdx];
		}
		private static ConsoleExtensions.CharInfo[] ComputeGraphColumn(int xIdx, BasicStatisticsInfo columnStats) {
			ConsoleExtensions.CharInfo[] result = new ConsoleExtensions.CharInfo[Parameters.GRAPH_HEIGHT];

			double
				y000 = columnStats.Percentile0,
				y010 = columnStats.Percentile10,
				y025 = columnStats.Percentile25,
				y050 = columnStats.Percentile50,
				Y075 = columnStats.Percentile75,
				Y090 = columnStats.Percentile90,
				y100 = columnStats.Percentile100;
			double
				y000Scaled = Parameters.GRAPH_HEIGHT * (y000 - _currentMin.Current) / (_currentMax.Current - _currentMin.Current),
				y010Scaled = Parameters.GRAPH_HEIGHT * (y010 - _currentMin.Current) / (_currentMax.Current - _currentMin.Current),
				y0205caled = Parameters.GRAPH_HEIGHT * (y025 - _currentMin.Current) / (_currentMax.Current - _currentMin.Current),
				y050Scaled = Parameters.GRAPH_HEIGHT * (y050 - _currentMin.Current) / (_currentMax.Current - _currentMin.Current),
				Y075Scaled = Parameters.GRAPH_HEIGHT * (Y075 - _currentMin.Current) / (_currentMax.Current - _currentMin.Current),
				Y090Scaled = Parameters.GRAPH_HEIGHT * (Y090 - _currentMin.Current) / (_currentMax.Current - _currentMin.Current),
				y100Scaled = Parameters.GRAPH_HEIGHT * (y100 - _currentMin.Current) / (_currentMax.Current - _currentMin.Current);
				
			int
				minY = y000Scaled < 0 ? 0 : (int)Math.Floor(y000Scaled),
				maxY = y100Scaled > Parameters.GRAPH_HEIGHT ? Parameters.GRAPH_HEIGHT : (int)Math.Ceiling(y100Scaled);
			ConsoleColor color; char chr;
			for (int yIdx = minY; yIdx < maxY; yIdx++) {
				if ((int)y100Scaled == yIdx) {//top pixel
					if (y100Scaled % 1d < 0.5d)//bottom half
						chr = Parameters.CHAR_BOTTOM;
					else if (y000Scaled >= yIdx + 0.5d)//top half
						chr = Parameters.CHAR_TOP;
					else chr = Parameters.CHAR_BOTH;
				} else if ((int)y000Scaled == yIdx) {//bottom pixel
					if (y000Scaled % 1d >= 0.5d)//top half
							chr = Parameters.CHAR_TOP;
					else if (y100Scaled < yIdx + 0.5d)//bottom half
						chr = Parameters.CHAR_BOTTOM;
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
					case 0://average
						color = ChooseFrameIntervalColor(y050);
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
			foreach (Tuple<double, ConsoleColor> rank in RatioColors) {
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