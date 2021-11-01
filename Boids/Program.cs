using System;
using System.Collections.Generic;
using System.Linq;
using Generic;

namespace Boids {
	//TODO add handshake optimization (boids sharing interactions, to compute only half as many)
	//SEEALSO https://www.youtube.com/watch?v=TrrbshL_0-s
	public class Program {
		public static Flock[] Flocks { get; private set; }

		public static IEnumerable<Boid> AllBoids { get { return Flocks.SelectMany(f => f.Boids); } }
		public static int TotalBoids { get { return Flocks.Sum(f => f.Boids.Length); } }

		public static void Main(string[] args) {
			Random rand = new Random();

			Flocks = Enumerable.Range(0, Parameters.NUM_FLOCKS).Select(i => new Flock(Parameters.NUM_BOIDS_PER_FLOCK, rand)).ToArray();

			/*
			Console.ForegroundColor = ConsoleColor.White;
			Console.Write("Simulating ");
			Console.ForegroundColor = ChooseColor(Math.Pow((double)RATED_BOIDS / Simulator.TotalBoids, 2));
			Console.Write(Simulator.TotalBoids);
			Console.ForegroundColor = ConsoleColor.White;
			Console.WriteLine(" {0} in {1}-D",
				"boid".Pluralize(Simulator.TotalBoids),
				Parameters.Domain.Length);
			
			ConsoleExtensions.WaitForEnter("Press <Enter> to begin");
			*/

			ExecutionManager.Run();
		}
	}
}