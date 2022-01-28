using System.Collections.Generic;
using Generic.Vectors;

namespace Generic.Trees {
	//n-dimensional binary tree
	public abstract class AHyperdimensionalBinaryTree<TItem, TCorner> : ABinaryTree<TItem>
	where TItem : IPosition<TCorner> {
		protected AHyperdimensionalBinaryTree(int dim, TCorner cornerLeft, TCorner cornerRight, AHyperdimensionalBinaryTree<TItem, TCorner> parent = null)
		: base(parent) {
			this.Dim = dim;
			this.CornerLeft = cornerLeft;
			this.CornerRight = cornerRight;
			this.Center = this.Midpoint(cornerLeft, cornerRight);

			this._limitReached =
				this.EqualsAny(cornerLeft, this.Center)
				|| this.EqualsAny(cornerRight, this.Center);
		}
		protected abstract (TCorner, TCorner) NewNodeCorners(int directionMask, bool isExpansion);
		protected abstract AHyperdimensionalBinaryTree<TItem, TCorner> NewNode(TCorner cornerLeft, TCorner cornerRight, AHyperdimensionalBinaryTree<TItem, TCorner> parent);

		public override string ToString() =>
			string.Format("{0}[{1} thru {2}]", base.ToString(), string.Join("", this.CornerLeft), string.Join("", this.CornerRight));
		
		public readonly int Dim;
		public readonly TCorner CornerLeft;
		public readonly TCorner Center;
		public readonly TCorner CornerRight;

		private bool _limitReached = false;
		public override bool MaxDepthReached => this._limitReached;
		
		public override bool DoesEncompass(TItem item) =>//left-handed convention [a, b)
			this.BitmaskLessThan(item.Position, this.CornerLeft) == 0
			&& this.BitmaskGreaterThanOrEqual(item.Position, this.CornerRight) == 0;

		public override int ChildIndex(TItem item) =>
			this.BitmaskGreaterThanOrEqual(item.Position, this.Center);//left-handed convention [a, b)
		public int InverseIndex(int idx) =>
			(1 << this.Dim) - idx - 1;
		
		public abstract TCorner Midpoint(TCorner first, TCorner second);
		public abstract bool EqualsAny(TCorner first, TCorner second);
		public abstract int BitmaskLessThan(TCorner first, TCorner second);
		//public abstract int BitmaskLessThanOrEqual(TCorner first, TCorner second);
		//public abstract int BitmaskGreaterThan(TCorner first, TCorner second);
		public abstract int BitmaskGreaterThanOrEqual(TCorner first, TCorner second);

		protected override IEnumerable<AHyperdimensionalBinaryTree<TItem, TCorner>> FormSubnodes() {
			int max = 1 << this.Dim;
			TCorner left, right;
			for (int i = 0; i < max; i++) {
				(left, right) = this.NewNodeCorners(i, false);
				yield return this.NewNode(left, right, this);
			}
		}

		protected override AHyperdimensionalBinaryTree<TItem, TCorner> Expand(TItem item) {
			int quadrantMask = this.ChildIndex(item);
			int inverseQuadrantMask = this.InverseIndex(quadrantMask);

			TCorner left, right;
			(left, right) = this.NewNodeCorners(quadrantMask, true);
			AHyperdimensionalBinaryTree<TItem, TCorner> newParent = this.NewNode(left, right, null);
			newParent.ItemCount = this.ItemCount;
			newParent.Children = new AHyperdimensionalBinaryTree<TItem, TCorner>[1 << this.Dim];
			this.Parent = newParent;

			int max = 1 << this.Dim;
			AHyperdimensionalBinaryTree<TItem, TCorner> childNode;
			for (int i = 0; i < max; i++) {
				if (i == inverseQuadrantMask) {
					childNode = this;
				} else {
					(left, right) = this.NewNodeCorners(quadrantMask, false);
					childNode = this.NewNode(left, right, newParent);
				}
				newParent.Children[i] = childNode;
			}

			return newParent;
		}
	}
}