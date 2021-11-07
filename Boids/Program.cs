using System;
using System.Collections.Generic;
using System.Linq;
using Generic;

namespace Boids {
	//TODO add handshake optimization (boids sharing interactions, to compute only half as many)
	//TODO world wrap interaction: boids look forward only into adjoining quadrant
	//FOR LATER refactor to support different underlying simulations, and support far-field interactions (e.g. astrophysical simulation)
	//SEEALSO https://www.youtube.com/watch?v=TrrbshL_0-s
	public class Program {
		public const bool ENABLE_DEBUG_LOGGING = false;

		public static RunManager Manager { get; private set; }
		public static Flock[] Flocks { get; private set; }
		public static bool IsActive { get; private set; }

		public static SynchronizedDataBuffer Q_Tree, Q_Locations, Q_Autoscaling, Q_Rasterization, Q_Frame;
		public static AEvaluationStep Step_TreeManager, Step_Simulator, Step_Rasterizer, Step_Autoscaler, Step_Renderer, Step_Drawer;
		public static IEnumerable<Boid> AllBoids { get { return Flocks.SelectMany(f => f.Boids); } }
		public static int TotalBoids { get { return Flocks.Sum(f => f.Boids.Length); } }

		public static void Main(string[] args) {
			Console.CancelKeyPress += new ConsoleCancelEventHandler(CancelAction);
			Console.WindowWidth = Parameters.WINDOW_WIDTH;
			Console.WindowHeight = Parameters.WINDOW_HEIGHT;
			ConsoleExtensions.HideScrollbars();
			//flushing the console buffer gets *really* messed up if the window gets resized by anything
			ConsoleExtensions.DisableAllResizing();
			//ConsoleExtensions.SetWindowPosition(0, 0);
			Console.CursorVisible = false;

			Random rand = new Random();
			Flocks = Enumerable.Range(0, Parameters.NUM_FLOCKS).Select(i => new Flock(Parameters.NUM_BOIDS_PER_FLOCK, rand)).ToArray();

			Manager = BuildRunManager();

			IsActive = true;
			Manager.Start();
		}

		private static RunManager BuildRunManager() {
			Q_Tree = new SynchronizedDataBuffer("Quadtree Constructor", 1);
			Q_Locations = new SynchronizedDataBuffer("Location Mapper", Parameters.PRECALCULATION_LIMIT);
			Q_Autoscaling = new SynchronizedDataBuffer("Autoscaler", 1);
			Q_Rasterization = new SynchronizedDataBuffer("Rasterizer", Parameters.PRECALCULATION_LIMIT);
			Q_Frame = new SynchronizedDataBuffer("Frame Renderer", Parameters.PRECALCULATION_LIMIT);

			Q_Autoscaling.Overwrite(
				Enumerable.Range(1, Parameters.DENSITY_COLORS.Length - 1)
				.Select(x => new SampleSMA(Parameters.AUTOSCALING_SMA_ALPHA, x)).ToArray());

			Step_TreeManager = new EvaluationStep(Q_Tree,
				Simulator.BuildTree)
				{ Name = "Quadtree Builder" };
			Step_Simulator = new EvaluationStep(Q_Locations, !Parameters.SYNC_SIMULATION, Parameters.SUBFRAME_MULTIPLE,
				Simulator.Simulate,
				new Prerequisite(Q_Tree, DataConsumptionType.Consume, Parameters.QUADTREE_REFRESH_FRAMES))
				{ Name = "Simulator" };
			Step_Autoscaler = new EvaluationStep(Q_Autoscaling, true,//TODO limit the execution frequency
				Rasterizer.Autoscale,
				new Prerequisite(Q_Autoscaling, DataConsumptionType.ReadDirty),
				new Prerequisite(Q_Rasterization, DataConsumptionType.OnUpdate))
				{ Name = "Autoscaler" };
			Step_Rasterizer = new EvaluationStep(Q_Rasterization, !Parameters.SYNC_SIMULATION,
				Rasterizer.Rasterize,
				new Prerequisite(Q_Locations, DataConsumptionType.Consume),
				new Prerequisite(Q_Autoscaling, DataConsumptionType.Read))
				{ Name = "Rasterizer" };
			Step_Renderer = new	EvaluationStep(Q_Frame, !Parameters.SYNC_SIMULATION,
				Renderer.Render,
				new Prerequisite(Q_Rasterization, Parameters.SYNC_SIMULATION ? DataConsumptionType.Consume : DataConsumptionType.OnUpdate),
				new Prerequisite(Q_Autoscaling, DataConsumptionType.Read))
				{ Name = "Renderer" };
			Step_Drawer = new NonOutputtingEvaluationStep(
				Renderer.Draw,
				new Prerequisite(Q_Frame,
					Parameters.SYNC_SIMULATION ? DataConsumptionType.Consume : DataConsumptionType.ReadDirty,
					TimeSpan.FromMilliseconds(Parameters.PERF_WARN_MS)),
				new Prerequisite(Q_Autoscaling, DataConsumptionType.ReadDirty))
				{ Name = "Drawer" };

			return new RunManager(Step_TreeManager, Step_Simulator, Parameters.DENSITY_AUTOSCALE_ENABLE ? Step_Autoscaler : null, Step_Rasterizer, Step_Renderer, Step_Drawer);
		}

		private static void CancelAction(object sender, ConsoleCancelEventArgs args) {//ctrl+C
			//args.Cancel = true;//keep master thread alive for results output (if enabled)
			IsActive = false;
			Manager.Dispose();
			Environment.Exit(0);
		}
	}
}