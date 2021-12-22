using System;
using System.Linq;
using Generic.Extensions;
using Generic.Models;

namespace ParticleSimulator.ConsoleRendering {
	public class PerfGraph {
		public PerfGraph(int numStats) {
			int width =
				Parameters.PERF_STATS_ENABLE
					? Parameters.GRAPH_WIDTH > 0
						? Parameters.GRAPH_WIDTH
						: 3 + Parameters.NUMBER_SPACING
							+ (2 + numStats)
								* (1 + Parameters.NUMBER_SPACING)
					: Parameters.PERF_GRAPH_DEFAULT_WIDTH;
			GraphWidth = Console.WindowWidth > width ? width : Console.WindowWidth;

			_columnFrameTimeStatsMs = new StatsInfo[GraphWidth];
			_columnFpsStatsMs = new StatsInfo[GraphWidth];
			_graphColumns = new ConsoleExtensions.CharInfo[GraphWidth][];
		}

		public int GraphWidth;
		
		private StatsInfo[] _columnFrameTimeStatsMs;
		private StatsInfo[] _columnFpsStatsMs;
		private ConsoleExtensions.CharInfo[][] _graphColumns;
		private double[] _currentColumnFrameTimeDataMs;
		private double[] _currentColumnFpsDataMs;

		private double _graphMin = 0;
		private double _graphMax = 0;
		private DateTime _lastGraphRenderFrameUtc = DateTime.UtcNow;
		private readonly object _columnStatsLock = new object();

		public void Update(int frameIdx, double simTimeMs, double frameTimeMs) {
			lock (_columnStatsLock) {
				if (frameIdx == 0) {
					_currentColumnFpsDataMs = new double[Parameters.PERF_GRAPH_FRAMES_PER_COLUMN];
					_currentColumnFrameTimeDataMs = new double[Parameters.PERF_GRAPH_FRAMES_PER_COLUMN];
					_graphColumns = _graphColumns.RotateRight();
					_columnFpsStatsMs = _columnFpsStatsMs.RotateRight();
					_columnFrameTimeStatsMs = _columnFrameTimeStatsMs.RotateRight();
				}

				_currentColumnFpsDataMs[frameIdx] = frameTimeMs;
				_columnFpsStatsMs[0] = new StatsInfo(_currentColumnFpsDataMs.Take(frameIdx + 1));

				_currentColumnFrameTimeDataMs[frameIdx] = simTimeMs;
				_columnFrameTimeStatsMs[0] = new StatsInfo(_currentColumnFrameTimeDataMs.Take(frameIdx + 1));
			}
		}

