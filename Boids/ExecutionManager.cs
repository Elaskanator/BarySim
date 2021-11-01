using System;
using System.Linq;
using System.Threading;
using Generic;
using Generic.Structures;

namespace Boids {
	internal static class ExecutionManager {
		#region Properties
		public static bool IsActive { get; private set; }
		public static int FramesSimulated { get; private set; }
		public static int FramesRasterized { get; private set; }
		public static int FramesRendered { get; private set; }
		public static DateTime StartTime { get; private set; }
		public static DateTime EndTime { get; private set; }
		public static DateTime IterationStart { get; private set; }
		public static DateTime IterationEnd { get; private set; }

		private static readonly AutoResetEvent _event_master = new AutoResetEvent(false);//keep the master thread alive until receiving a ConsoleCancelEvent
		private static readonly AutoResetEvent _event_debug = new AutoResetEvent(true);

		private static readonly AutoResetEvent _event_quadtree = new AutoResetEvent(true);
		private static readonly ManualResetEvent _event_quadtree_release = new ManualResetEvent(false);
		private static readonly AutoResetEvent _event_rasterize = new AutoResetEvent(false);
		private static readonly AutoResetEvent _event_rasterize_release = new AutoResetEvent(true);
		private static readonly AutoResetEvent _event_autoscale = new AutoResetEvent(false);
		private static readonly AutoResetEvent _event_autoscale_release = new AutoResetEvent(true);
		private static readonly AutoResetEvent _event_syncDraw = new AutoResetEvent(false);
		private static readonly AutoResetEvent _event_syncDraw_release = new AutoResetEvent(true);

		private static readonly Mutex _mutex_frameBuffer = new Mutex();//shared access to the monitor window for rendering
		
		private static Thread[] _workerThreads;
		#endregion Properties
		
		#region
		public static void Run() {
			Initialize();
			Start();
			_event_master.WaitOne(Parameters.RUNTIME_LIMIT_MS);//pause the thread until cancel action by user (ctrl+C or ctrl+break) or timeout after runtime limit
			Stop();
		}

		private static void Initialize() {
			FramesSimulated = 0;
			FramesRasterized = 0;
			FramesRendered = 0;
			
			_onscreenData = new ConsoleExtensions.CharInfo[Parameters.WIDTH * Parameters.HEIGHT];
			_workerThreads = new Thread[] {
				new Thread(AutoscaleThread),
				new Thread(MonitoringThread),
				new Thread(DrawSynchronizationThread),
				new Thread(QuadtreeThread),
				new Thread(RasterizeThread),
				new Thread(SimulateThread)
			};

			Console.CancelKeyPress += new ConsoleCancelEventHandler(CancelAction);

			#pragma warning disable CA1416//just shut the FUCK up
			Console.WindowWidth = Parameters.WIDTH;
			Console.WindowHeight = Parameters.HEIGHT;
			ConsoleExtensions.HideScrollbars();
			ConsoleExtensions.DisableAllResizing();//prevent user from resizing (or maximizing) the window, messing up the console buffer/size and desyncing rendered sprites
			//ConsoleExtensions.SetWindowPosition(0, 0);

			Console.CursorVisible = false;
		}

		private static void Start() {
			StartTime = IterationStart = DateTime.Now;

			IsActive = true;
			
			foreach (Thread t in _workerThreads) t.Start();
		}

		private static void Stop() {
			EndTime = DateTime.Now;
			IsActive = false;

			foreach (Thread t in _workerThreads) t.Join(100);

			if (Parameters.DEBUG_ENABLE) PerformanceMonitor.WriteEnd();

			Environment.Exit(0);
		}

		private static void CancelAction(object sender, ConsoleCancelEventArgs args) {//ctrl+C
			if (IsActive) {
				args.Cancel = true;//keep master thread alive for results output (if enabled)
				_event_master.Set();
			} else Environment.Exit(0);
		}
		#endregion

		private static DateTime _lastWrite = DateTime.MinValue;
		private static DateTime _targetUpdateTime = DateTime.MinValue;
		private static DateTime[] _newDataStarts = new DateTime[4];
		private static readonly QuadTree<Boid> _tree = new QuadTree<Boid>(Parameters.Domain);
		private static double[][] _positions;
		private static Tuple<char, double>[] _rasterization;
		private static ConsoleExtensions.CharInfo[] _frameBuffer;
		private static ConsoleExtensions.CharInfo[] _onscreenData;

