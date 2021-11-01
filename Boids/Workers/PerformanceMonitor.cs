using System;
using System.Linq;
using Generic;

namespace Boids {
	internal static class PerformanceMonitor {
		public const int NUMBER_ACCURACY = 2;
		public const int NUMBER_SPACING = 5;

		public const int GRAPH_WIDTH = 73;
		public const int GRAPH_HEIGHT = 12;

		public static SampleSMA IterationTime_SMA = new SampleSMA(Parameters.PERF_SMA_ALPHA);
		public static SampleSMA FrameTime_SMA = new SampleSMA(Parameters.PERF_SMA_ALPHA);
		public static SampleSMA WriteTime_SMA = new SampleSMA(Parameters.PERF_SMA_ALPHA);
		public static SampleSMA RefreshTime_SMA = new SampleSMA(Parameters.PERF_SMA_ALPHA);
		public static SampleSMA SynchronizeTime_SMA = new SampleSMA(Parameters.PERF_SMA_ALPHA);
		public static SampleSMA DelayTime_SMA = new SampleSMA(Parameters.PERF_SMA_ALPHA);
		public static SampleSMA UpdateTime_SMA = new SampleSMA(Parameters.PERF_SMA_ALPHA);

		public static SampleSMA SimulationTime_SMA = new SampleSMA(Parameters.PERF_SMA_ALPHA);
		public static SampleSMA RasterizeTime_SMA = new SampleSMA(Parameters.PERF_SMA_ALPHA);
		public static SampleSMA QuadtreeTime_SMA = new SampleSMA(Parameters.PERF_SMA_ALPHA);
		public static SampleSMA AutoscaleTime_SMA = new SampleSMA(Parameters.PERF_SMA_ALPHA);

		public readonly static string[] DebugBarLabels = new string[] {
			"FPS",//total time between draws to screen
			"Time(ms)",//total time between draws to screen minus synchronization time (implies the maximum fps)
			"U",//update time between start of simulation and when it finishes drawing to screen
			"D",//delay between receiving data to draw to screen
			"S",//synchronization delay drawing to screen for a target FPS
			"| W",//total time spent drawing to screen with any overlays (legend, debug)
			"R",//time spent flushing data to screen via native platform invokation
			"| S",//simulation time
			"R",//rasterization time
			"| Q",//quadtree rebuild time
			"A",//chart autoscaling duration
		};

