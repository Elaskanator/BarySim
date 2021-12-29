using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using System.Linq;
using Generic.Extensions;
using Generic.Models;
using Generic.Models.Trees;
using Generic.Vectors;

namespace ParticleSimulator.Simulation {
	public class BarnesHutTree : QuadTreeSIMD<Particle> {
		private BarnesHutTree(int dim, Vector<float> corner1, Vector<float> corner2, QuadTreeSIMD<Particle> parent)
		: base(dim, corner1, corner2, parent) { }
		public BarnesHutTree(int dim, Vector<float> size) : base(dim, size) { }
		public BarnesHutTree(int dim) : base(dim, Vector<float>.One) { }

		protected override BarnesHutTree NewNode(QuadTreeSIMD<Particle> parent, Vector<float> cornerLeft, Vector<float> cornerRight) =>
			new BarnesHutTree(this.Dim, cornerLeft, cornerRight, parent);

		public override int Capacity => Parameters.QUADTREE_NODE_CAPACITY;

		public bool[] CompleteChildren { get; private set; }

		//public readonly BaryonCenter BaryCenter_Position = new();
		//public readonly BaryonCenter BaryCenter_Mass = new();
		//public readonly BaryonCenter BaryCenter_Charge = new();
		//
		//protected override void Incorporate(T item) {
		//	this.BaryCenter_Position.Update(item.Position, 1d);
		//	this.BaryCenter_Mass.Update(item.Position, item.Mass);
		//	this.BaryCenter_Charge.Update(item.Position, item.Charge);
		//}
		//
		//protected override void AfterRemove(T item) {
		//	this.BaryCenter_Position.Update(item.Position, -1d);
		//	this.BaryCenter_Mass.Update(item.Position, -item.Mass);
		//	this.BaryCenter_Charge.Update(item.Position, -item.Charge);
		//}
		//
		//private List<BarnesHutTree<T>> _siblings = new();
		//private List<BarnesHutTree<T>> _cousins = new();
		//
		//private const float _neighborDist = 0.1f;
		//private bool IsNeighbor(BarnesHutTree<T> other) => this.Center.Distance(other.Center) <= _neighborDist;
		//
		public void Do() {
			ATree<Particle> node = this;
			while (!node.IsLeaf)
				node = node.Children[0];
		}
		//
		//	//Stack<ATree<T>> stack = new();
		//
		//	//int inverse = this.InverseIndex(i);
		//	//ATree<T> node = this;
		//	//while (!node.IsLeaf) {
		//	//	for (int i = 0; i < node.Children.Length; i++) {
		//	//	}
		//	//}
		//}
	}
}