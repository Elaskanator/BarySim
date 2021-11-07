using System;
using System.Collections.Generic;
using System.Linq;
using Generic;

namespace Boids {
	internal static class PerfMon {
		private static SampleSMA _frameTiming = new SampleSMA(Parameters.PERF_SMA_ALPHA);
		private static Dictionary<int, double> _simulationTimes = new();//TODO this is a bigass memory leak
		private static double[] _currentColumnData;
		private static SampleSMA _currentMin = new SampleSMA(Parameters.PERF_SMA_ALPHA);
		private static SampleSMA _currentMax = new SampleSMA(Parameters.PERF_SMA_ALPHA);
		private static ConsoleExtensions.CharInfo[][] _columns = new ConsoleExtensions.CharInfo[Parameters.GRAPH_WIDTH][];
		private static BasicStatisticsInfo[] _columnStats = new BasicStatisticsInfo[Parameters.GRAPH_WIDTH];
		private static readonly object _lock = new();

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

		internal static void AfterRasterize(DateTime startUtc) {
			//IterationCount is not yet updated
			int frameIdx = Program.Step_Rasterizer.IterationCount % Parameters.PERF_GRAPH_FRAMES_PER_COLUMN;
			if (frameIdx == 0) lock (_lock) {
				_currentColumnData = new double[Parameters.PERF_GRAPH_FRAMES_PER_COLUMN];
				_columns = _columns.ShiftRight(false);
				_columns[0] = _columns[1];
				_columnStats = _columnStats.ShiftRight(false);
			}
			long currentFrameTimeTicks = (long)(_simulationTimes[Program.Step_Rasterizer.IterationCount] + DateTime.UtcNow.Subtract(startUtc).Ticks);//TODO small memory leak
			_frameTiming.Update(currentFrameTimeTicks);
			_currentColumnData[frameIdx] = currentFrameTimeTicks / 10000d;
			_columnStats[0] = new BasicStatisticsInfo(_currentColumnData.Take(frameIdx + 1));
		}

		internal static void AfterSimulate(DateTime startUtc) {
			if (Program.Step_Simulator.IterationCount % Parameters.SUBFRAME_MULTIPLE == 0)
				_simulationTimes[Program.Step_Simulator.IterationCount / Parameters.SUBFRAME_MULTIPLE] = DateTime.UtcNow.Subtract(startUtc).Ticks;
		}

		public static void DrawStatsHeader(ConsoleExtensions.CharInfo[] buffer) {
			AEvaluationStep[] steps = new AEvaluationStep[] {
				Program.Step_TreeManager,
				Program.Step_Simulator,
				Program.Step_Rasterizer,
				Program.Step_Drawer,
				Program.Step_Renderer,
				Program.Step_Autoscaler,
			};

			Func<AEvaluationStep, bool> includeQTimingsTester = s => (s is EvaluationStep) && !(s as EvaluationStep).IsOutputOverwrite;
			Tuple<string, double, ConsoleColor>[] values = new Tuple<string, double, ConsoleColor>[steps.Sum(s => includeQTimingsTester(s) ? 3 : 1) + 1];
			double fps;
			if (Parameters.SYNC_SIMULATION)
				fps = Program.Step_Drawer.IterationTimings_Ticks.Current ?? 0d;
			else fps = _frameTiming.Current ?? 0d;
			fps = 10000000 / fps;//conversion from ticks
			values[0] = new("FPS", fps, ChooseFpsColor(fps));

			string label;
			double timeVal;
			for (int i = 0, k = 1; i < steps.Length; k += includeQTimingsTester(steps[i]) ? 3 : 1, i++) {
				label = steps[i].Name[0].ToString();
				timeVal = (steps[i].CalculationTimings_Ticks.Current ?? 0) / 10000d;
				values[k] = new(label, timeVal, ChooseFrameIntervalColor(timeVal));

				if (includeQTimingsTester(steps[i])) {
					timeVal = ((steps[i] as EvaluationStep).OutputResource.EnqueueTimings_Ticks.Current ?? 0d) / 10000d;
					values[k + 1] = new("|", timeVal, ChooseFrameIntervalColor(timeVal));

					timeVal = ((steps[i] as EvaluationStep).OutputResource.DequeueTimings_Ticks.Current ?? 0d) / 10000d;
					values[k + 2] = new("|", timeVal, ChooseFrameIntervalColor(timeVal));
				}
			}

			int position = 0;
			string numberStr;
			for (int i = 0; i < values.Length; i++) {
				for (int j = 0; j < values[i].Item1.Length; j++)
					buffer[position + j] = new ConsoleExtensions.CharInfo(values[i].Item1[j], ConsoleColor.White);
				position += values[i].Item1.Length;
				numberStr = values[i].Item2.ToStringBetter(Parameters.NUMBER_ACCURACY, Parameters.NUMBER_SPACING).PadCenter(Parameters.NUMBER_SPACING);
				for (int j = 0; j < numberStr.Length; j++)
					buffer[position + j] = new ConsoleExtensions.CharInfo(numberStr[j], values[i].Item3);
				position += numberStr.Length;
			}
		}

