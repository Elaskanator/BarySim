using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Generic;
using Generic.Structures;

namespace Boids {
	internal static class ExecutionManager {
		/*
		//public static bool IsActive { get; private set; }
		public static DateTime StartTime { get; private set; }
		public static DateTime EndTime { get; private set; }
		public static Fraction UpdateIntervalSec { get; private set; }

		#region Fields
		private static readonly AutoResetEvent _event_master = new AutoResetEvent(false);//keep the master thread alive until receiving a ConsoleCancelEvent
		private static readonly AutoResetEvent _event_debug = new AutoResetEvent(true);

		private static readonly AutoResetEvent _event_quadtree = new AutoResetEvent(true);
		private static readonly ManualResetEvent _event_quadtree_release = new ManualResetEvent(false);
		private static readonly AutoResetEvent _event_rasterize = new AutoResetEvent(false);
		private static readonly AutoResetEvent _event_rasterize_release = new AutoResetEvent(true);
		private static readonly AutoResetEvent _event_autoscale = new AutoResetEvent(false);
		private static readonly AutoResetEvent _event_autoscale_release = new AutoResetEvent(true);
		private static readonly AutoResetEvent _event_pushFrame = new AutoResetEvent(false);
		private static readonly AutoResetEvent _event_syncDraw_release = new AutoResetEvent(true);

		private static readonly Mutex _mutex_frameBuffer = new Mutex();//shared access to the monitor window for rendering
		private static readonly Mutex _mutex_frameOverlay = new Mutex();//shared access to the monitor window for rendering
		
		private static DateTime _lastStatsGraphWrite = DateTime.MinValue;
		private static DateTime _targetUpdateTime = DateTime.MinValue;
		private static DateTime[] _newDataStarts = new DateTime[4];
		private static readonly QuadTree<Boid> _tree = new QuadTree<Boid>(Parameters.Domain);
		private static double[][] _positions;
		private static Tuple<char, double>[] _rasterization;
		private static ConsoleExtensions.CharInfo[] _frameBuffer;
		private static ConsoleExtensions.CharInfo[] _overlayBuffer;
		#endregion Fields
		
		#region
		public static void Run() {
			Initialize();
			Start();
			_event_master.WaitOne(Parameters.RUNTIME_LIMIT_MS);//pause the thread until cancel action by user (ctrl+C or ctrl+break) or timeout after runtime limit
			Stop();
		}

		private static void Initialize() {
			UpdateIntervalSec = Parameters.TARGET_FPS <= 0 ? new Fraction(0) : new Fraction(1, Parameters.TARGET_FPS);
			_frameBuffer = new ConsoleExtensions.CharInfo[Parameters.WINDOW_WIDTH*Parameters.WINDOW_HEIGHT];

			

			Console.CancelKeyPress += new ConsoleCancelEventHandler(CancelAction);

			#pragma warning disable CA1416//just shut the FUCK up
			Console.WindowWidth = Parameters.WINDOW_WIDTH;
			Console.WindowHeight = Parameters.WINDOW_HEIGHT;
			ConsoleExtensions.HideScrollbars();
			ConsoleExtensions.DisableAllResizing();//prevent user from resizing (or maximizing) the window, messing up the console buffer/size and desyncing rendered sprites
			//ConsoleExtensions.SetWindowPosition(0, 0);

			Console.CursorVisible = false;
		}

		private static void Start() {
			StartTime = PerfMon.IterationStart = DateTime.Now;

			IsActive = true;
			
			//foreach (Thread t in _workerThreads) t.Start();
		}

		private static void Stop() {
			EndTime = DateTime.Now;
			IsActive = false;

			//foreach (Thread t in _workerThreads) t.Join(100);

			if (Parameters.DEBUG_ENABLE) PerfMon.WriteEnd();

			Environment.Exit(0);
		}

		private static void CancelAction(object sender, ConsoleCancelEventArgs args) {//ctrl+C
			if (IsActive) {
				args.Cancel = true;//keep master thread alive for results output (if enabled)
				_event_master.Set();
			} else Environment.Exit(0);
		}
		#endregion


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
				PerfMon.QuadtreeTime_SMA.Update(end.Subtract(start).TotalSeconds);

				_event_quadtree_release.Set();
				_event_debug.Set();
			}
		}

		private static void SimulateThread() {
			DateTime start = DateTime.Now, calcStart, end;
			while (IsActive) {
				if (PerfMon.FramesSimulated % Parameters.SUBFRAME_MULTIPLE == 0)
					_newDataStarts[(PerfMon.FramesSimulated / Parameters.SUBFRAME_MULTIPLE) % 4] = DateTime.Now;
				_event_quadtree_release.WaitOne();
				calcStart = DateTime.Now;
				if (Parameters.QUADTREE_HYBRID_METHOD) {
					if (Parameters.ENABLE_PARALLELISM) Parallel.ForEach(_tree.GetLeaves(), leaf =>
						{ foreach (Boid b in leaf.AllMembers) b.UpdateDeltas(leaf.GetNeighborsAlt(b, Parameters.QUADTREE_INCREASED_ACCURACY)); });
					else foreach (Boid b in Program.AllBoids) b.UpdateDeltas(_tree.GetNeighborsAlt(b));
				} else {
					if (Parameters.ENABLE_PARALLELISM) Parallel.ForEach(Program.AllBoids, b =>
						{ b.UpdateDeltas(_tree.GetNeighbors(b, b.Vision)); });
					else foreach (QuadTree<Boid> leaf in _tree.GetLeaves()) foreach (Boid b in leaf.AllMembers) b.UpdateDeltas(leaf.GetNeighborsAlt(b));
				}
				end = DateTime.Now;
				PerfMon.SimulationTime_SMA.Update(end.Subtract(calcStart).TotalSeconds);
				_event_debug.Set();

				if ((PerfMon.FramesSimulated - 1) % Parameters.QUADTREE_REFRESH_FRAMES == 0) {
					_event_quadtree_release.Reset();
					_event_quadtree.Set();
				}

				if (PerfMon.FramesSimulated % Parameters.SUBFRAME_MULTIPLE == 0) {
					_event_rasterize_release.WaitOne();
					_positions = Program.AllBoids.Select(b => (double[])b.Coordinates.Clone()).ToArray();
					_event_rasterize.Set();

					start = DateTime.Now;
				}
				
				PerfMon.FramesSimulated++;
			}
		}

		private static void RasterizeThread() {
			DateTime start, end;
			while (IsActive) {
				_event_rasterize.WaitOne();
				start = DateTime.Now;

				Tuple<char, double>[] rasterization = Rasterizer.Rasterize(_positions);

				_event_rasterize_release.Set();

				if (Parameters.DENSITY_AUTOSCALE_ENABLE && PerfMon.FramesRasterized % Parameters.AUTOSCALING_REFRESH_FRAMES == 0) {
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
				
				if (Parameters.LEGEND_ENABLE) Rasterizer.DrawLegend(frameData);

				_event_debug.Set();
				end = DateTime.Now;
				PerfMon.RasterizeTime_SMA.Update(end.Subtract(start).TotalSeconds);

				_event_syncDraw_release.WaitOne();
				_mutex_frameBuffer.WaitOne();
				_frameBuffer = frameData;
				_mutex_frameBuffer.ReleaseMutex();
				_event_pushFrame.Set();

				PerfMon.FramesRasterized++;
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
				PerfMon.AutoscaleTime_SMA.Update(end.Subtract(start).TotalSeconds);
			}
		}
		private static void DrawSynchronizationThread() {
			DateTime start, end;
			while (IsActive) {
				_event_pushFrame.WaitOne();
				start = DateTime.Now;
				PerfMon.DelayTime_SMA.Update(start.Subtract(PerfMon.IterationStart).TotalSeconds);

				ConsoleExtensions.CharInfo[] frameData = (ConsoleExtensions.CharInfo[])_frameBuffer.Clone();
				_event_syncDraw_release.Set();

				DrawSynchronize();
				synchronize = DateTime.Now;
				//PerfMon.SynchronizeTime_SMA.Update(synchronize.Subtract(overlay).TotalSeconds);

				PushFrame();
				end = DateTime.Now;

				_targetUpdateTime = _targetUpdateTime.AddMilliseconds(UpdateIntervalMs);
				if (_targetUpdateTime < end) _targetUpdateTime = end;

				//PerfMon.UpdateTime_SMA.Update(end.Subtract(_newDataStarts[FramesRendered % 4]).TotalSeconds);
				//PerfMon.WriteTime_SMA.Update(end.Subtract(synchronize).Add(overlay.Subtract(start)).TotalSeconds);
				//PerfMon.FrameTime_SMA.Update(end.Subtract(synchronize).Add(overlay.Subtract(IterationStart)).TotalSeconds);
				PerfMon.Update();
			}
		}
		
		private static void MonitoringThread() {
			while (IsActive) {
				 _event_debug.WaitOne();
				if (!IsActive || DateTime.Now.Subtract(_lastStatsGraphWrite).TotalMilliseconds >= Parameters.PERF_GRAPH_INTERVAL_MS) {
					_mutex_frameOverlay.WaitOne();
					_overlayBuffer = PerfMon.BuildFpsGraph();
					_mutex_frameOverlay.ReleaseMutex();
					_lastStatsGraphWrite = DateTime.Now;
				}
			}
		}
		

		private static void RenderThread() {
			DateTime nextRefresh = DateTime.Now;
			while (IsActive) {

			}
		}
		private static void DrawSynchronize() {//for the target FPS and minimum duration visible
			DateTime now = DateTime.Now;

			double scheduledWaitMs =
				_targetUpdateTime.Subtract(now).TotalMilliseconds
				- 1000*(PerfMon.RefreshTime_SMA.NumUpdates == 0 ? 0 : PerfMon.RefreshTime_SMA.History.Max());//"rush" it to not be late most the time
			double requiredWaitMs =
				UpdateIntervalMs
				- now.Subtract(PerfMon.IterationEnd).TotalMilliseconds;

			double maxWaitMs = scheduledWaitMs > requiredWaitMs ? scheduledWaitMs : requiredWaitMs;

			if (IsActive && maxWaitMs >= 1) Thread.Sleep((int)maxWaitMs);
		}
		private static void PushFrame() {
			_mutex_frameBuffer.WaitOne();
			DateTime start = DateTime.Now, end;

			int yOffset = 0, xOffset = 0;
			if (Parameters.DEBUG_ENABLE) {
				yOffset = 1; xOffset = 4;

				_mutex_frameOverlay.WaitOne();
				PerfMon.DrawStatsHeader(_overlayBuffer);
				MergeFrames(_frameBuffer, _overlayBuffer, 0, yOffset);
				_mutex_frameOverlay.ReleaseMutex();
			}

			if (DateTime.Now.Subtract(PerfMon.IterationStart).TotalMilliseconds >= Parameters.PERF_WARN_MS) {
				string message = "No update for "
					+ (DateTime.Now.Subtract(PerfMon.IterationStart).TotalSeconds.ToString_Number2(2, true) + "s").PadRight(8);
				for (int i = 0; i < message.Length; i++)
					_frameBuffer[i + xOffset + yOffset*Parameters.WINDOW_WIDTH] = new ConsoleExtensions.CharInfo(message[i], ConsoleColor.Red);
			}
			ConsoleExtensions.WriteConsoleOutput(_frameBuffer);
			end = DateTime.Now;
			PerfMon.RefreshTime_SMA.Update(end.Subtract(start).TotalSeconds);
			_mutex_frameBuffer.ReleaseMutex();
		}
		private static void MergeFrames(ConsoleExtensions.CharInfo[] agg, ConsoleExtensions.CharInfo[] additional, int xOffset = 0, int yOffset = 0) {
			if (!(additional is null)) Array.Copy(additional, 0, agg, xOffset + yOffset*Parameters.WINDOW_WIDTH, additional.Length);
		}
		#endregion Thread runners
		*/
	}
}