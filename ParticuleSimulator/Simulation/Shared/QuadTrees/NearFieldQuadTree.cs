using Generic.Models;

namespace ParticleSimulator.Simulation {
	public sealed class NearFieldQuadTree : CentroidTree<AClassicalParticle> {
		public NearFieldQuadTree(double[] corner1, double[] corner2, QuadTree<AClassicalParticle> parent = null)
		: base(corner1, corner2, parent) { }
		protected override QuadTree<AClassicalParticle> NewNode(double[] cornerA, double[] cornerB, QuadTree<AClassicalParticle> parent) {
			return new NearFieldQuadTree(cornerA, cornerB, parent); }
		
		public override int Capacity => Parameters.BOIDS_QUADTREE_NODE_CAPACITY;
		protected override double[] ChooseCenter() {
			return this.ChooseRandomCenter(); }
	}
}