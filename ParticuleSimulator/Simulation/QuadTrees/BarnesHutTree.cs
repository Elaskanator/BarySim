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
		public BarnesHutTree(int dim, Vector<float> size) : base(dim, size) { }
		public BarnesHutTree(int dim) : base(dim, Vector<float>.One) { }
		private BarnesHutTree(int dim, Vector<float> corner1, Vector<float> corner2, QuadTreeSIMD<Particle> parent)
		: base(dim, corner1, corner2, parent) { }

		protected override BarnesHutTree NewNode(QuadTreeSIMD<Particle> parent, Vector<float> cornerLeft, Vector<float> cornerRight) =>
			new BarnesHutTree(this.Dim, cornerLeft, cornerRight, parent);

		public override int Capacity => Parameters.QUADTREE_NODE_CAPACITY;

		public Tuple<Vector<float>, float> Barycenter { get; private set; }
		public void UpdateBarycenter() {
			Tuple<Vector<float>, float> total = new(Vector<float>.Zero, 0f), agg;
			BarnesHutTree child;
			for (int i = 0; i < this.Children.Length; i++) {
				if (this.Children[i].Count > 0) {
					if (this.Children[i].IsLeaf) {
						agg = new(Vector<float>.Zero, 0f);
						foreach (Particle p in this.Children[i].Bin.Where(p => p.Mass > 0f)) {
							agg = new(
								agg.Item1 + (p.Position * p.Mass),
								agg.Item2 + p.Mass);
						}
					} else {
						child = (BarnesHutTree)this.Children[i];
						agg = new(
							child.Barycenter.Item1 * child.Barycenter.Item2,
							child.Barycenter.Item2);
					}
					total = new(
						total.Item1 + (agg.Item1 * agg.Item2),
						total.Item2 + agg.Item2);
				}
			}
			this.Barycenter = new(total.Item1 * (1f / total.Item2), total.Item2);
		}

		public override void Add(Particle item) {
			Stack<BarnesHutTree> path = new();
			BarnesHutTree node = this;
			while (!node.IsLeaf) {
				path.Push(node);
				node = (BarnesHutTree)node.Children[node.GetIndex(item)];
			}

			if (node.Count == 0 || !node.TryMerge(item)) {
				node.Count++;
				if (node.Count <= node.Capacity || node.LimitReached)
					(node.Bin ??= node.NewBin()).Add(item);
				else node.AddLayer(item);

				while (path.TryPop(out node))
					node.Count++;
			}
		}

		public bool TryMerge(Particle p1) {
			foreach (Particle p2 in this.Bin)
				if (p2.TryMerge(p1))
					return true;
			return false;
		}

		//public bool[] CompleteChildren { get; private set; }

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