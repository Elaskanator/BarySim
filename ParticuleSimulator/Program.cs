using System;
using Generic.Extensions;
using Generic.Models;
using ParticleSimulator.Rendering;
using ParticleSimulator.Simulation;
using ParticleSimulator.Engine;
using System.Collections.Generic;

namespace ParticleSimulator {
	//TODO world wrap interaction: particles look forward only into adjoining quadrant
	//SEEALSO https://www.youtube.com/watch?v=TrrbshL_0-s
	public class Program {
		public static RenderEngine Engine { get; private set; }
		public static PerfMon Monitor { get; private set; }
		public static BaryonSimulator Simulator { get; private set; }
		public static ConsoleRenderer Renderer { get; private set; }
		public static Rasterizer Rasterizer { get; private set; }
		public static readonly Random Random = new();
		
		public static ISynchronousConsumedResource Resource_ParticleData, Resource_Rasterization, Resource_ScalingData;
		public static ProcessThread StepEval_Simulate, StepEval_Autoscale, StepEval_Rasterize, StepEval_Render, Step_Export, StepEval_Monitor;

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

			Rasterizer = new(Parameters.WINDOW_WIDTH, Parameters.WINDOW_HEIGHT * 2);
			Renderer = new ConsoleRenderer();
			Monitor = new PerfMon(Parameters.AUTOSCALER_ENABLE ? 5 : 4);

			Engine = BuildRunManager();
			Monitor.TitleUpdate();
			//ConsoleExtensions.WaitForEnter("Press enter to start");
			Engine.Start();
		}

		private static RenderEngine BuildRunManager() {
			SynchronousBuffer<Queue<ParticleData>> particleResource = new("Locations", Parameters.PRECALCULATION_LIMIT);
			SynchronousBuffer<Pixel[]> rasterResource = new("Rasterization", Parameters.PRECALCULATION_LIMIT);
			SynchronousBuffer<float?[]> scalingResource = new("ScalingData", 0);

			Resource_ParticleData = particleResource;
			Resource_Rasterization = rasterResource;
			Resource_ScalingData = scalingResource;
			
			StepEval_Render = ProcessThread.New(new() {
				Name = "Draw",
				EvaluatorFn = Renderer.FlushScreenBuffer,
				Synchronizer = Parameters.TARGET_FPS > 0f || Parameters.MAX_FPS > 0f ? TimeSynchronizer.FromFps(Parameters.TARGET_FPS, Parameters.MAX_FPS) : null,
				Callback = Monitor.AfterRender,
				DataLoadingTimeout = TimeSpan.FromMilliseconds(Parameters.PERF_WARN_MS),
				InputResourceUses = new IPrerequisite[] {
					new IPrerequisite<Pixel[]>() {
						Resource = rasterResource,
						DoConsume = true,
			}}});

			StepEval_Simulate = ProcessThread.New(new() {
				Name = "Simulate",
				GeneratorFn = Simulator.RefreshSimulation,
				OutputResource = Resource_ParticleData,
				IsOutputOverwrite = !Parameters.SYNC_SIMULATION,
				OutputSkips = Parameters.SIMULATION_SKIPS,
			});
			StepEval_Rasterize = ProcessThread.New(new() {
				Name = "Rasterize",
				CalculatorFn = Rasterizer.Rasterize,
				OutputResource = Resource_Rasterization,
				InputResourceUses = new IPrerequisite[] {
					new IPrerequisite<Queue<ParticleData>>() {
						Resource = particleResource,
						DoConsume = true,
			}}});

			StepEval_Monitor = ProcessThread.New(new() {
				Name = "Monitor",
				EvaluatorFn = Monitor.TitleUpdate,
				Synchronizer = new TimeSynchronizer(null, TimeSpan.FromMilliseconds(Parameters.CONSOLE_TITLE_INTERVAL_MS))});
			
			if (Parameters.AUTOSCALER_ENABLE)
				StepEval_Autoscale = ProcessThread.New(new() {
					Name = "Autoscale",
					EvaluatorFn = Rasterizer.Scaling.Update,
					Synchronizer = new TimeSynchronizer(null, TimeSpan.FromMilliseconds(Parameters.AUTOSCALE_INTERVAL_MS)),
					InputResourceUses = new IPrerequisite[] {
					new IPrerequisite<float?[]>() {
						Resource = scalingResource,
						OnChange = true,
				}}});

			//if (Parameters.EXPORT_FRAMES) {
			//	Step_Export = ProcessThread.New(new() {
			//		Name = "Export",
			//		EvaluatorFn = Exporter.Eport,
			//		Synchronizer = new TimeSynchronizer(null, TimeSpan.FromMilliseconds(Parameters.AUTOSCALE_INTERVAL_MS)),
			//		InputResourceUses = new IPrerequisite[] {
			//		new IPrerequisite<float?[]>() {
			//			Resource = scalingResource,
			//			OnChange = true,
			//	}}});
			//}

			return new RenderEngine(
				new[] {
					StepEval_Simulate,
					StepEval_Rasterize,
					StepEval_Monitor,
					StepEval_Autoscale,
					StepEval_Render,
					Step_Export,
				});
		}

		public static void CancelAction(object sender, ConsoleCancelEventArgs args) {//ctrl+c and alt+f4 etc
			//keep master thread alive for results output (if enabled)
			//also necessary to cleanup the application, otherwise any threading calls would immediately kill this thread
			if (!(args is null)) args.Cancel = true;

			Engine.Stop();
			Monitor.WriteEnd();
			Engine.Dispose();

			ConsoleExtensions.WaitForEnter("Press enter to exit");
			//Manager.Dispose();
			Environment.Exit(0);
		}
	}
}