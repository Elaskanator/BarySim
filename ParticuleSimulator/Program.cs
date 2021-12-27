using System;
using Generic.Extensions;
using ParticleSimulator.Engine;

namespace ParticleSimulator {
	public class Program {
		public static RenderEngine Engine { get; private set; }

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

			Engine = new RenderEngine();
			//ConsoleExtensions.WaitForEnter("Press enter to start");
			Engine.Start();
		}

		public static void CancelAction(object sender, ConsoleCancelEventArgs args) {//ctrl+c and alt+f4 etc
			//keep master thread alive for results output (if enabled)
			//also necessary to cleanup the application, otherwise any threading calls would immediately kill this thread
			if (!(args is null))
				args.Cancel = true;

			Engine.Stop();
			Engine.Dispose();

			ConsoleExtensions.WaitForEnter("Press enter to exit");
			//Manager.Dispose();
			Environment.Exit(0);
		}
	}
}