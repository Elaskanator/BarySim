using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Generic.Structures;

namespace Boids {
	internal static class Simulator {

		public static void Update(IEnumerable<Boid> boids, QuadTree<Boid> tree) {
			Parallel.ForEach(
				boids,
				b => b.UpdateDeltas(GetNeighbors(tree, b)));
		}

		private static Boid[] GetNeighbors(QuadTree<Boid> tree, Boid b) {
			return tree
				.GetNeighbors_Fast(b, b.Vision)//cube intersection
			//	.Where(b2 => b.Coordinates.Distance(b2.Coordinates) <= b.Vision)//futher filter by true distance (mild performance impact)
			//	.OrderBy(b => Math.Abs(b.Coordinates.Subtract(this.Coordinates).AngleTo(this.Velocity)) * 180d / Math.PI)//prefer boids in direction of travel (mild performance impact)
				.Take(Parameters.DESIRED_NEIGHBORS + 1)
				.ToArray();
		}
	}
}