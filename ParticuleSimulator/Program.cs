using System;
using Generic.Extensions;
using Generic.Models;
using ParticleSimulator.ConsoleRendering;
using ParticleSimulator.Simulation;
using ParticleSimulator.Engine;

namespace ParticleSimulator {
	//TODO quadtree neighborhood search is too effectively localized, so particles cluster way too much never seeing farfield
	//TODO add handshake optimization (particles with symmetric interactions, to compute only half as many)
	//TODO world wrap interaction: particles look forward only into adjoining quadrant
	//SEEALSO https://www.youtube.com/watch?v=TrrbshL_0-s
	public class Program {
		public static PerfMon Monitor { get; private set; }
		public static RunManager Manager { get; private set; }
		public static BaryonSimulator Simulator { get; private set; }
		public static readonly Random Random = new();
		
		public static SynchronizedDataBuffer Resource_ParticleData, Resource_Rasterization, Resource_ScalingData;
		public static ProcessThread StepEval_Simulate, StepEval_Autoscale, StepEval_Rasterize, StepEval_Render, StepEval_Monitor;

		public static void Main(string[] args) {
			Console.Title = string.Format("Barnes-Hut Simulator ({0}D) - Initializing", Parameters.DIM);

			if (Generic.Vectors.VectorFunctions.VECT_CAPACITY < Parameters.DIM) {
				Console.ForegroundColor = ConsoleColor.Red;
				Console.WriteLine("Vector dimensionality greater than maximum support of {0}", Generic.Vectors.VectorFunctions.VECT_CAPACITY);
				ConsoleExtensions.WaitForEnter("Press enter to exit");
				Environment.Exit(0);
			}

			if (!System.Numerics.Vector.IsHardwareAccelerated) {
				Console.ForegroundColor = ConsoleColor.Red;
				Console.WriteLine("Hardware vector acceleration is disabled");
				Console.ResetColor();
				ConsoleExtensions.WaitForEnter();
			}

			Console.CancelKeyPress += new ConsoleCancelEventHandler(CancelAction);//ctrl+c and alt+f4 etc

			//prepare the rendering area (abusing the System.Console window with p-invokes to flush frame buffers)
			Console.WindowWidth = Parameters.WINDOW_WIDTH;
			Console.WindowHeight = Parameters.WINDOW_HEIGHT;
			Console.CursorVisible = false;
			//these require p-invokes
			ConsoleExtensions.HideScrollbars();
			//rendering gets *really* messed up if the window gets resized by anything
			ConsoleExtensions.DisableResizing();//note this doesn't work to disable OS window snapping
			//ConsoleExtensions.SetWindowPosition(0, 0);//TODO

			Simulator = new BaryonSimulator();
			Monitor = new PerfMon();
			Monitor.TitleUpdate();

			Manager = BuildRunManager();
			//ConsoleExtensions.WaitForEnter("Press enter to start");
			Manager.Start();
		}

		private static RunManager BuildRunManager() {
			Resource_ParticleData = new SynchronizedDataBuffer("Locations", Parameters.PRECALCULATION_LIMIT);
			Resource_Rasterization = new SynchronizedDataBuffer("Rasterization", Parameters.PRECALCULATION_LIMIT);
			Resource_ScalingData = new SynchronizedDataBuffer("ScalingData", 0);
			
			StepEval_Render = ProcessThread.New(new() {
				Name = "Draw",
				//Initializer = null,
				//Calculator = null,
				Evaluator = Renderer.FlushScreenBuffer,
				Synchronizer = Parameters.TARGET_FPS > 0f || Parameters.MAX_FPS > 0f ? TimeSynchronizer.FromFps(Parameters.TARGET_FPS, Parameters.MAX_FPS) : null,
				Callback = Monitor.AfterRender,
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

			StepEval_Simulate = ProcessThread.New(new() {
				Name = "Simulate",
				//Initializer = null,
				Calculator = Simulator.RefreshSimulation,
				//Evaluator = null,
				//Synchronizer = null,
				//Callback = null,
				//DataLoadingTimeout = null,
				OutputResource = Resource_ParticleData,
				IsOutputOverwrite = !Parameters.SYNC_SIMULATION,
				OutputSkips = Parameters.SIMULATION_SKIPS,
				//InputResourceUses = null
			});
			StepEval_Rasterize = ProcessThread.New(new() {
				Name = "Rasterizer",
				//Initializer = null,
				Calculator = Renderer.Rasterize,
				//Evaluator = null,
				//Synchronizer = null,
				//Callback = null,
				//DataLoadingTimeout = null,
				OutputResource = Resource_Rasterization,
				//IsOutputOverwrite = false,
				//OutputSkips = 0,
				InputResourceUses = new Prerequisite[] {
					new() {
						Resource = Resource_ParticleData,
						DoConsume = true,
						//OnChange = false,
						//DoHold = false,
						//AllowDirtyRead = false,
						//ReuseAmount = 0,
						//ReuseTolerance = 0,
						//ReadTimeout = null
			}}});

			StepEval_Monitor = ProcessThread.New(new() {
				Name = "Monitor",
				//Initializer = null,
				//Calculator = null,
				Evaluator = Monitor.TitleUpdate,
				Synchronizer = new TimeSynchronizer(null, TimeSpan.FromMilliseconds(Parameters.CONSOLE_TITLE_INTERVAL_MS)),
				//Callback = null,
				//DataLoadingTimeout = null,
				//OutputResource = null,
				//IsOutputOverwrite = false,
				//OutputSkips = 0,
				InputResourceUses = new Prerequisite[] {
					new() {
						Resource = Resource_ParticleData,
						//DoConsume = false,
						OnChange = true,
						//DoHold = false,
						//AllowDirtyRead = false,
						//ReuseAmount = 0,
						//ReuseTolerance = 0,
						//ReadTimeout = null
			}}});

			if (!Parameters.COLOR_USE_FIXED_BANDS
			&& Parameters.COLOR_ARRAY.Length > 1
			&& Parameters.COLOR_METHOD != ParticleColoringMethod.Depth)
				StepEval_Autoscale = ProcessThread.New(new() {
					Name = "Autoscale",
					//Initializer = null,
					//Calculator = null,
					Evaluator = Renderer.Scaling.Update,
					Synchronizer = new TimeSynchronizer(null, TimeSpan.FromMilliseconds(Parameters.AUTOSCALE_INTERVAL_MS)),
					//Callback = null,
					//DataLoadingTimeout = null,
					//OutputResource = null,
					//IsOutputOverwrite = false,
					//OutputSkips = 0,
					InputResourceUses = new Prerequisite[] {
					new() {
						Resource = Resource_ScalingData,
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
					StepEval_Rasterize,
					StepEval_Monitor,
					StepEval_Autoscale,
					StepEval_Render,
				});
		}

		public static void CancelAction(object sender, ConsoleCancelEventArgs args) {//ctrl+c and alt+f4 etc
			//keep master thread alive for results output (if enabled)
			//also necessary to cleanup the application, otherwise any threading calls would immediately kill this thread
			if (!(args is null)) args.Cancel = true;

			Manager.Stop();
			Monitor.WriteEnd();

			ConsoleExtensions.WaitForEnter("Press enter to exit");
			//Manager.Dispose();
			Environment.Exit(0);
		}
	}
}