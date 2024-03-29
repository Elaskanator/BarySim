﻿using System;
using System.Linq;
using Generic.Extensions;
using Generic.Classes;

namespace ParticleSimulator.Rendering.SystemConsole {
	public class PerfGraph {
		public PerfGraph(int width) {
			this.Width = width;

			_columnSimTimeStatsMs = new StatsInfo[this.Width];
			_columnFpsStatsMs = new StatsInfo[this.Width];
			_graphColumns = new ConsoleExtensions.CharInfo[this.Width][];
		}

		public readonly int Width;
		private readonly object _columnStatsLock = new object();
		
		private StatsInfo[] _columnSimTimeStatsMs;
		private StatsInfo[] _columnFpsStatsMs;
		private double[] _currentColumnFrameTimeDataMs;
		private double[] _currentColumnFpsDataMs;

		private double _graphMin = 0;
		private double _graphMax = 0;
		private ConsoleExtensions.CharInfo[][] _graphColumns;
		private DateTime _lastGraphRenderFrameUtc = DateTime.UtcNow;

		public void Update(int frameIdx, TimeSpan frameTime, TimeSpan fpsTime) {
			lock (_columnStatsLock) {
				if (frameIdx == 0) {
					_currentColumnFpsDataMs = new double[Parameters.MON_GRAPH_COLUMN_FRAMES];
					_currentColumnFrameTimeDataMs = new double[Parameters.MON_GRAPH_COLUMN_FRAMES];
					_graphColumns = _graphColumns.RotateRight();
					_columnFpsStatsMs = _columnFpsStatsMs.RotateRight();
					_columnSimTimeStatsMs = _columnSimTimeStatsMs.RotateRight();
				}

				_currentColumnFpsDataMs[frameIdx] = fpsTime.TotalMilliseconds;
				_columnFpsStatsMs[0] = new StatsInfo(_currentColumnFpsDataMs.Take(frameIdx + 1));

				_currentColumnFrameTimeDataMs[frameIdx] = frameTime.TotalMilliseconds;
				_columnSimTimeStatsMs[0] = new StatsInfo(_currentColumnFrameTimeDataMs.Take(frameIdx + 1));
			}
		}