		public static void DrawStatsHeader(ConsoleExtensions.CharInfo[] buffer) {
			double
				fps = 1d / IterationTime_SMA.Current ?? 0,
				frameTimeMs = 1000d * (FrameTime_SMA.Current ?? 0d),
				updateTimeMs = 1000d * (UpdateTime_SMA.Current ?? 0d),
				delayTimeMs = 1000d * (DelayTime_SMA.Current ?? 0d),
				synchronizeTimeMs = 1000d * (SynchronizeTime_SMA.Current ?? 0d),
				writeTimeMs = 1000d * (WriteTime_SMA.Current ?? 0d),
				refreshTimeMs = 1000d * (RefreshTime_SMA.Current ?? 0d),
				
				simulationTimeMs = 1000d * (SimulationTime_SMA.Current ?? 0d),
				rasterizationTimeMs = 1000d * (RasterizeTime_SMA.Current ?? 0d),
				quadtreeTimeMs = 1000d * (QuadtreeTime_SMA.Current ?? 0d),
				autoscaleTimeMs = 1000d * (AutoscaleTime_SMA.Current ?? 0d);

			Tuple<string, ConsoleColor>[] values = new Tuple<string, ConsoleColor>[] {
				new Tuple<string, ConsoleColor>(
					fps.ToString_Number2(NUMBER_ACCURACY, true, NUMBER_SPACING).PadCenter(NUMBER_SPACING),
					ChooseFpsColor(fps)),
				new Tuple<string, ConsoleColor>(
					frameTimeMs.ToString_Number2(NUMBER_ACCURACY, true, NUMBER_SPACING).PadCenter(NUMBER_SPACING),
					ChooseFrameIntervalColor(frameTimeMs)),
				new Tuple<string, ConsoleColor>(
					updateTimeMs.ToString_Number2(NUMBER_ACCURACY, true, NUMBER_SPACING).PadCenter(NUMBER_SPACING),
					ChooseFrameIntervalColor(updateTimeMs)),
				new Tuple<string, ConsoleColor>(
					delayTimeMs.ToString_Number2(NUMBER_ACCURACY, true, NUMBER_SPACING).PadCenter(NUMBER_SPACING),
					ChooseFrameIntervalColor(delayTimeMs)),
				new Tuple<string, ConsoleColor>(
					synchronizeTimeMs.ToString_Number2(NUMBER_ACCURACY, true, NUMBER_SPACING).PadCenter(NUMBER_SPACING),
					ChooseFrameIntervalColor(synchronizeTimeMs)),
				new Tuple<string, ConsoleColor>(
					writeTimeMs.ToString_Number2(NUMBER_ACCURACY, true, NUMBER_SPACING).PadCenter(NUMBER_SPACING),
					ChooseFrameIntervalColor(writeTimeMs)),
				new Tuple<string, ConsoleColor>(
					refreshTimeMs.ToString_Number2(NUMBER_ACCURACY, true, NUMBER_SPACING).PadCenter(NUMBER_SPACING),
					ChooseFrameIntervalColor(refreshTimeMs)),
				new Tuple<string, ConsoleColor>(
					simulationTimeMs.ToString_Number2(NUMBER_ACCURACY, true, NUMBER_SPACING).PadCenter(NUMBER_SPACING),
					ChooseFrameIntervalColor(simulationTimeMs)),
				new Tuple<string, ConsoleColor>(
					rasterizationTimeMs.ToString_Number2(NUMBER_ACCURACY, true, NUMBER_SPACING).PadCenter(NUMBER_SPACING),
					ChooseFrameIntervalColor(rasterizationTimeMs)),
				new Tuple<string, ConsoleColor>(
					quadtreeTimeMs.ToString_Number2(NUMBER_ACCURACY, true, NUMBER_SPACING).PadCenter(NUMBER_SPACING),
					ChooseFrameIntervalColor(quadtreeTimeMs)),
				new Tuple<string, ConsoleColor>(
					autoscaleTimeMs.ToString_Number2(NUMBER_ACCURACY, true, NUMBER_SPACING).PadCenter(NUMBER_SPACING),
					ChooseFrameIntervalColor(autoscaleTimeMs)),
			};

			int position = 0;
			for (int i = Parameters.PERF_GRAPH_ENABLE ? 1 : 0; i < DebugBarLabels.Length; i++) {
				for (int j = 0; j < DebugBarLabels[i].Length; j++)
					buffer[position + j] = new ConsoleExtensions.CharInfo(DebugBarLabels[i][j], ConsoleColor.White);
				position += DebugBarLabels[i].Length;

				for (int j = 0; j < values[i].Item1.Length; j++)
					buffer[position + j] = new ConsoleExtensions.CharInfo(values[i].Item1[j], values[i].Item2);
				position += values[i].Item1.Length;
			}
		}

