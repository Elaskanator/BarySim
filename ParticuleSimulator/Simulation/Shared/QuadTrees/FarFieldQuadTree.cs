using Generic.Models;

namespace ParticleSimulator.Simulation {
	public class FarFieldQuadTree : CentroidTree<AClassicalParticle> {
		public FarFieldQuadTree(double[] corner1, double[] corner2, QuadTree<AClassicalParticle> parent = null)
		: base(corner1, corner2, parent) {
			this.BaryCenter_Position = new();
			this.BaryCenter_Mass = new();
			this.BaryCenter_Charge = new();
		}
		protected override QuadTree<AClassicalParticle> NewNode(double[] cornerA, double[] cornerB, QuadTree<AClassicalParticle> parent) {
			return new FarFieldQuadTree(cornerA, cornerB, parent);
		}

		public override int Capacity => Parameters.GRAVITY_QUADTREE_NODE_CAPACITY;
		public readonly BaryonCenter BaryCenter_Position;
		public readonly BaryonCenter BaryCenter_Mass;
		public readonly BaryonCenter BaryCenter_Charge;
		
		protected override void Incorporate(AClassicalParticle element) {
			this.BaryCenter_Position.Update(element.LiveCoordinates, 1d);
			this.BaryCenter_Mass.Update(element.LiveCoordinates, element.Mass);
			this.BaryCenter_Charge.Update(element.LiveCoordinates, element.Charge);
		}
	}
}