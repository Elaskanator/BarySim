using System.Collections.Generic;
using System.Linq;
using Generic.Extensions;
using Generic.Vectors;

namespace ParticleSimulator.Simulation {
	public class FarFieldQuadTree : AVectorQuadTree<AClassicalParticle> {
		public FarFieldQuadTree(double[] corner1, double[] corner2, FarFieldQuadTree parent, params PhysicalAttribute[] baryonAttributes)
		: base(corner1, corner2, parent) {
			this.PositionAverage = new();
			this.PositionAverage = new();
			this.BaryCenter = baryonAttributes.ToDictionary(x => x, x => new VectorIncrementalWeightedAverage());
		}

		public override int Capacity => Parameters.GRAVITY_QUADTREE_NODE_CAPACITY;
		public readonly VectorIncrementalWeightedAverage PositionAverage;
		public readonly Dictionary<PhysicalAttribute, VectorIncrementalWeightedAverage> BaryCenter;

		protected override AVectorQuadTree<AClassicalParticle> NewNode(double[] cornerA, double[] cornerB, AVectorQuadTree<AClassicalParticle> parent) {
			return new FarFieldQuadTree(cornerA, cornerB, (FarFieldQuadTree)parent, this.BaryCenter.Keys.ToArray());
		}
		
		protected override double[] MakeCenter() {
			return this.CornerLeft.Zip(this.CornerRight, (a, b) => a + Program.Random.NextDouble() * (b - a)).ToArray();
		}
		protected override AVectorQuadTree<AClassicalParticle> GetContainingChild(AClassicalParticle element) {
			return this._quadrants.Single(q => q.DoesContain(element));
		}

		protected override void ArrangeNodes() {
			Program.Random.ShuffleInPlace(this._quadrants);
		}
		
		protected override void Incorporate(AClassicalParticle element) {
			this.PositionAverage.Update(element.LiveCoordinates);
			foreach (KeyValuePair<PhysicalAttribute, VectorIncrementalWeightedAverage> b in this.BaryCenter)
				b.Value.Update(element.LiveCoordinates, element.GetPhysicalAttribute(b.Key));
		}
	}
}
