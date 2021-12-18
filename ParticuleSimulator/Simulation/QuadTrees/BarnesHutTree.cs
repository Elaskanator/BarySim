using System.Collections.Generic;
using System.Numerics;
using Generic.Models;

namespace ParticleSimulator.Simulation {
	public class BarnesHutTree<TElement> : AVectorQuadTree<TElement, BarnesHutTree<TElement>>
	where TElement : BaryonParticle {
		public BarnesHutTree(int dim, Vector<float> corner1, Vector<float> corner2, BarnesHutTree<TElement> parent = null)
		: base(dim, corner1, corner2, parent) { }
		public BarnesHutTree(int dim, IEnumerable<TElement> elements) : base(dim, elements) { }
		protected override BarnesHutTree<TElement> NewInstance(Vector<float> cornerA, Vector<float> cornerB, BarnesHutTree<TElement> parent) =>
			new BarnesHutTree<TElement>(this.Dim, cornerA, cornerB, parent);
		protected override ILeafNode<TElement> NewLeafContainer() => new HashedLeafNode<TElement>(this.NodeCapacity);
		
		protected override void Incorporate(TElement element) {
			this.BaryCenter_Position.Update(element.Position, 1d);
			this.BaryCenter_Mass.Update(element.Position, element.Mass);
			this.BaryCenter_Charge.Update(element.Position, element.Charge);
		}

		public override int NodeCapacity => Parameters.GRAVITY_QUADTREE_NODE_CAPACITY;
		public readonly BaryonCenter BaryCenter_Position = new();
		public readonly BaryonCenter BaryCenter_Mass = new();
		public readonly BaryonCenter BaryCenter_Charge = new();
	}
}