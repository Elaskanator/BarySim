using System;
using System.ComponentModel;
using Generic.Extensions;
using Generic.Models;
using ParticleSimulator.Simulation;
using ParticleSimulator.Threading;

namespace ParticleSimulator {
	//TODO quadtree neighborhood search is too effectively localized, so particles cluster way too much never seeing farfield
	//TODO add handshake optimization (particles with symmetric interactions, to compute only half as many)
	//TODO world wrap interaction: particles look forward only into adjoining quadrant
	//SEEALSO https://www.youtube.com/watch?v=TrrbshL_0-s
	public class Program {
		public static readonly Random Random = new();
		public static IParticleSimulator Simulator { get; private set; }
		public static RunManager Manager { get; private set; }

		public static SynchronizedDataBuffer Resource_Tree, Resource_Locations, Resource_Resamplings, Resource_Rasterization;
		public static StepEvaluator StepEval_TreeMaintain, StepEval_Simulate, StepEval_Resample, StepEval_Autoscale, StepEval_Rasterize, StepEval_Draw, StepEval_ConsoleWindow;

		public static void Main(string[] args) {
			Console.CancelKeyPress += new ConsoleCancelEventHandler(CancelAction);//ctrl+c and alt+f4 etc
			
			Console.Title = string.Format("{0} Simulator ({1}D)",
				Parameters.SimType,
				Parameters.DIM);
			//prepare the rendering area (abusing the System.Console window with p-invokes to flush frame buffers)
			Console.WindowWidth = Parameters.WINDOW_WIDTH;
			Console.WindowHeight = Parameters.WINDOW_HEIGHT;
			Console.CursorVisible = false;
			//these require p-invokes
			ConsoleExtensions.HideScrollbars();
			//rendering gets *really* messed up if the window gets resized by anything
			ConsoleExtensions.DisableResizing();//note this doesn't work to disable OS window snapping
			//ConsoleExtensions.SetWindowPosition(0, 0);//TODO

			switch (Parameters.SimType) {
				case SimulationType.Boid:
					Simulator = new Simulation.Boids.BoidSimulator();
					break;
				case SimulationType.Gravity:
					Simulator = new Simulation.Gravity.GravitySimulator();
					break;
				default:
					throw new InvalidEnumArgumentException(nameof(Parameters.SimType), (int)Parameters.SimType, typeof(SimulationType));
			}

			Manager = BuildRunManager();
			Manager.Start();
		}

