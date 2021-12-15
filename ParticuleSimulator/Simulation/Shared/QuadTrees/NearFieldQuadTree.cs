using Generic.Models;

namespace ParticleSimulator.Simulation {
	public sealed class NearFieldQuadTree<TParticle> : CentroidTree<TParticle>
	where TParticle : AParticle<TParticle> {
		public NearFieldQuadTree(double[] corner1, double[] corner2, QuadTree<TParticle> parent = null)
		: base(corner1, corner2, parent) { }
		protected override QuadTree<TParticle> NewNode(double[] cornerA, double[] cornerB, QuadTree<TParticle> parent) {
			return new NearFieldQuadTree<TParticle>(cornerA, cornerB, parent); }
		
		public override int Capacity => Parameters.BOIDS_QUADTREE_NODE_CAPACITY;
	}
}