		public void DrawFpsGraph(ConsoleExtensions.CharInfo[] frameBuffer, AIncrementalAverage<TimeSpan> frameTimings, AIncrementalAverage<TimeSpan> fpsTimings) {
			ConsoleExtensions.CharInfo[][] graphColumnsCopy;
			lock (_columnStatsLock) {
				if (_columnSimTimeStatsMs[0] is null)
					return;
				else if (_graphColumns[0] is null || DateTime.UtcNow.Subtract(_lastGraphRenderFrameUtc).TotalMilliseconds >= Parameters.MON_GRAPH_REFRESH_MS)
					RerenderGraph();
				graphColumnsCopy = _graphColumns.TakeUntil(s => s is null).ToArray();
			}
			int numCols = graphColumnsCopy.Length;
			
			ConsoleExtensions.CharInfo[] graphData = new ConsoleExtensions.CharInfo[Width * Parameters.MON_GRAPH_HEIGHT];

			for (int i = 0; i < graphColumnsCopy.Length; i++)
				DrawGraphColumn(graphData, graphColumnsCopy[i], i);

			double
				frameTime = frameTimings.Current.TotalMilliseconds,
				fpsTime = fpsTimings.Current.TotalMilliseconds,
				targetTime = 1000d / (Parameters.TARGET_FPS > 0 ? Parameters.TARGET_FPS : Parameters.MON_FPS_DEFAULT);
			string
				label_min = _graphMin < 1000 ? _graphMin.ToStringBetter(2, false) : (_graphMin / 1000).ToStringBetter(2, false),
				label_target = targetTime < 1000 ? targetTime.ToStringBetter(3, true): (targetTime / 1000).ToStringBetter(3, true),
				label_max = _graphMax < 1000 ? _graphMax.ToStringBetter(2, false) : (_graphMax / 1000).ToStringBetter(2, false),
				label_frameTime = frameTime < 1000 ? frameTime.ToStringBetter(2, false) + "ms": (frameTime / 1000).ToStringBetter(2, false) + "s",
				label_FpsTime = fpsTime < 1000 ? fpsTime.ToStringBetter(3, false) + "ms" : (fpsTime / 1000).ToStringBetter(3, false) + "s";

			int offset_fps = (int)(Parameters.MON_GRAPH_HEIGHT * (fpsTime - _graphMin) / (_graphMax - _graphMin));
			offset_fps = offset_fps < 0 ? 0 : offset_fps < Parameters.MON_GRAPH_HEIGHT ? offset_fps : Parameters.MON_GRAPH_HEIGHT - 1;

			int offset_targetTime = (int)(Parameters.MON_GRAPH_HEIGHT * (targetTime - _graphMin) / (_graphMax - _graphMin));

			int offset_frameTime = (int)(Parameters.MON_GRAPH_HEIGHT * (frameTime - _graphMin) / (_graphMax - _graphMin));
			offset_frameTime = offset_frameTime < 0 ? 0 : offset_frameTime < Parameters.MON_GRAPH_HEIGHT ? offset_frameTime : Parameters.MON_GRAPH_HEIGHT - 1;
			
			bool drawFps = true,
				drawFrameTime = offset_frameTime != offset_fps || frameTime > fpsTime,
				drawtargetTime = offset_targetTime != offset_frameTime && offset_targetTime != offset_fps
					&& offset_targetTime >= 0 && offset_targetTime < Parameters.MON_GRAPH_HEIGHT,
				drawMinTime = 0 != offset_frameTime && 0 != offset_fps && 0 != offset_targetTime,
				drawMaxTime = Parameters.MON_GRAPH_HEIGHT - 1 != offset_frameTime && Parameters.MON_GRAPH_HEIGHT - 1 != offset_fps && Parameters.MON_GRAPH_HEIGHT - 1 != offset_targetTime;
				
			int yOffset;
			if (drawMinTime) {
				numCols = numCols < label_min.Length ? label_min.Length : numCols;
				for (int x = 0; x < label_min.Length; x++) {
					yOffset = 0;
					graphData[x + Width * (Parameters.MON_GRAPH_HEIGHT - 1 - yOffset)] = new ConsoleExtensions.CharInfo(
						label_min[x], ConsoleColor.DarkBlue, graphData[x + Width * (Parameters.MON_GRAPH_HEIGHT - 1 - yOffset)].BackgroundColor);
				}
			}

			if (drawFrameTime) {
				numCols = numCols < label_frameTime.Length ? label_frameTime.Length : numCols;
				for (int x = 0; x < label_frameTime.Length; x++) {
					yOffset = offset_frameTime;
					graphData[x + Width * (Parameters.MON_GRAPH_HEIGHT - 1 - yOffset)] = new ConsoleExtensions.CharInfo(
						label_frameTime[x], ConsoleColor.White, offset_targetTime == offset_frameTime ? ConsoleColor.DarkYellow : ConsoleColor.DarkCyan);
				}
			}

			if (drawtargetTime) {
				numCols = numCols < label_target.Length ? label_target.Length : numCols;
				for (int x = 0; x < label_target.Length; x++) {
					yOffset = offset_targetTime;
					graphData[x + Width * (Parameters.MON_GRAPH_HEIGHT - 1 - yOffset)] = new ConsoleExtensions.CharInfo(
						label_target[x], ConsoleColor.Black, ConsoleColor.DarkYellow);
				}
			}

			if (drawFps) {
				numCols = numCols < label_FpsTime.Length ? label_FpsTime.Length : numCols;
				for (int x = 0; x < label_FpsTime.Length; x++) {
					yOffset = offset_fps;
					graphData[x + Width * (Parameters.MON_GRAPH_HEIGHT - 1 - yOffset)] = new ConsoleExtensions.CharInfo(
						label_FpsTime[x], ConsoleColor.White, offset_targetTime == offset_fps ? ConsoleColor.DarkYellow : ConsoleColor.DarkGreen);
				}
			}

			if (drawMaxTime) {
				numCols = numCols < label_max.Length ? label_max.Length : numCols;
				for (int x = 0; x < label_max.Length; x++) {
					yOffset = Parameters.MON_GRAPH_HEIGHT - 1;
					graphData[x + Width * (Parameters.MON_GRAPH_HEIGHT - 1 - yOffset)] = new ConsoleExtensions.CharInfo(
						label_max[x], ConsoleColor.DarkBlue, graphData[x + Width * (Parameters.MON_GRAPH_HEIGHT - 1 - yOffset)].BackgroundColor);
				}
			}

			frameBuffer.RegionMerge(Parameters.WINDOW_WIDTH, graphData, Width, 0, 1, numCols);
		}

