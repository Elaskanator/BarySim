using System;
using Generic.Extensions;
using Generic.Models;
using Simulation.Boids;
using Simulation.Threading;

namespace Simulation {
	//TODO add handshake optimization (boids sharing interactions, to compute only half as many)
	//TODO world wrap interaction: boids look forward only into adjoining quadrant
	//FOR LATER refactor to support different underlying simulations, and support far-field interactions (e.g. astrophysical simulation)
	//SEEALSO https://www.youtube.com/watch?v=TrrbshL_0-s
	public class Program {
		public const bool ENABLE_DEBUG_LOGGING = false;

		public static IParticleSimulator Simulator { get; private set; }
		public static RunManager Manager { get; private set; }
		public static bool IsActive { get; private set; }

		public static SynchronizedDataBuffer Q_Tree, Q_Locations, Q_Rasterization, Q_Frame;
		public static AEvaluationStep Step_TreeManager, Step_Simulator, Step_Rasterizer, Step_Autoscaler, Step_Renderer, Step_Drawer;

		public static void Main(string[] args) {
			Console.CancelKeyPress += new ConsoleCancelEventHandler(CancelAction);

			Simulator = new BoidSimulator();
			Manager = BuildRunManager();
			
			Console.WindowWidth = Parameters.WINDOW_WIDTH;
			Console.WindowHeight = Parameters.WINDOW_HEIGHT;
			ConsoleExtensions.HideScrollbars();
			//flushing the console buffer gets *really* messed up if the window gets resized by anything
			ConsoleExtensions.DisableAllResizing();
			//ConsoleExtensions.SetWindowPosition(0, 0);
			Console.CursorVisible = false;

			IsActive = true;
			Manager.Start();
		}

		private static RunManager BuildRunManager() {
			Q_Tree = new SynchronizedDataBuffer("Tree", 0);
			Q_Locations = new SynchronizedDataBuffer("Location Mapper", Parameters.PRECALCULATION_LIMIT);
			Q_Rasterization = new SynchronizedDataBuffer("Rasterizer", Parameters.PRECALCULATION_LIMIT);
			Q_Frame = new SynchronizedDataBuffer("Frame Renderer", Parameters.PRECALCULATION_LIMIT);

			Step_TreeManager = new EvaluationStep(Q_Tree, false, 1,
				p => Simulator.BuildTree(Simulator.AllParticles))
				{ Name = "Tree Builder" };
			Step_Simulator = new EvaluationStep(Q_Locations, !Parameters.SYNC_SIMULATION, Parameters.SUBFRAME_MULTIPLE,
				p => Simulator.Simulate((ITree)p[0]),
				new Prerequisite(Q_Tree, DataConsumptionType.Consume, Parameters.TREE_REFRESH_FRAMES))
				{ Name = "Simulator" };
			Step_Rasterizer = new EvaluationStep(Q_Rasterization, !Parameters.SYNC_SIMULATION, 1,
				Rasterizer.Rasterize,
				new Prerequisite(Q_Locations, DataConsumptionType.Consume))
				{ Name = "Rasterizer" };
			Step_Autoscaler = new NonOutputtingEvaluationStep(
				p => Simulator.AutoscaleUpdate((Tuple<char, double>[])p[0]),
				new TimeSynchronizer(null, TimeSpan.FromMilliseconds(500)),
				new Prerequisite(Q_Rasterization, DataConsumptionType.OnUpdate))
				{ Name = "Autoscaler" };
			Step_Renderer = new	EvaluationStep(Q_Frame, !Parameters.SYNC_SIMULATION, 1,
				Renderer.Render,
				new Prerequisite(Q_Rasterization, Parameters.SYNC_SIMULATION ? DataConsumptionType.Consume : DataConsumptionType.OnUpdate))
				{ Name = "Renderer" };
			Step_Drawer = new NonOutputtingEvaluationStep(
				Renderer.Draw,
				TimeSynchronizer.FromFps(Parameters.TARGET_FPS, Parameters.MAX_FPS),
				new Prerequisite(Q_Frame,
					Parameters.SYNC_SIMULATION ? DataConsumptionType.Consume : DataConsumptionType.ReadDirty,
					TimeSpan.FromMilliseconds(Parameters.PERF_WARN_MS)))
				{ Name = "Drawer" };

			return new RunManager(Step_TreeManager, Step_Simulator, Parameters.DENSITY_AUTOSCALE_ENABLE ? Step_Autoscaler : null, Step_Rasterizer, Step_Renderer, Step_Drawer);
		}

		private static void CancelAction(object sender, ConsoleCancelEventArgs args) {//ctrl+C
			args.Cancel = true;//keep master thread alive for results output (if enabled)
			IsActive = false;
			Manager.Dispose();
			PerfMon.WriteEnd();
			ConsoleExtensions.WaitForEnter("Press enter to end");
			Environment.Exit(0);
		}
	}
}