		private static double[] _currentColumnData;
		private static SampleSMA _currentMin = new SampleSMA(Parameters.PERF_SMA_ALPHA);
		private static SampleSMA _currentMax = new SampleSMA(Parameters.PERF_SMA_ALPHA);
		private static ConsoleExtensions.CharInfo[][] _columns = new ConsoleExtensions.CharInfo[GRAPH_WIDTH][];
		private static BasicStatisticsInfo[] _columnStats = new BasicStatisticsInfo[GRAPH_WIDTH];
		public static void DrawFpsGraph(ConsoleExtensions.CharInfo[] buffer, int yOffset = 0) {
			if (ExecutionManager.FramesRendered > 0) {
				double numColumns = ExecutionManager.FramesRendered / Parameters.PERF_GRAPH_FRAMES_PER_COLUMN;
				int frameIdx = (ExecutionManager.FramesRendered - 1) % Parameters.PERF_GRAPH_FRAMES_PER_COLUMN;
				if (frameIdx == 0) {
					_currentColumnData = new double[Parameters.PERF_GRAPH_FRAMES_PER_COLUMN];
					_columns = _columns.ShiftRight(false);
					_columnStats = _columnStats.ShiftRight(false);
				}
				_currentColumnData[frameIdx] = 1d / IterationTime_SMA.LastUpdate.Value;
				_columnStats[0] = new BasicStatisticsInfo(_currentColumnData.Take(frameIdx + 1));

				double
					dataMin = _columnStats[0].Min,
					dataAvg = _columnStats[0].Mean,
					dataMax = _columnStats[0].Max;
				if (GRAPH_WIDTH > 2) {
					dataMin = _columnStats.Skip(numColumns > 2 ? 1 : 0).TakeUntil(s => s is null).Min(s => s.Percentile10);
					dataAvg = _columnStats.Skip(numColumns > 2 ? 1 : 0).TakeUntil(s => s is null).Average(s => s.Mean);//faster than true average calculation
					dataMax = _columnStats.Skip(numColumns > 2 ? 1 : 0).TakeUntil(s => s is null).Max(s => s.Percentile90);
					if (dataMin < 1) dataMin = 0;
					if (dataMin >= dataMax) dataMax = dataMin + 1;
				}

				bool recompute = _currentMin.NumUpdates == 0
					|| dataMin != _currentMin.Current.Value || dataMax != _currentMax.Current.Value;
				if (recompute) {
					_currentMin.Update(dataMin);
					_currentMax.Update(dataMax);
				}

				for (int i = 0; i < GRAPH_WIDTH; i++) {
					if (i == 0 || recompute) ComputeColumn(i);
					DrawColumn(buffer, i);
				}

				string
					label_current = (1d / IterationTime_SMA.LastUpdate.Value).ToString_Number3(3) + "fps",
					label_min = _currentMin.Current.Value.ToString_Number3(4),
					label_avg = dataAvg.ToString_Number3(4),
					label_max = _currentMax.Current.Value.ToString_Number3(4);

				for (int i = 0; i < label_max.Length; i++)
					buffer[i + Parameters.WIDTH*yOffset] = new ConsoleExtensions.CharInfo(label_max[i], ConsoleColor.Gray);
				for (int i = 0; i < label_min.Length; i++)
					buffer[i + Parameters.WIDTH * (yOffset + GRAPH_HEIGHT - 1)] = new ConsoleExtensions.CharInfo(label_min[i], ConsoleColor.Gray);

				int offset_avg = (int)(GRAPH_HEIGHT * (dataAvg - _currentMin.Current.Value) / (_currentMax.Current.Value - _currentMin.Current.Value));
				if (offset_avg >= 0 && offset_avg < GRAPH_HEIGHT)
					for (int i = 0; i < label_avg.Length; i++)
						buffer[i + Parameters.WIDTH * (yOffset + GRAPH_HEIGHT - 1 - offset_avg)] = new ConsoleExtensions.CharInfo(label_avg[i], ConsoleColor.Gray);

				int offset_current = (int)(GRAPH_HEIGHT * ((1d / IterationTime_SMA.Current.Value) - _currentMin.Current.Value) / (_currentMax.Current.Value - _currentMin.Current.Value));
				if (offset_current >= 0 && offset_current < GRAPH_HEIGHT) {
					ConsoleColor color_current = ChooseFpsColor(1d / IterationTime_SMA.LastUpdate.Value);
					for (int i = 0; i < label_current.Length; i++)
						buffer[i + Parameters.WIDTH * (yOffset + GRAPH_HEIGHT - 1 - offset_current)] = new ConsoleExtensions.CharInfo(label_current[i], color_current);
				}
			}
		}

		private static void DrawColumn(ConsoleExtensions.CharInfo[] buffer, int xIdx) {
			if (!(_columns[xIdx] is null))
				for (int yIdx = 0; yIdx < GRAPH_HEIGHT; yIdx++)
					if (!Equals(_columns[xIdx][yIdx], default(ConsoleExtensions.CharInfo)))
						buffer[xIdx + (GRAPH_HEIGHT - yIdx)*Parameters.WIDTH] = _columns[xIdx][yIdx];
		}