		private static RunManager BuildRunManager() {
			Resource_Tree = new SynchronizedDataBuffer("Tree", 0);
			Resource_Locations = new SynchronizedDataBuffer("Locations", Parameters.ENABLE_ASYNCHRONOUS ? Parameters.PRECALCULATION_LIMIT : 0);
			Resource_Resamplings = new SynchronizedDataBuffer("Resampling", Parameters.ENABLE_ASYNCHRONOUS ? Parameters.PRECALCULATION_LIMIT : 0);
			Resource_Rasterization = new SynchronizedDataBuffer("Rasterization", Parameters.ENABLE_ASYNCHRONOUS ? Parameters.PRECALCULATION_LIMIT : 0);
			
			StepEval_Draw = new(new() {
				Name = "Drawing",
				//Initializer = null,
				//Calculator = null,
				Evaluator = Renderer.FlushScreenBuffer,
				Synchronizer = Parameters.TARGET_FPS > 0 || Parameters.MAX_FPS > 0 ? TimeSynchronizer.FromFps(Parameters.TARGET_FPS, Parameters.MAX_FPS) : null,
				//Callback = null,
				//DataAssimilationTicksAverager = null,
				//SynchronizationTicksAverager = null,
				ExclusiveTicksAverager = Parameters.PERF_ENABLE ? new SampleSMA(Parameters.PERF_SMA_ALPHA) : null,
				IterationTicksAverager = Parameters.PERF_STATS_ENABLE ? new SampleSMA(Parameters.PERF_SMA_ALPHA) : null,
				DataLoadingTimeout = TimeSpan.FromMilliseconds(Parameters.PERF_WARN_MS),
				//OutputResource = null,
				//IsOutputOverwrite = false,
				//OutputSkips = 0,
				InputResourceUses = new Prerequisite[] {
					new() {
						Resource = Resource_Rasterization,
						DoConsume = true,
						//OnChange = false,
						//DoHold = false,
						//AllowDirtyRead = false,
						//ReuseAmount = 0,
						//ReuseTolerance = 0,
						//ReadTimeout = null
			}}});

			StepEval_Simulate = new(new() {
				Name = "Simulating",
				//Initializer = null,
				Calculator = Simulator.RefreshSimulation,
				//Evaluator = null,
				//Synchronizer = null,
				//Callback = null,
				DataAssimilationTicksAverager = Parameters.PERF_ENABLE ? new SampleSMA(Parameters.PERF_SMA_ALPHA) : null,
				//SynchronizationTicksAverager = null,
				ExclusiveTicksAverager = Parameters.PERF_ENABLE ? new SampleSMA_Tracking(Parameters.PERF_SMA_ALPHA) : null,
				//IterationTicksAverager = null,
				//DataLoadingTimeout = null,
				OutputResource = Resource_Locations,
				IsOutputOverwrite = !Parameters.SYNC_SIMULATION,
				OutputSkips = Parameters.SIMULATION_SKIPS,
				InputResourceUses = new Prerequisite[] {
					new() {
						Resource = Resource_Tree,
						DoConsume = true,
						//OnChange = false,
						DoHold = Parameters.SYNC_TREE_REFRESH,
						//AllowDirtyRead = false,
						ReuseAmount = Parameters.SYNC_TREE_REFRESH ? Parameters.TREE_REFRESH_REUSE_ALLOWANCE : 0,
						ReuseTolerance = Parameters.SYNC_TREE_REFRESH ? 0 : Parameters.TREE_REFRESH_REUSE_ALLOWANCE,
						//ReadTimeout = null
			}}});
			StepEval_TreeMaintain = new(new() {
				Name = "Tree Managing",
				Initializer = Simulator.RebuildTree,
				//Calculator = null,
				//Evaluator = null,
				//Synchronizer = null,
				//Callback = null,
				//DataAssimilationTicksAverager = null,
				//SynchronizationTicksAverager = null,
				ExclusiveTicksAverager = Parameters.PERF_ENABLE ? new SampleSMA(Parameters.PERF_SMA_ALPHA) : null,
				IterationTicksAverager = Parameters.PERF_STATS_ENABLE ? new SampleSMA(Parameters.PERF_SMA_ALPHA) : null,
				//DataLoadingTimeout = null,
				OutputResource = Resource_Tree,
				//IsOutputOverwrite = false,
				//OutputSkips = 0,
				//InputResourceUses = null
			});
			StepEval_Resample = new(new() {
				Name = "Density Sampling",
				//Initializer = null,
				Calculator = Simulator.Resample,
				//Evaluator = null,
				//Synchronizer = null,
				//Callback = null,
				//DataAssimilationTicksAverager = null,
				//SynchronizationTicksAverager = null,
				ExclusiveTicksAverager = Parameters.PERF_ENABLE ? new SampleSMA(Parameters.PERF_SMA_ALPHA) : null,
				IterationTicksAverager = Parameters.PERF_STATS_ENABLE ? new SampleSMA(Parameters.PERF_SMA_ALPHA) : null,
				//DataLoadingTimeout = null,
				OutputResource = Resource_Resamplings,
				//IsOutputOverwrite = false,
				//OutputSkips = 0,
				InputResourceUses = new Prerequisite[] {
					new() {
						Resource = Resource_Locations,
						DoConsume = true,
						//OnChange = false,
						//DoHold = false,
						//AllowDirtyRead = false,
						//ReuseAmount = 0,
						//ReuseTolerance = 0,
						//ReadTimeout = null
			}}});
			StepEval_Rasterize = new(new() {
				Name = "Rasterizer",
				//Initializer = null,
				Calculator = Renderer.Rasterize,
				//Evaluator = null,
				//Synchronizer = null,
				Callback = Parameters.PERF_ENABLE ? PerfMon.AfterRasterize : null,
				//DataAssimilationTicksAverager = null,
				//SynchronizationTicksAverager = null,
				ExclusiveTicksAverager = Parameters.PERF_ENABLE ? new SampleSMA(Parameters.PERF_SMA_ALPHA) : null,
				IterationTicksAverager = Parameters.PERF_ENABLE ? new SampleSMA(Parameters.PERF_SMA_ALPHA) : null,
				//DataLoadingTimeout = null,
				OutputResource = Resource_Rasterization,
				//IsOutputOverwrite = false,
				//OutputSkips = 0,
				InputResourceUses = new Prerequisite[] {
					new() {
						Resource = Resource_Resamplings,
						DoConsume = true,
						//OnChange = false,
						//DoHold = false,
						//AllowDirtyRead = false,
						//ReuseAmount = 0,
						//ReuseTolerance = 0,
						//ReadTimeout = null
			}}});

			StepEval_ConsoleWindow = new(new() {
				Name = "Console Window Monitor",
				//Initializer = null,
				//Calculator = null,
				Evaluator = Renderer.TitleUpdate,
				Synchronizer = new TimeSynchronizer(null, TimeSpan.FromMilliseconds(Parameters.CONSOLE_TITLE_INTERVAL_MS)),
				//Callback = null,
				//DataAssimilationTicksAverager = null,
				//SynchronizationTicksAverager = null,
				ExclusiveTicksAverager = Parameters.PERF_ENABLE ? new SampleSMA(Parameters.PERF_SMA_ALPHA) : null,
				//IterationTicksAverager = null,
				//DataLoadingTimeout = null,
				//OutputResource = null,
				//IsOutputOverwrite = false,
				//OutputSkips = 0,
				InputResourceUses = new Prerequisite[] {
					new() {
						Resource = Resource_Tree,
						//DoConsume = false,
						OnChange = true,
						//DoHold = false,
						//AllowDirtyRead = false,
						//ReuseAmount = 0,
						//ReuseTolerance = 0,
						//ReadTimeout = null
			}}});

			if (Parameters.COLOR_SCHEME == ParticleColoringMethod.Density && Parameters.COLOR_ARRAY.Length > 1)
				StepEval_Autoscale = new(new() {
					Name = "Autoscaler",
					//Initializer = null,
					//Calculator = null,
					Evaluator = Simulator.AutoscaleUpdate,
					Synchronizer = new TimeSynchronizer(null, TimeSpan.FromMilliseconds(Parameters.AUTOSCALE_INTERVAL_MS)),
					//Callback = null,
					//DataAssimilationTicksAverager = null,
					//SynchronizationTicksAverager = null,
					ExclusiveTicksAverager = Parameters.PERF_ENABLE ? new SampleSMA(Parameters.PERF_SMA_ALPHA) : null,
					//IterationTicksAverager = null,
					//DataLoadingTimeout = null,
					//OutputResource = null,
					//IsOutputOverwrite = false,
					//OutputSkips = 0,
					InputResourceUses = new Prerequisite[] {
					new() {
						Resource = Resource_Resamplings,
						//DoConsume = false,
						OnChange = true,
						//DoHold = false,
						//AllowDirtyRead = false,
						//ReuseAmount = 0,
						//ReuseTolerance = 0,
						//ReadTimeout = null
			}}});

			return new RunManager(
				new[] {
					StepEval_Simulate,
					StepEval_TreeMaintain,
					StepEval_Resample,
					StepEval_Rasterize,
					StepEval_ConsoleWindow,
					StepEval_Autoscale,
					StepEval_Draw,
				});
		}

		public static void CancelAction(object sender, ConsoleCancelEventArgs args) {//ctrl+c and alt+f4 etc
			//keep master thread alive for results output (if enabled)
			//also necessary to cleanup the application, otherwise any threading calls would immediately kill this thread
			if (!(args is null)) args.Cancel = true;

			Manager.Stop();
			PerfMon.WriteEnd();

			ConsoleExtensions.WaitForEnter("Press enter to end");
			//Manager.Dispose();
			Environment.Exit(0);
		}
	}
}