		private static void WriteConsoleOutput() {
			DateTime start = DateTime.Now;
			ConsoleExtensions.WriteConsoleOutput(_onscreenData);
			DateTime end = DateTime.Now;
			PerformanceMonitor.RefreshTime_SMA.Update(end.Subtract(start).TotalSeconds);
		}

		private static void DrawSynchronize() {//for the target FPS and minimum duration visible
			DateTime now = DateTime.Now;

			double scheduledWaitMs =
				_targetUpdateTime.Subtract(now).TotalMilliseconds
				- 1000*(PerformanceMonitor.RefreshTime_SMA.Current ?? 0d);//"rush" it to not be late most the time
			double requiredWaitMs =
				Parameters.MIN_DISPLAY_TIME_MS
				- now.Subtract(IterationEnd).TotalMilliseconds;

			double maxWaitMs = scheduledWaitMs > requiredWaitMs ? scheduledWaitMs : requiredWaitMs;

			if (IsActive && maxWaitMs >= 1) Thread.Sleep((int)maxWaitMs);
		}

		//in rough order of which runs first
		#region Thread runners
		private static void QuadtreeThread() {
			DateTime start, end;
			while (IsActive) {
				_event_quadtree.WaitOne();
				start = DateTime.Now;

				_tree.Clear();
				foreach (Boid b in Program.AllBoids)
					_tree.Add(b);

				end = DateTime.Now;
				PerformanceMonitor.QuadtreeTime_SMA.Update(end.Subtract(start).TotalSeconds);

				_event_quadtree_release.Set();
				_event_debug.Set();
			}
		}

		private static void SimulateThread() {
			DateTime start = DateTime.Now, calcStart, end;
			while (IsActive) {
				if (FramesSimulated % Parameters.SUBFRAME_MULTIPLE == 0)
					_newDataStarts[(FramesSimulated / Parameters.SUBFRAME_MULTIPLE) % 4] = DateTime.Now;
				_event_quadtree_release.WaitOne();
				calcStart = DateTime.Now;

				Simulator.Update(Program.AllBoids, _tree);

				end = DateTime.Now;
				PerformanceMonitor.SimulationTime_SMA.Update(end.Subtract(calcStart).TotalSeconds);
				_event_debug.Set();

				if ((FramesSimulated - 1) % Parameters.QUADTREE_REFRESH_FRAMES == 0) {
					_event_quadtree_release.Reset();
					_event_quadtree.Set();
				}

				if (FramesSimulated % Parameters.SUBFRAME_MULTIPLE == 0) {
					_event_rasterize_release.WaitOne();
					_positions = Program.AllBoids.Select(b => (double[])b.Coordinates.Clone()).ToArray();
					_event_rasterize.Set();

					start = DateTime.Now;
				}
				
				FramesSimulated++;
			}
		}

		private static void RasterizeThread() {
			DateTime start, end;
			while (IsActive) {
				_event_rasterize.WaitOne();
				start = DateTime.Now;

				Tuple<char, double>[] rasterization = Rasterizer.Rasterize(_positions);

				_event_rasterize_release.Set();

				if (Parameters.DENSITY_AUTOSCALE_ENABLE && FramesRasterized % Parameters.AUTOSCALING_REFRESH_FRAMES == 0) {
					_event_autoscale_release.WaitOne();
					_rasterization = rasterization;
					_event_autoscale.Set();
				}
				
				ConsoleExtensions.CharInfo[] frameData = rasterization
					.Select(t => t is null ? default :
						new ConsoleExtensions.CharInfo(
							t.Item1,
							Rasterizer.ChooseDensityColor(t.Item2)))
					.ToArray();
				
				_event_debug.Set();
				end = DateTime.Now;
				PerformanceMonitor.RasterizeTime_SMA.Update(end.Subtract(start).TotalSeconds);

				_event_syncDraw_release.WaitOne();
				_frameBuffer = frameData;
				_event_syncDraw.Set();

				FramesRasterized++;
			}
		}
		