		private static void ComputeColumn(int xIdx) {
			if (!(_columnStats[xIdx] is null)) {
				_columns[xIdx] = new ConsoleExtensions.CharInfo[GRAPH_HEIGHT];

				double
					yMin0 = _columnStats[xIdx].Percentile0,
					yMin10 = _columnStats[xIdx].Percentile10,
					yMin25 = _columnStats[xIdx].Percentile25,
					yAvg50 = _columnStats[xIdx].Mean,
					YMax75 = _columnStats[xIdx].Percentile75,
					YMax90 = _columnStats[xIdx].Percentile90,
					yMax100 = _columnStats[xIdx].Percentile100;
				double
					yMin0Scaled = GRAPH_HEIGHT * (yMin0 - _currentMin.Current.Value) / (_currentMax.Current.Value - _currentMin.Current.Value),
					yMin10caled = GRAPH_HEIGHT * (yMin10 - _currentMin.Current.Value) / (_currentMax.Current.Value - _currentMin.Current.Value),
					yMin25caled = GRAPH_HEIGHT * (yMin25 - _currentMin.Current.Value) / (_currentMax.Current.Value - _currentMin.Current.Value),
					yAvg50Scaled = GRAPH_HEIGHT * (yAvg50 - _currentMin.Current.Value) / (_currentMax.Current.Value - _currentMin.Current.Value),
					YMax75Scaled = GRAPH_HEIGHT * (YMax75 - _currentMin.Current.Value) / (_currentMax.Current.Value - _currentMin.Current.Value),
					YMax90Scaled = GRAPH_HEIGHT * (YMax90 - _currentMin.Current.Value) / (_currentMax.Current.Value - _currentMin.Current.Value),
					yMax100Scaled = GRAPH_HEIGHT * (yMax100 - _currentMin.Current.Value) / (_currentMax.Current.Value - _currentMin.Current.Value);
				
				int
					minY = yMin0Scaled < 0 ? 0 : (int)Math.Floor(yMin0Scaled),
					maxY = yMax100Scaled > GRAPH_HEIGHT ? GRAPH_HEIGHT : (int)Math.Ceiling(yMax100Scaled);
				ConsoleColor color; char chr;
				for (int yIdx = minY; yIdx < maxY; yIdx++) {
					if ((int)yMax100Scaled == yIdx) {//top pixel
						if (yMax100Scaled % 1d < 0.5d)//bottom half
							chr = Rasterizer.CHAR_BOTTOM;
						else if (yMin0Scaled >= yIdx + 0.5d)//top half
							chr = Rasterizer.CHAR_TOP;
						else chr = Rasterizer.CHAR_BOTH;
					} else if ((int)yMin0Scaled == yIdx) {//bottom pixel
						if (yMin0Scaled % 1d >= 0.5d)//top half
								chr = Rasterizer.CHAR_TOP;
						else if (yMax100Scaled < yIdx + 0.5d)//bottom half
							chr = Rasterizer.CHAR_BOTTOM;
						else chr = Rasterizer.CHAR_BOTH;
					} else chr = Rasterizer.CHAR_BOTH;

					switch (yIdx.CompareTo((int)yAvg50Scaled)) {
						case -1://bottom stat
							if ((int)yMin10caled > yIdx)
								color = ConsoleColor.DarkGray;
							else if ((int)yMin25caled > yIdx)
								color = ConsoleColor.Gray;
							else color = ConsoleColor.White;
							break;
						case 0://average
							color = ChooseFpsColor(yAvg50);
							break;
						case 1://top stat
							if ((int)YMax90Scaled < yIdx)
								color = ConsoleColor.DarkGray;
							else if ((int)YMax75Scaled < yIdx)
								color = ConsoleColor.Gray;
							else color = ConsoleColor.White;
							break;
						default:
							throw new ImpossibleCompareToException();
					}

					_columns[xIdx][yIdx] = new ConsoleExtensions.CharInfo(chr, color);
				}
			}
		}

