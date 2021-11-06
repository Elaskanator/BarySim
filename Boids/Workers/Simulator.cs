using System;
using System.Linq;
using System.Threading.Tasks;
using Generic.Structures;

namespace Boids {
	public static class Simulator {
		public static QuadTree<Boid> BuildTree() {
			if (Program.ENABLE_DEBUG_LOGGING) Generic.DebugExtensions.DebugWriteline("BuildTree - Start");
			QuadTree<Boid> result = new QuadTree<Boid>(Program.AllBoids, Parameters.DOMAIN);
			if (Program.ENABLE_DEBUG_LOGGING) Generic.DebugExtensions.DebugWriteline("BuildTree - End");
			return result;
		}

		public static double[][] Simulate(QuadTree<Boid> tree) {
			DateTime start = DateTime.Now;
			if (Program.ENABLE_DEBUG_LOGGING) Generic.DebugExtensions.DebugWriteline("Simulate - Start");

			if (Parameters.QUADTREE_HYBRID_METHOD) {
				if (Parameters.ENABLE_PARALLELISM) Parallel.ForEach(tree.GetLeaves(), leaf =>
					{ foreach (Boid b in leaf.AllMembers) b.UpdateDeltas(leaf.GetNeighborsAlt(b, Parameters.QUADTREE_INCREASED_ACCURACY)); });
				else foreach (Boid b in Program.AllBoids) b.UpdateDeltas(tree.GetNeighborsAlt(b));
			} else {
				if (Parameters.ENABLE_PARALLELISM) Parallel.ForEach(Program.AllBoids, b =>
					{ b.UpdateDeltas(tree.GetNeighbors(b, b.Vision)); });
				else foreach (QuadTree<Boid> leaf in tree.GetLeaves()) foreach (Boid b in leaf.AllMembers) b.UpdateDeltas(leaf.GetNeighborsAlt(b));
			}

			if (Parameters.DEBUG_ENABLE) PerfMon.AfterSimulate(start);
			if (Program.ENABLE_DEBUG_LOGGING) Generic.DebugExtensions.DebugWriteline("Simulate - End");
			return Program.AllBoids.Select(b => (double[])b.Coordinates.Clone()).ToArray();
		}
	}
}