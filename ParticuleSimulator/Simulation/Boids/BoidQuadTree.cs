namespace ParticleSimulator.Simulation.Boids {
	public class BoidQuadTree : ASimulationQuadTree<Boid, BoidQuadTree>{
		public BoidQuadTree(double[] corner1, double[] corner2, BoidQuadTree parent = null)
		: base(corner1, corner2, parent) { }

		protected override BoidQuadTree NewNode(double[] cornerA, double[] cornerB, BoidQuadTree parent) {
			return new BoidQuadTree(cornerA, cornerB, parent);
		}
	}
}