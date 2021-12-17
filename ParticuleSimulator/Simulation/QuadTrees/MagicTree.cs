using System;
using System.Collections.Generic;
using System.Numerics;
using Generic.Models;

namespace ParticleSimulator.Simulation {
	public class MagicTree<TElement> : AQuadTree<TElement, MagicTree<TElement>>
	where TElement : ABaryonParticle<TElement> {
		public MagicTree(int dim, Vector<float> corner1, Vector<float> corner2, MagicTree<TElement> parent = null)
		: base(dim, corner1, corner2, parent) {
			this.BaryCenter_Position = new();
			this.BaryCenter_Mass = new();
			this.BaryCenter_Charge = new();
		}
		protected override MagicTree<TElement> NewNode(Vector<float> cornerA, Vector<float> cornerB, MagicTree<TElement> parent) {
			return new MagicTree<TElement>(this.Dim, cornerA, cornerB, parent);
		}
		
		protected override void Incorporate(TElement element) {
			this.BaryCenter_Position.Update(element.Position, 1d);
			this.BaryCenter_Mass.Update(element.Position, element.Mass);
			this.BaryCenter_Charge.Update(element.Position, element.Charge);
		}

		public override int NodeCapacity => Parameters.GRAVITY_QUADTREE_NODE_CAPACITY;
		public readonly BaryonCenter BaryCenter_Position;
		public readonly BaryonCenter BaryCenter_Mass;
		public readonly BaryonCenter BaryCenter_Charge;

		public void Magic() {
			//Queue<MagicTree<TElement>> q = new();

		}
	}
}