		public static void WriteEnd() {
			TimeSpan totalDuration = ExecutionManager.EndTime.Subtract(ExecutionManager.StartTime);
			
			Console.SetCursorPosition(0, 1);
			Console.ForegroundColor = ConsoleColor.White;
			Console.BackgroundColor = ConsoleColor.Black;
			Console.WriteLine("---END--- Duration {0:G3}s", ExecutionManager.EndTime.Subtract(ExecutionManager.StartTime).TotalSeconds);
			
			Console.Write("Simulated ");
			
			Console.ForegroundColor = ChooseColor(Math.Pow((double)Parameters.RATED_BOIDS / Program.TotalBoids, 2));
			Console.Write(Program.TotalBoids);

			Console.ForegroundColor = ConsoleColor.White;
			Console.Write(" {0} across {1} for {2} averaging ",
				"boid".Pluralize(Program.TotalBoids),
				Parameters.NUM_FLOCKS.Pluralize("flock"),
				ExecutionManager.FramesRendered.Pluralize("frame")
					+ (Parameters.SUBFRAME_MULTIPLE < 2
						? ""
						: " and " + ExecutionManager.FramesSimulated.Pluralize("simulation steps")));

			double fps = (double)ExecutionManager.FramesRendered / totalDuration.TotalSeconds;
			Console.ForegroundColor = ChooseFpsColor(fps);
			Console.Write(fps.ToString_Number2(4));
			Console.ForegroundColor = ConsoleColor.White;
			Console.Write(" FPS");

			if (Parameters.UPDATE_INTERVAL_MS > 0) {
				double expectedFramesRendered = (double)totalDuration.TotalMilliseconds / Parameters.UPDATE_INTERVAL_MS;
				double fpsRatio = (double)ExecutionManager.FramesRendered / ((int)(1 + expectedFramesRendered));
				
				Console.Write(" ({0:G3}% of {1} fps)",
					100 * fpsRatio,
					Parameters.TARGET_FPS);
			}

			Console.WriteLine();
			Console.ResetColor();
			ConsoleExtensions.WaitForEnter("Press <Enter> to exit");
		}

		public static readonly Tuple<double, ConsoleColor>[] RatioColors = new Tuple<double, ConsoleColor>[] {
			new Tuple<double, ConsoleColor>(1.05d, ConsoleColor.Cyan),
			new Tuple<double, ConsoleColor>(0.95d, ConsoleColor.Green),
			new Tuple<double, ConsoleColor>(0.80d, ConsoleColor.DarkGreen),
			new Tuple<double, ConsoleColor>(0.67d, ConsoleColor.Yellow),
			new Tuple<double, ConsoleColor>(0.50d, ConsoleColor.DarkYellow),
			new Tuple<double, ConsoleColor>(0.33d, ConsoleColor.Magenta),
			new Tuple<double, ConsoleColor>(0.25d, ConsoleColor.Red),
			new Tuple<double, ConsoleColor>(0.10d, ConsoleColor.DarkRed),
			new Tuple<double, ConsoleColor>(0.00d, ConsoleColor.DarkRed),
			new Tuple<double, ConsoleColor>(double.NegativeInfinity, ConsoleColor.White)
		};

		private static ConsoleColor ChooseColor(double ratioToDesired) {
			foreach (Tuple<double, ConsoleColor> rank in RatioColors) {
				if (ratioToDesired >= rank.Item1) return rank.Item2;
			}
			return ConsoleColor.White;
		}
		private static ConsoleColor ChooseFpsColor(double fps) {
			double ratioToDesired = fps / (Parameters.TARGET_FPS > 0 ? Parameters.TARGET_FPS : 30d);
			return ChooseColor(ratioToDesired);
		}
		private static ConsoleColor ChooseFrameIntervalColor(double timeMs) {
			double ratioToDesired = 1000d / (Parameters.TARGET_FPS > 0 ? Parameters.TARGET_FPS : 30d) / timeMs;
			return ChooseColor(ratioToDesired);
		}
	}
}