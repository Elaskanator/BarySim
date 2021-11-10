using Generic.Extensions;
using Generic.Models;

namespace ParticleSimulator.Simulation.Boids {
	public class BoidQuadTree : AQuadTree<Boid, BoidQuadTree>{
		public BoidQuadTree(double[] corner1, double[] corner2, BoidQuadTree parent = null)
		: base(corner1, corner2, parent) { }

		protected override BoidQuadTree NewNode(double[] cornerA, double[] cornerB, BoidQuadTree parent) {
			return new BoidQuadTree(cornerA, cornerB, parent);
		}
		
		protected override BoidQuadTree[] OrganizeNodes(BoidQuadTree[] nodes) {
			Program.Random.ShuffleInPlace(nodes);
			return nodes;
		}
	}
}