using System;
using System.Collections.Generic;
using System.Numerics;
using Generic.Models;

namespace ParticleSimulator.Simulation {
	public class MagicTree<TElement> : AVectorQuadTree<TElement, MagicTree<TElement>>
	where TElement : ABaryonParticle<TElement> {
		public MagicTree(int dim, Vector<float> corner1, Vector<float> corner2, MagicTree<TElement> parent = null)
		: base(dim, corner1, corner2, parent) { }
		public MagicTree(int dim, IEnumerable<TElement> elements) : base(dim, elements) { }
		protected override MagicTree<TElement> NewInstance(Vector<float> cornerA, Vector<float> cornerB, MagicTree<TElement> parent) =>
			new MagicTree<TElement>(this.Dim, cornerA, cornerB, parent);
		protected override ILeafNode<TElement> NewLeafContainer() => new LeafNode<TElement>(this.NodeCapacity);
		
		protected override void Incorporate(TElement element) {
			this.BaryCenter_Position.Update(element.Position, 1d);
			this.BaryCenter_Mass.Update(element.Position, element.Mass);
			this.BaryCenter_Charge.Update(element.Position, element.Charge);
		}

		public override int NodeCapacity => Parameters.GRAVITY_QUADTREE_NODE_CAPACITY;
		public readonly BaryonCenter BaryCenter_Position = new();
		public readonly BaryonCenter BaryCenter_Mass = new();
		public readonly BaryonCenter BaryCenter_Charge = new();


		public MagicTree<TElement> Refresh() {
			//Queue<TElement> movedElements = new(), retainedElements = new();
			//foreach (MagicTree<TElement> leaf in this.LeafNodesNonEmpty) {
			//	foreach (TElement element in leaf.LeafContainer.Elements)
			//		if (leaf.DoesContain(element))
			//			movedElements.Enqueue(element);
			//		else retainedElements.Enqueue(element);
			//	if (movedElements.Count == leaf.LeafContainer.Count)
			//		;
			//}
			//throw new NotImplementedException();
			return this;
		}
	}
}