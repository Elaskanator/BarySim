using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Generic.Extensions;
using Generic.Models;

namespace Simulation.Boids {
	public class BoidSimulator : AParticleSimulator<Boid> {
		public const int NUM_BOIDS_PER_FLOCK = 3000;
		public const int NUM_FLOCKS = 1;

		public Flock[] Flocks { get; private set; }
		public override IEnumerable<Boid> AllParticles { get { return Flocks.SelectMany(f => f.Boids); } }
		public int TotalBoids { get { return Flocks.Sum(f => f.Boids.Length); } }

		public BoidSimulator() {
			Flocks = Enumerable.Range(0, NUM_FLOCKS).Select(i => new Flock(NUM_BOIDS_PER_FLOCK, this._rand)).ToArray();
		}

		public override ATree<Boid> BuildTree(IEnumerable<Boid> boids) {
			if (Program.ENABLE_DEBUG_LOGGING) DebugExtensions.DebugWriteline("BuildTree - Start");

			QuadTree<Boid> result = new QuadTree<Boid>((Vector)new double[Parameters.DOMAIN.Length], (Vector)Parameters.DOMAIN);
			result.AddRange(boids);

			if (Program.ENABLE_DEBUG_LOGGING) DebugExtensions.DebugWriteline("BuildTree - End");
			return result;
		}

		public override double[][] Simulate(ATree<Boid> tree) {
			if (Program.ENABLE_DEBUG_LOGGING) DebugExtensions.DebugWriteline("Simulate - Start");

			DateTime startUtc = DateTime.UtcNow;

			Parallel.ForEach(tree.Leaves, leaf => {
				foreach (Boid b in leaf.AllElements)
					b.UpdateDeltas(leaf.GetNeighbors()); });

			if (Parameters.DEBUG_ENABLE) PerfMon.AfterSimulate(startUtc);

			if (Program.ENABLE_DEBUG_LOGGING) DebugExtensions.DebugWriteline("Simulate - End");
			return this.AllParticles.Select(b => (double[])b.Coordinates.Clone()).ToArray();
		}
	}
}