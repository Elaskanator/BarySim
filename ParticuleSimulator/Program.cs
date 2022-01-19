using System;
using System.Numerics;
using Generic.Extensions;
using ParticleSimulator.Engine;

namespace ParticleSimulator {
	public class Program {
		public static RenderEngine Engine { get; private set; }

		public static void Main(string[] args) {
			Console.Title = string.Format("Particle Simulator ({0}D) - Initializing", Parameters.DIM);

			if (Vector<float>.Count < Parameters.DIM) {
				Console.ForegroundColor = ConsoleColor.Red;
				Console.WriteLine("Vector dimensionality greater than maximum support of {0}", Vector<float>.Count);
				ConsoleExtensions.WaitForEnter("Press enter to exit");
				Environment.Exit(0);
			}

			if (!Vector.IsHardwareAccelerated) {
				Console.ForegroundColor = ConsoleColor.Red;
				Console.WriteLine("Hardware vector acceleration is disabled");
				Console.ResetColor();
				ConsoleExtensions.WaitForEnter();
			}

			Console.CancelKeyPress += new ConsoleCancelEventHandler(CancelAction);//ctrl+c and alt+f4 etc

			Engine = new RenderEngine();
			//ConsoleExtensions.WaitForEnter("Press enter to start");
			Engine.Start();
		}

		public static void CancelAction(object sender, ConsoleCancelEventArgs args) {//ctrl+c and alt+f4 etc
			//keep master thread alive for results output (if enabled)
			//also necessary to cleanup the application, otherwise any threading calls would immediately kill this thread
			if (!(args is null))
				args.Cancel = true;

			Console.ResetColor();
			Console.CursorLeft = 0;
			Console.CursorTop = Engine.OverlaysEnabled ? 1 + Parameters.GRAPH_HEIGHT : 0;
			Console.WriteLine("{0} simulated in {1}", Engine.Simulator.IterationCount.Pluralize("frame"), DateTime.UtcNow.Subtract(Engine.StartTimeUtc.Value));

			ConsoleExtensions.WaitForEnter("Press enter to exit");

			Engine.Stop();
			Engine.Dispose();
			Environment.Exit(0);
		}
	}
}