		private void RerenderGraph() {
			StatsInfo[]
				frameTimeStats = _columnSimTimeStatsMs.Without(s => s is null).ToArray(),
				fpsStats = _columnFpsStatsMs.Without(s => s is null).ToArray();
			if (frameTimeStats.Length + fpsStats.Length > 0) {
				StatsInfo rangeStats = new(frameTimeStats.Concat(fpsStats).SelectMany(s => s.Data_asc));
			
				double
					min = rangeStats.GetPercentileValue(Parameters.MON_GRAPH_PERC_CUTOFF),
					max = rangeStats.GetPercentileValue(100d - Parameters.MON_GRAPH_PERC_CUTOFF),
					avg = frameTimeStats.Concat(fpsStats).Average(s => s.GetPercentileValue(50d));

				double minMedian, maxMedian;
				if (frameTimeStats.Length > 1 && fpsStats.Length > 1) {
					minMedian = frameTimeStats.Skip(1).Concat(fpsStats.Skip(1)).Min(s => s.GetPercentileValue(50d));
					maxMedian = frameTimeStats.Skip(1).Concat(fpsStats.Skip(1)).Max(s => s.GetPercentileValue(50d));
				} else {
					minMedian = frameTimeStats.Concat(fpsStats).Min(s => s.GetPercentileValue(50d));
					maxMedian = frameTimeStats.Concat(fpsStats).Max(s => s.GetPercentileValue(50d));
				}

				min = min < minMedian ? min : minMedian;
				max = max > maxMedian ? max : maxMedian;

				min = min < avg ? min : avg;
				max = max > avg ? max : avg;

				min = min > 1d ? min : 0f;
				max = max > min ? max : min + 1d;

				_graphMin = min;
				_graphMax = max;

				for (int i = 0; i < frameTimeStats.Length || i < fpsStats.Length; i++)
					_graphColumns[i] = RenderGraphColumn(_columnFpsStatsMs[i], _columnSimTimeStatsMs[i]);

			}
			_lastGraphRenderFrameUtc = DateTime.UtcNow;
		}

