using System.Collections.Generic;
using System.Linq;
using Generic.Vectors;

namespace Generic.Trees {
	//n-dimensional binary tree
	public abstract class ABinaryTree<TItem, TCorner> : ATree<TItem>
	where TItem : IPosition<TCorner> {
		protected ABinaryTree(int dim, TCorner cornerLeft, TCorner cornerRight, ABinaryTree<TItem, TCorner> parent = null)
		: base (parent) {
			this.Dim = dim;
			this.CornerLeft = cornerLeft;
			this.CornerRight = cornerRight;
			this.Center = this.Midpoint(cornerLeft, cornerRight);

			this._limitReached =
				this.EqualsAny(this.CornerLeft, this.Center)
				|| this.EqualsAny(this.CornerRight, this.Center);
		}

		protected abstract ABinaryTree<TItem, TCorner> NewNode(int directionMask, bool isExpansion);

		public override string ToString() =>
			string.Format("{0}[{1} thru {2}]", base.ToString(), string.Join("", this.CornerLeft), string.Join("", this.CornerRight));
		
		public readonly int Dim;
		public readonly TCorner CornerLeft;
		public readonly TCorner Center;
		public readonly TCorner CornerRight;

		private readonly bool _limitReached;
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
		public abstract int BitmaskGreaterThanOrEqual(TCorner first, TCorner second);

		protected override IEnumerable<ABinaryTree<TItem, TCorner>> FormSubnodes() =>
			Enumerable.Range(0, 1 << this.Dim)
				.Select(i => this.NewNode(i, false));

		protected override ABinaryTree<TItem, TCorner> Expand(TItem item) {
			int quadrantMask = this.ChildIndex(item);
			int inverseQuadrantMask = this.InverseIndex(quadrantMask);

			ABinaryTree<TItem, TCorner> newParent = this.NewNode(quadrantMask, true);
			newParent.ItemCount = this.ItemCount;
			newParent.Children = new ABinaryTree<TItem, TCorner>[1u << this.Dim];
			this.Parent = newParent;

			int i = 0;
			foreach (ABinaryTree<TItem, TCorner> node in newParent.FormSubnodes())
				newParent.Children[i] = inverseQuadrantMask == i++ ? this : node;

			return newParent;
		}
	}
}