		public void DrawFpsGraph(ConsoleExtensions.CharInfo[] frameBuffer, AIncrementalAverage<double> frameTimingMs, AIncrementalAverage<double> fpsTimingMs) {
			if (Program.StepEval_Render.ExclusiveTime.NumUpdates > 0) {
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
					frameTime = frameTimingMs.Current,
					fpsTime = fpsTimingMs.Current,
					targetTime = 1000d / (Parameters.TARGET_FPS > 0 ? Parameters.TARGET_FPS : Parameters.MAX_FPS);
				string
					label_min = _graphMin < 1000 ? _graphMin.ToStringBetter(2, false) : (_graphMin / 1000).ToStringBetter(2, false),
					label_target = targetTime < 1000 ? targetTime.ToStringBetter(3, true): (targetTime / 1000).ToStringBetter(3, true),
					label_max = _graphMax < 1000 ? _graphMax.ToStringBetter(2, false) : (_graphMax / 1000).ToStringBetter(2, false),
					label_frameTime = frameTime < 1000 ? frameTime.ToStringBetter(2, false): (frameTime / 1000).ToStringBetter(2, false),
					label_FpsTime = fpsTime < 1000 ? fpsTime.ToStringBetter(3, false) + "ms" : (fpsTime / 1000).ToStringBetter(3, false) + "s";
				
				int yOffset;
				Func<int, int, ConsoleColor> colorResolver = (x, yOffset) =>
					graphData[x + GraphWidth * (Parameters.GRAPH_HEIGHT - 1 - yOffset)].ForegroundColor;

				int offset_fps = (int)(Parameters.GRAPH_HEIGHT * (fpsTime - _graphMin) / (_graphMax - _graphMin));
				offset_fps = offset_fps < 0 ? 0 : offset_fps < Parameters.GRAPH_HEIGHT ? offset_fps : Parameters.GRAPH_HEIGHT - 1;
				int offset_targetTime = (int)(Parameters.GRAPH_HEIGHT * (targetTime - _graphMin) / (_graphMax - _graphMin));
				int offset_frameTime = (int)(Parameters.GRAPH_HEIGHT * (frameTime - _graphMin) / (_graphMax - _graphMin));
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

		private void RerenderGraph() {
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

				if (Math.Abs(min - _graphMin) / _graphMin > 0.05d)
					_graphMin = min;
				if (Math.Abs(max - _graphMax) / _graphMax > 0.05d)
					_graphMax = max;

				for (int i = 0; i < frameTimeStats.Length || i < iterationTimeStats.Length; i++)
					_graphColumns[i] = RenderGraphColumn(_columnFpsStatsMs[i], _columnFrameTimeStatsMs[i]);

			}
			_lastGraphRenderFrameUtc = DateTime.UtcNow;
		}

		private void DrawGraphColumn(ConsoleExtensions.CharInfo[] buffer, ConsoleExtensions.CharInfo[] newColumn, int xIdx) {
			for (int yIdx = 0; yIdx < Parameters.GRAPH_HEIGHT; yIdx++)
				if (!Equals(newColumn[yIdx], default(ConsoleExtensions.CharInfo)))
					buffer[xIdx + (Parameters.GRAPH_HEIGHT - yIdx - 1)*GraphWidth] = newColumn[yIdx];
		}
		private ConsoleExtensions.CharInfo[] RenderGraphColumn(StatsInfo fpsStatsMs, StatsInfo simTimeStatsMs) {
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
				yFps000Scaled = Parameters.GRAPH_HEIGHT * (yFps000 - _graphMin) / (_graphMax - _graphMin),
				yFps010Scaled = Parameters.GRAPH_HEIGHT * (yFps010 - _graphMin) / (_graphMax - _graphMin),
				yFps025Scaled = Parameters.GRAPH_HEIGHT * (yFps025 - _graphMin) / (_graphMax - _graphMin),
				yFps040Scaled = Parameters.GRAPH_HEIGHT * (yFps040 - _graphMin) / (_graphMax - _graphMin),
				yFps050Scaled = Parameters.GRAPH_HEIGHT * (yFps050 - _graphMin) / (_graphMax - _graphMin),
				yFps060Scaled = Parameters.GRAPH_HEIGHT * (yFps060 - _graphMin) / (_graphMax - _graphMin),
				yFps075Scaled = Parameters.GRAPH_HEIGHT * (yFps075 - _graphMin) / (_graphMax - _graphMin),
				yFps090Scaled = Parameters.GRAPH_HEIGHT * (yFps090 - _graphMin) / (_graphMax - _graphMin),
				yFps100Scaled = Parameters.GRAPH_HEIGHT * (yFps100 - _graphMin) / (_graphMax - _graphMin),
				yTime000Scaled = Parameters.GRAPH_HEIGHT * (yTime000 - _graphMin) / (_graphMax - _graphMin),
				yTime010Scaled = Parameters.GRAPH_HEIGHT * (yTime010 - _graphMin) / (_graphMax - _graphMin),
				yTime025Scaled = Parameters.GRAPH_HEIGHT * (yTime025 - _graphMin) / (_graphMax - _graphMin),
				yTime040Scaled = Parameters.GRAPH_HEIGHT * (yTime040 - _graphMin) / (_graphMax - _graphMin),
				yTime050Scaled = Parameters.GRAPH_HEIGHT * (yTime050 - _graphMin) / (_graphMax - _graphMin),
				yTime060Scaled = Parameters.GRAPH_HEIGHT * (yTime060 - _graphMin) / (_graphMax - _graphMin),
				yTime075Scaled = Parameters.GRAPH_HEIGHT * (yTime075 - _graphMin) / (_graphMax - _graphMin),
				yTime090Scaled = Parameters.GRAPH_HEIGHT * (yTime090 - _graphMin) / (_graphMax - _graphMin),
				yTime100Scaled = Parameters.GRAPH_HEIGHT * (yTime100 - _graphMin) / (_graphMax - _graphMin),
				yTargetTimeScaled = Parameters.GRAPH_HEIGHT * ((1000d / fps) - _graphMin) / (_graphMax - _graphMin);
			double
				y000Scaled = yFps000Scaled <= yTime000Scaled ? yFps000Scaled : yTime000Scaled,
				y100Scaled = yFps100Scaled >= yTime100Scaled ? yFps100Scaled : yTime100Scaled;
				
			ConsoleColor color; char chr;
			for (int yIdx = y000Scaled >= 0f ? (int)y000Scaled : 0; yIdx < (y100Scaled <= Parameters.GRAPH_HEIGHT ? y100Scaled : Parameters.GRAPH_HEIGHT); yIdx++) {
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
	}
}