		public static ConsoleExtensions.CharInfo[] GetFpsGraph() {
			ConsoleExtensions.CharInfo[] result;
			double dataMin, dataAvg, dataMax;
			lock (_lock) {
				if (_columnStats[0] is null) return null;

				result = new ConsoleExtensions.CharInfo[Parameters.GRAPH_WIDTH * Parameters.GRAPH_HEIGHT];
				double numColumns = Program.Step_Rasterizer.IterationCount / Parameters.PERF_GRAPH_FRAMES_PER_COLUMN;

				dataMin = _columnStats.Where(s => !(s is null)).Min(s => s.Percentile25);
				dataAvg = _columnStats.Where(s => !(s is null)).Average(s => s.Mean);//faster than true average calculation
				dataMax = _columnStats.Where(s => !(s is null)).Max(s => s.Percentile75);
				if (dataMin < 1)
					dataMin = 0;
				if (dataMin >= dataMax)
					dataMax = dataMin + 1;

				//autoscaling - ignore small anomalies
				bool recompute = _currentMin.NumUpdates == 0
					|| 0.1 < (Math.Abs(dataMin - _currentMin.Current.Value) + Math.Abs(dataMax - _currentMax.Current.Value)) / (dataMax - dataMin);
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
				label_current = (_frameTiming.LastUpdate.Value / 10000d).ToStringBetter(2) + "ms",
				label_min = _currentMin.Current.Value.ToStringBetter(2),
				label_avg = dataAvg.ToStringBetter(2),
				label_max = _currentMax.Current.Value.ToStringBetter(2);

			for (int i = 0; i < label_max.Length; i++)
				result[i] = new ConsoleExtensions.CharInfo(label_max[i], ConsoleColor.Gray);
			for (int i = 0; i < label_min.Length; i++)
				result[i + Parameters.GRAPH_WIDTH * (Parameters.GRAPH_HEIGHT - 1)] = new ConsoleExtensions.CharInfo(label_min[i], ConsoleColor.Gray);

			int offset_avg = (int)(Parameters.GRAPH_HEIGHT * (dataAvg - _currentMin.Current.Value) / (_currentMax.Current.Value - _currentMin.Current.Value));
			if (offset_avg >= 0 && offset_avg < Parameters.GRAPH_HEIGHT)
				for (int i = 0; i < label_avg.Length; i++)
					result[i + Parameters.GRAPH_WIDTH * (Parameters.GRAPH_HEIGHT - 1 - offset_avg)] = new ConsoleExtensions.CharInfo(label_avg[i], ConsoleColor.Gray);

			int offset_current = (int)(Parameters.GRAPH_HEIGHT * ((_frameTiming.Current.Value / 10000d) - _currentMin.Current.Value) / (_currentMax.Current.Value - _currentMin.Current.Value));
			if (offset_current < 0) offset_current = 0;
			else if (offset_current >= Parameters.GRAPH_HEIGHT) offset_current = Parameters.GRAPH_HEIGHT - 1;
			ConsoleColor color_current = ChooseFrameIntervalColor(_frameTiming.LastUpdate.Value / 10000d);
			for (int i = 0; i < label_current.Length; i++)
				result[i + Parameters.GRAPH_WIDTH * (Parameters.GRAPH_HEIGHT - 1 - offset_current)] = new ConsoleExtensions.CharInfo(label_current[i], color_current);

			return result;
		}

		private static void DrawGraphColumn(ConsoleExtensions.CharInfo[] buffer, ConsoleExtensions.CharInfo[] newColumn, int xIdx) {
			for (int yIdx = 0; yIdx < Parameters.GRAPH_HEIGHT; yIdx++)
				if (!Equals(newColumn[yIdx], default(ConsoleExtensions.CharInfo)))
					buffer[xIdx + (Parameters.GRAPH_HEIGHT - yIdx - 1)*Parameters.GRAPH_WIDTH] = newColumn[yIdx];
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
				y000Scaled = Parameters.GRAPH_HEIGHT * (y000 - _currentMin.Current.Value) / (_currentMax.Current.Value - _currentMin.Current.Value),
				y010Scaled = Parameters.GRAPH_HEIGHT * (y010 - _currentMin.Current.Value) / (_currentMax.Current.Value - _currentMin.Current.Value),
				y0205caled = Parameters.GRAPH_HEIGHT * (y025 - _currentMin.Current.Value) / (_currentMax.Current.Value - _currentMin.Current.Value),
				y050Scaled = Parameters.GRAPH_HEIGHT * (y050 - _currentMin.Current.Value) / (_currentMax.Current.Value - _currentMin.Current.Value),
				Y075Scaled = Parameters.GRAPH_HEIGHT * (Y075 - _currentMin.Current.Value) / (_currentMax.Current.Value - _currentMin.Current.Value),
				Y090Scaled = Parameters.GRAPH_HEIGHT * (Y090 - _currentMin.Current.Value) / (_currentMax.Current.Value - _currentMin.Current.Value),
				y100Scaled = Parameters.GRAPH_HEIGHT * (y100 - _currentMin.Current.Value) / (_currentMax.Current.Value - _currentMin.Current.Value);
				
			int
				minY = y000Scaled < 0 ? 0 : (int)Math.Floor(y000Scaled),
				maxY = y100Scaled > Parameters.GRAPH_HEIGHT ? Parameters.GRAPH_HEIGHT : (int)Math.Ceiling(y100Scaled);
			ConsoleColor color; char chr;
			for (int yIdx = minY; yIdx < maxY; yIdx++) {
				if ((int)y100Scaled == yIdx) {//top pixel
					if (y100Scaled % 1d < 0.5d)//bottom half
						chr = Rasterizer.CHAR_BOTTOM;
					else if (y000Scaled >= yIdx + 0.5d)//top half
						chr = Rasterizer.CHAR_TOP;
					else chr = Rasterizer.CHAR_BOTH;
				} else if ((int)y000Scaled == yIdx) {//bottom pixel
					if (y000Scaled % 1d >= 0.5d)//top half
							chr = Rasterizer.CHAR_TOP;
					else if (y100Scaled < yIdx + 0.5d)//bottom half
						chr = Rasterizer.CHAR_BOTTOM;
					else chr = Rasterizer.CHAR_BOTH;
				} else chr = Rasterizer.CHAR_BOTH;

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
			
			Console.ForegroundColor = ChooseColor(Math.Pow((double)Parameters.RATED_BOIDS / Program.TotalBoids, 2));
			Console.Write(Program.TotalBoids);

			Console.ForegroundColor = ConsoleColor.White;
			Console.Write(" {0} across {1} for {2} averaging ",
				"boid".Pluralize(Program.TotalBoids),
				Parameters.NUM_FLOCKS.Pluralize("flock"),
				(Program.Step_Rasterizer.IterationCount - Program.Q_Rasterization.Depth).Pluralize("rasters")
					+ (Parameters.SUBFRAME_MULTIPLE < 2
						? ""
						: " and " + Program.Step_Simulator.IterationCount.Pluralize("simulation steps")));

			double fps = (double)Program.Step_Rasterizer.IterationCount / totalDuration.TotalSeconds;
			Console.ForegroundColor = ChooseFpsColor(fps);
			Console.Write(fps.ToStringBetter(4));
			Console.ForegroundColor = ConsoleColor.White;
			Console.Write(" FPS");

			if (Parameters.TARGET_FPS > 0) {
				double expectedFramesRendered = Parameters.TARGET_FPS * (double)totalDuration.TotalSeconds;
				double fpsRatio = (double)Program.Step_Renderer.IterationCount / ((int)(1 + expectedFramesRendered));

				Console.Write(" ({0:G3}% of {1} fps)",
					100 * fpsRatio,
					Parameters.TARGET_FPS);
			}

			Console.WriteLine();
			Console.ResetColor();
			ConsoleExtensions.WaitForEnter("Press <Enter> to exit");
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