		private static void AutoscaleThread() {
			DateTime start, end;
			while (IsActive) {
				_event_autoscale.WaitOne();
				start = DateTime.Now;
				
				Rasterizer.AutoscaleUpdate(_rasterization);

				end = DateTime.Now;
				_event_autoscale_release.Set();
				PerformanceMonitor.AutoscaleTime_SMA.Update(end.Subtract(start).TotalSeconds);
			}
		}
		
		private static void DrawSynchronizationThread() {
			DateTime start, overlay, synchronize, end;
			while (IsActive) {
				_event_syncDraw.WaitOne();
				start = DateTime.Now;

				ConsoleExtensions.CharInfo[] frameData = (ConsoleExtensions.CharInfo[])_frameBuffer.Clone();
				_event_syncDraw_release.Set();

				if (Parameters.LEGEND_ENABLE) Rasterizer.DrawLegend(frameData);
				if (Parameters.DEBUG_ENABLE) {
					if (Parameters.PERF_STATS_ENABLE) PerformanceMonitor.DrawStatsHeader(frameData);
					if (Parameters.PERF_GRAPH_ENABLE) PerformanceMonitor.DrawFpsGraph(frameData, Parameters.PERF_STATS_ENABLE ? 1 : 0);
				}

				overlay = DateTime.Now;

				DrawSynchronize();
				if (!IsActive) return;

				synchronize = DateTime.Now;
				_mutex_frameBuffer.WaitOne();

				_onscreenData = frameData;
				WriteConsoleOutput();

				_mutex_frameBuffer.ReleaseMutex();
				end = IterationEnd = _lastWrite = DateTime.Now;

				_targetUpdateTime = _targetUpdateTime.AddMilliseconds(Parameters.UPDATE_INTERVAL_MS);
				if (_targetUpdateTime < end) _targetUpdateTime = end;

				PerformanceMonitor.DelayTime_SMA.Update(start.Subtract(IterationStart).TotalSeconds);
				PerformanceMonitor.SynchronizeTime_SMA.Update(synchronize.Subtract(overlay).TotalSeconds);
				PerformanceMonitor.UpdateTime_SMA.Update(end.Subtract(_newDataStarts[FramesRendered % 4]).TotalSeconds);
				PerformanceMonitor.WriteTime_SMA.Update(end.Subtract(synchronize).Add(overlay.Subtract(start)).TotalSeconds);
				PerformanceMonitor.FrameTime_SMA.Update(end.Subtract(synchronize).Add(overlay.Subtract(IterationStart)).TotalSeconds);
				PerformanceMonitor.IterationTime_SMA.Update(end.Subtract(IterationStart).TotalSeconds);
				
				FramesRendered++;
				IterationStart = DateTime.Now;
			}
		}

		private static void MonitoringThread() {
			bool anyUpdate;
			string slowWarningMessage;
			while (IsActive) {
				anyUpdate = _event_debug.WaitOne(Parameters.PERF_MAX_INTERVAL_MS);

				if (!IsActive || DateTime.Now.Subtract(_lastWrite).TotalMilliseconds >= Parameters.PERF_MIN_INTERVAL_MS) {
					if (_mutex_frameBuffer.WaitOne(0)) {//defer to the main render
						if (!anyUpdate) {
							slowWarningMessage = "No update for "
								+ (DateTime.Now.Subtract(IterationStart).TotalSeconds.ToString_Number2(2, true) + "s").PadRight(8);
							int inc = 0;
							if (Parameters.DEBUG_ENABLE && Parameters.PERF_STATS_ENABLE) {
								PerformanceMonitor.DrawStatsHeader(_onscreenData);
								inc = 1;
							}
							for (int i = 0; i < slowWarningMessage.Length; i++)
								_onscreenData[i + Parameters.WIDTH*inc] = new ConsoleExtensions.CharInfo(slowWarningMessage[i], ConsoleColor.Red);
						}
						WriteConsoleOutput();
						_mutex_frameBuffer.ReleaseMutex();
						_lastWrite = DateTime.Now;
					}
				}
			}
		}
		#endregion Thread runners
	}
}