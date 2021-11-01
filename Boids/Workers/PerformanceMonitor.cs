using System;
using System.Collections.Generic;
using System.Linq;
using Generic;

namespace Boids {
	internal static class PerformanceMonitor {
		public const int NUMBER_ACCURACY = 2;
		public const int NUMBER_SPACING = 5;

		public const int GRAPH_WIDTH = 67;
		public const int GRAPH_HEIGHT = 12;

		public static SampleSMA IterationTime_SMA = new SampleSMA(Parameters.PERF_SMA_ALPHA);
		public static SampleSMA FrameTime_SMA = new SampleSMA(Parameters.PERF_SMA_ALPHA);
		public static SampleSMA WriteTime_SMA = new SampleSMA(Parameters.PERF_SMA_ALPHA);
		public static SampleSMA RefreshTime_SMA = new SampleSMA(Parameters.PERF_SMA_ALPHA);
		public static SampleSMA DelayTime_SMA = new SampleSMA(Parameters.PERF_SMA_ALPHA);
		public static SampleSMA UpdateTime_SMA = new SampleSMA(Parameters.PERF_SMA_ALPHA);

		public static SampleSMA SimulationTime_SMA = new SampleSMA(Parameters.PERF_SMA_ALPHA);
		public static SampleSMA RasterizeTime_SMA = new SampleSMA(Parameters.PERF_SMA_ALPHA);
		public static SampleSMA QuadtreeTime_SMA = new SampleSMA(Parameters.PERF_SMA_ALPHA);
		public static SampleSMA AutoscaleTime_SMA = new SampleSMA(Parameters.PERF_SMA_ALPHA);

		public readonly static string[] DebugBarLabels = new string[] {
			"FPS",
			"Time(ms)",
			"D",
			"| W",
			"R",
			"| U",
			"S",
			"R",
			"| Q",
			"A"};

		public static void DrawStatsHeader(ConsoleExtensions.CharInfo[] buffer) {
			double
				fps = 1d / IterationTime_SMA.Current ?? 0,
				frameTimeMs = 1000d * (FrameTime_SMA.Current ?? 0d),
				delayTimeMs = 1000d * (DelayTime_SMA.Current ?? 0d),
				writeTimeMs = 1000d * (WriteTime_SMA.Current ?? 0d),
				refreshTimeMs = 1000d * (RefreshTime_SMA.Current ?? 0d),
				
				updateTimeMs = 1000d * (UpdateTime_SMA.Current ?? 0d),
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
					delayTimeMs.ToString_Number2(NUMBER_ACCURACY, true, NUMBER_SPACING).PadCenter(NUMBER_SPACING),
					ChooseFrameIntervalColor(delayTimeMs)),
				new Tuple<string, ConsoleColor>(
					writeTimeMs.ToString_Number2(NUMBER_ACCURACY, true, NUMBER_SPACING).PadCenter(NUMBER_SPACING),
					ChooseFrameIntervalColor(writeTimeMs)),
				new Tuple<string, ConsoleColor>(
					refreshTimeMs.ToString_Number2(NUMBER_ACCURACY, true, NUMBER_SPACING).PadCenter(NUMBER_SPACING),
					ChooseFrameIntervalColor(refreshTimeMs)),
				new Tuple<string, ConsoleColor>(
					updateTimeMs.ToString_Number2(NUMBER_ACCURACY, true, NUMBER_SPACING).PadCenter(NUMBER_SPACING),
					ChooseFrameIntervalColor(updateTimeMs)),
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