		private void DrawGraphColumn(ConsoleExtensions.CharInfo[] buffer, ConsoleExtensions.CharInfo[] newColumn, int xIdx) {
			for (int yIdx = 0; yIdx < Parameters.MON_GRAPH_HEIGHT; yIdx++)
				if (!Equals(newColumn[yIdx], default(ConsoleExtensions.CharInfo)))
					buffer[xIdx + (Parameters.MON_GRAPH_HEIGHT - yIdx - 1)*Width] = newColumn[yIdx];
		}
		private ConsoleExtensions.CharInfo[] RenderGraphColumn(StatsInfo fpsStatsMs, StatsInfo simTimeStatsMs) {
			ConsoleExtensions.CharInfo[] result = new ConsoleExtensions.CharInfo[Parameters.MON_GRAPH_HEIGHT];

			double
				yFps000 = fpsStatsMs.Min,
				yFps010 = fpsStatsMs.GetPercentileValue(10d, false),
				yFps025 = fpsStatsMs.GetPercentileValue(25d, false),
				yFps040 = fpsStatsMs.GetPercentileValue(40d, false),
				yFps050 = fpsStatsMs.GetPercentileValue(50d, false),
				yFps060 = fpsStatsMs.GetPercentileValue(60d, false),
				yFps075 = fpsStatsMs.GetPercentileValue(75d, false),
				yFps090 = fpsStatsMs.GetPercentileValue(90d, false),
				yFps100 = fpsStatsMs.Max,
				yTime000 = simTimeStatsMs.Min,
				yTime010 = simTimeStatsMs.GetPercentileValue(10d, false),
				yTime025 = simTimeStatsMs.GetPercentileValue(25d, false),
				yTime040 = simTimeStatsMs.GetPercentileValue(40d, false),
				yTime050 = simTimeStatsMs.GetPercentileValue(50d, false),
				yTime060 = simTimeStatsMs.GetPercentileValue(60d, false),
				yTime075 = simTimeStatsMs.GetPercentileValue(75d, false),
				yTime090 = simTimeStatsMs.GetPercentileValue(90d, false),
				yTime100 = simTimeStatsMs.Max;
			double fps = Parameters.TARGET_FPS > 0
				? Parameters.TARGET_FPS
				: Parameters.MON_FPS_DEFAULT;
			double
				yFps000Scaled =		2d*Parameters.MON_GRAPH_HEIGHT * (yFps000 - _graphMin) / (_graphMax - _graphMin),
				yFps010Scaled =		2d*Parameters.MON_GRAPH_HEIGHT * (yFps010 - _graphMin) / (_graphMax - _graphMin),
				yFps025Scaled =		2d*Parameters.MON_GRAPH_HEIGHT * (yFps025 - _graphMin) / (_graphMax - _graphMin),
				yFps040Scaled =		2d*Parameters.MON_GRAPH_HEIGHT * (yFps040 - _graphMin) / (_graphMax - _graphMin),
				yFps050Scaled =		2d*Parameters.MON_GRAPH_HEIGHT * (yFps050 - _graphMin) / (_graphMax - _graphMin),
				yFps060Scaled =		2d*Parameters.MON_GRAPH_HEIGHT * (yFps060 - _graphMin) / (_graphMax - _graphMin),
				yFps075Scaled =		2d*Parameters.MON_GRAPH_HEIGHT * (yFps075 - _graphMin) / (_graphMax - _graphMin),
				yFps090Scaled =		2d*Parameters.MON_GRAPH_HEIGHT * (yFps090 - _graphMin) / (_graphMax - _graphMin),
				yFps100Scaled =		2d*Parameters.MON_GRAPH_HEIGHT * (yFps100 - _graphMin) / (_graphMax - _graphMin),
				yTime000Scaled =	2d*Parameters.MON_GRAPH_HEIGHT * (yTime000 - _graphMin) / (_graphMax - _graphMin),
				yTime010Scaled =	2d*Parameters.MON_GRAPH_HEIGHT * (yTime010 - _graphMin) / (_graphMax - _graphMin),
				yTime025Scaled =	2d*Parameters.MON_GRAPH_HEIGHT * (yTime025 - _graphMin) / (_graphMax - _graphMin),
				yTime040Scaled =	2d*Parameters.MON_GRAPH_HEIGHT * (yTime040 - _graphMin) / (_graphMax - _graphMin),
				yTime050Scaled =	2d*Parameters.MON_GRAPH_HEIGHT * (yTime050 - _graphMin) / (_graphMax - _graphMin),
				yTime060Scaled =	2d*Parameters.MON_GRAPH_HEIGHT * (yTime060 - _graphMin) / (_graphMax - _graphMin),
				yTime075Scaled =	2d*Parameters.MON_GRAPH_HEIGHT * (yTime075 - _graphMin) / (_graphMax - _graphMin),
				yTime090Scaled =	2d*Parameters.MON_GRAPH_HEIGHT * (yTime090 - _graphMin) / (_graphMax - _graphMin),
				yTime100Scaled =	2d*Parameters.MON_GRAPH_HEIGHT * (yTime100 - _graphMin) / (_graphMax - _graphMin),
				yTargetTimeScaled = 2d*Parameters.MON_GRAPH_HEIGHT * ((1000d / fps) - _graphMin) / (_graphMax - _graphMin);
				
			ConsoleColor[] colors = new ConsoleColor[2*Parameters.MON_GRAPH_HEIGHT];
			for (int yIdx = 0; yIdx < 2*Parameters.MON_GRAPH_HEIGHT; yIdx++) {
				if (yIdx == (int)yFps050Scaled)
					colors[yIdx] = ConsoleColor.DarkGreen;
				else if (yIdx == (int)yTime050Scaled)
					colors[yIdx] = ConsoleColor.DarkCyan;

				else if (yIdx >= (int)yFps040Scaled && yIdx <= (int)yFps060Scaled)
					colors[yIdx] = ConsoleColor.Green;
				else if (yIdx >= (int)yTime040Scaled && yIdx <= (int)yTime060Scaled)
					colors[yIdx] = ConsoleColor.Cyan;
				
				else if (fps > 0d && yIdx >= (int)yTargetTimeScaled && yIdx < (int)yTime025Scaled) {
					if (yIdx < (int)yTime000Scaled)
						colors[yIdx] = ConsoleColor.DarkYellow;
					else colors[yIdx] = ConsoleColor.Yellow;
				}

				else if (yIdx >= (int)yFps025Scaled && yIdx <= (int)yFps075Scaled)
					colors[yIdx] = ConsoleColor.White;
				else if (yIdx >= (int)yTime025Scaled && yIdx <= (int)yTime075Scaled)
					colors[yIdx] = ConsoleColor.White;

				else if (yIdx >= (int)yFps010Scaled && yIdx <= (int)yFps090Scaled)
					colors[yIdx] = ConsoleColor.Gray;
				else if (yIdx >= (int)yTime010Scaled && yIdx <= (int)yTime090Scaled)
					colors[yIdx] = ConsoleColor.Gray;
				
				//else if (yIdx >= (int)yFps000Scaled && yIdx <= (int)yFps100Scaled)
				//	colors[yIdx] = ConsoleColor.DarkGray;
				//else if (yIdx >= (int)yTime000Scaled && yIdx <= (int)yTime100Scaled)
				//	colors[yIdx] = ConsoleColor.DarkGray;

				else if (fps > 0d && yIdx >= (int)yTargetTimeScaled && yIdx < (int)yFps050Scaled) {
					if (yIdx <= (int)yFps010Scaled)
						colors[yIdx] = ConsoleColor.Red;
					else if (yIdx <= (int)yFps025Scaled)
						colors[yIdx] = ConsoleColor.DarkRed;
					else if (yIdx <= (int)yFps040Scaled)
						colors[yIdx] = ConsoleColor.Magenta;
					else colors[yIdx] = ConsoleColor.DarkMagenta;
				}

				else colors[yIdx] = ConsoleColor.Black;
			}

			for (int i = 0; i < Parameters.MON_GRAPH_HEIGHT; i++)
				result[i] = ConsoleRenderer.BuildChar(colors[2*i], colors[2*i + 1]);

			return result;
		}
	}
}