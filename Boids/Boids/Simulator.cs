using System;
using System.Linq;
using System.Threading.Tasks;
using Generic.Models;

namespace Simulation.Boids {
	public static class Simulator {
		public static ATree<Boid> BuildTree(object[] p) {
			if (Program.ENABLE_DEBUG_LOGGING) Generic.DebugExtensions.DebugWriteline("BuildTree - Start");

			QuadTree<Boid> result = new QuadTree<Boid>((SimpleVector)new double[Parameters.DOMAIN.Length], (SimpleVector)Parameters.DOMAIN);
			result.AddRange(Program.AllBoids);

			if (Program.ENABLE_DEBUG_LOGGING) Generic.DebugExtensions.DebugWriteline("BuildTree - End");
			return result;
		}

		public static double[][] Simulate(object[] p) {
			if (Program.ENABLE_DEBUG_LOGGING) Generic.DebugExtensions.DebugWriteline("Simulate - Start");

			QuadTree<Boid> tree = (QuadTree<Boid>)p[0];
			DateTime startUtc = DateTime.UtcNow;

			Parallel.ForEach(tree.Leaves, leaf => {
				foreach (Boid b in leaf.AllElements)
					b.UpdateDeltas(leaf.GetNeighbors()); });


			if (Parameters.DEBUG_ENABLE) PerfMon.AfterSimulate(startUtc);

			if (Program.ENABLE_DEBUG_LOGGING) Generic.DebugExtensions.DebugWriteline("Simulate - End");
			return Program.AllBoids.Select(b => (double[])b.Coordinates.Clone()).ToArray();
		}
	}
}