		private static double[] _currentHistory;
		private static ConsoleExtensions.CharInfo[][] _columns = new ConsoleExtensions.CharInfo[GRAPH_WIDTH][];
		private static double[] _minValues = Enumerable.Repeat(double.PositiveInfinity, GRAPH_WIDTH).ToArray();
		private static double[] _maxValues = Enumerable.Repeat(double.NegativeInfinity, GRAPH_WIDTH).ToArray();
		public static void DrawFpsGraph(ConsoleExtensions.CharInfo[] buffer, int yOffset = 0) {
			if (ExecutionManager.FramesRendered > 0) {
				int frameIdx = (ExecutionManager.FramesRendered - 1) % Parameters.PERF_GRAPH_FRAMES_PER_COLUMN;

				if (frameIdx == 0) {
					_columns = ShiftRight(_columns);
					_minValues = ShiftRight(_minValues);
					_maxValues = ShiftRight(_maxValues);
					_columns[0] = new ConsoleExtensions.CharInfo[GRAPH_HEIGHT];

					_currentHistory = new double[Parameters.PERF_GRAPH_FRAMES_PER_COLUMN];
				}

				_currentHistory[frameIdx] = 1d / IterationTime_SMA.LastUpdate.Value;
				_minValues[0] = _currentHistory.Take(frameIdx + 1).Min();
				_maxValues[0] = _currentHistory.Take(frameIdx + 1).Max();

				double domainMin = _minValues.Min().RoundDown_Log(1.2);
				if (domainMin < 1) domainMin = 0;
				double domainMax = _maxValues.Max().RoundUp_Log(1.2);
				if (domainMin == domainMax) { domainMax++; }

				double
					yMin = _currentHistory.Take(frameIdx + 1).Min(),
					yMinCI = 0,
					yAvg = _currentHistory.Take(frameIdx + 1).Average(),
					YMaxCI = 0,
					yMax = _currentHistory.Take(frameIdx + 1).Max();
				double
					yMinScaled = GRAPH_HEIGHT * (yMin - domainMin) / (domainMax - domainMin),
					yMinCIScaled = GRAPH_HEIGHT * (yMinCI - domainMin) / (domainMax - domainMin),
					yAvgScaled = GRAPH_HEIGHT * (yAvg - domainMin) / (domainMax - domainMin),
					YMaxCIScaled = GRAPH_HEIGHT * (YMaxCI - domainMin) / (domainMax - domainMin),
					yMaxScaled = GRAPH_HEIGHT * (yMax - domainMin) / (domainMax - domainMin);
				
				ConsoleColor color; char chr;
				for (int y = (int)yMinScaled; y < yMaxScaled; y++) {
					if ((int)yMaxScaled == y) {
						if (yMaxScaled % 1d < 0.5d)
							chr = Rasterizer.CHAR_BOTTOM;
						else if (yMinScaled < y + 0.5d)
							chr = Rasterizer.CHAR_BOTH;
						else chr = Rasterizer.CHAR_TOP;
								
						if (yAvgScaled >= y)
							color = ChooseFpsColor(yAvg);
						else color = ConsoleColor.DarkGray;
					} else if ((int)yMinScaled < y) {
						chr = Rasterizer.CHAR_BOTH;
						if (yAvgScaled >= y && yAvgScaled < y + 1)
							color = ChooseFpsColor(yAvg);
						else color = ConsoleColor.DarkGray;
					} else {
						if (yMinScaled % 1d >= 0.5d)
							chr = Rasterizer.CHAR_TOP;
						else if (yMaxScaled >= y + 0.5d)
							chr = Rasterizer.CHAR_BOTH;
						else chr = Rasterizer.CHAR_BOTTOM;
								
						if (yAvgScaled < y + 1)
							color = ChooseFpsColor(yAvg);
						else color = ConsoleColor.DarkGray;
					}

					_columns[0][y] = new ConsoleExtensions.CharInfo(chr, color);
				}

				for (int x = 0; x < GRAPH_WIDTH; x++)
					if (!(_columns[x] is null))
						for (int y = 0; y < GRAPH_HEIGHT; y++)
							if (!object.Equals(_columns[x][y], default(ConsoleExtensions.CharInfo)))
								buffer[x + (GRAPH_HEIGHT - y)*Parameters.WIDTH] = _columns[x][y];

				string
					label_min = domainMin.ToString_Number3(4),
					label_mid = (1d / IterationTime_SMA.Current.Value).ToString_Number3(3) + "fps",
					label_max = domainMax.ToString_Number3(4);

				for (int i = 0; i < label_max.Length; i++)
					buffer[i + Parameters.WIDTH*yOffset] = new ConsoleExtensions.CharInfo(label_max[i], ConsoleColor.Gray);
				for (int i = 0; i < label_min.Length; i++)
					buffer[i + Parameters.WIDTH*(yOffset + GRAPH_HEIGHT)] = new ConsoleExtensions.CharInfo(label_min[i], ConsoleColor.Gray);
				
				ConsoleColor midLabelColor = ChooseFpsColor(1d / IterationTime_SMA.Current.Value);
				int midpointY = (int)(GRAPH_HEIGHT * ((1d / IterationTime_SMA.Current.Value) - domainMin) / (domainMax - domainMin));
				for (int i = 0; i < label_mid.Length; i++)
					buffer[i + Parameters.WIDTH*(yOffset + GRAPH_HEIGHT - midpointY - 1)] = new ConsoleExtensions.CharInfo(label_mid[i], midLabelColor);
			}
		}

		public static void WriteEnd() {
			TimeSpan simulationDuration = ExecutionManager.SimulationEndTime.Subtract(ExecutionManager.SimulationStartTime);
			
			Console.SetCursorPosition(0, 1);
			Console.ForegroundColor = ConsoleColor.White;
			Console.BackgroundColor = ConsoleColor.Black;
			Console.WriteLine("---END--- Duration {0:G3}s", ExecutionManager.SimulationEndTime.Subtract(ExecutionManager.SimulationStartTime).TotalSeconds);
			
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

			double fps = (double)ExecutionManager.FramesRendered / simulationDuration.TotalSeconds;
			Console.ForegroundColor = ChooseFpsColor(fps);
			Console.Write(fps.ToString_Number2(4));
			Console.ForegroundColor = ConsoleColor.White;
			Console.Write(" FPS");

			if (Parameters.UPDATE_INTERVAL_MS > 0) {
				double expectedFramesRendered = (double)simulationDuration.TotalMilliseconds / Parameters.UPDATE_INTERVAL_MS;
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
		private static T[] ShiftRight<T>(T[] arr) {
			T[] tmp = new T[arr.Length];

			//Array.Copy(arr, arr.Length-1, tmp, 0, 1); // move last position to first
			Array.Copy(arr, 0, tmp, 1, arr.Length-1); // copy over the rest

			return tmp;
		}
	}
}