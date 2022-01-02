using System.Collections.Generic;
using System.Linq;
using Generic.Vectors;

namespace Generic.Models.Trees {
	public abstract class AQuadTree<TItem, TCorner> : ATree<TItem>
	where TItem : IPosition<TCorner> {
		protected AQuadTree(int dim, TCorner cornerLeft, TCorner cornerRight, AQuadTree<TItem, TCorner> parent = null)
		: base (parent) {
			this.Dim = dim;
			this.CornerLeft = cornerLeft;
			this.CornerRight = cornerRight;
			this.Center = this.Midpoint(cornerLeft, cornerRight);
			this._limitReached =
				this.EqualsAny(this.CornerLeft, this.Center)
				|| this.EqualsAny(this.CornerRight, this.Center);
		}

		protected abstract AQuadTree<TItem, TCorner> NewNode(int directionMask, bool isExpansion);

		public override string ToString() =>
			string.Format("{0}[{1} thru {2}]", base.ToString(), string.Join("", this.CornerLeft), string.Join("", this.CornerRight));
		
		public int Dim { get; private set; }
		public TCorner CornerLeft { get; private set; }
		public TCorner Center { get; private set; }
		public TCorner CornerRight { get; private set; }

		private readonly bool _limitReached;
		public override bool LimitReached => this._limitReached;
		
		public override bool DoesEncompass(TItem item) =>//left-handed convention [a, b)
			this.BitmaskLessThan(item.Position, this.CornerLeft) == 0
			&& this.BitmaskGreaterThanOrEqual(item.Position, this.CornerRight) == 0;

		protected override int GetIndex(TItem item) =>
			this.BitmaskGreaterThanOrEqual(item.Position, this.Center);//left-handed convention [a, b)
		protected int InverseIndex(int idx) =>
			(1 << this.Dim) - idx - 1;
		
		protected abstract TCorner Midpoint(TCorner first, TCorner second);
		protected abstract bool EqualsAny(TCorner first, TCorner second);
		protected abstract int BitmaskLessThan(TCorner first, TCorner second);
		protected abstract int BitmaskGreaterThanOrEqual(TCorner first, TCorner second);

		public AQuadTree<TItem, TCorner> AddUpOrDown(TItem item) {
			AQuadTree<TItem, TCorner> node = this;
			while (!node.DoesEncompass(item))
				if (node.IsRoot)
					node = node.Expand(item);
				else node = (AQuadTree<TItem, TCorner>)node.Parent;

			node.Add(item);
			return node;
		}

		public AQuadTree<TItem, TCorner> AddUpOrDown(IEnumerable<TItem> items) {
			AQuadTree<TItem, TCorner> root = this;
			foreach (TItem item in items)
				root = root.AddUpOrDown(item);
			return root;
		}

		protected override IEnumerable<AQuadTree<TItem, TCorner>> FormSubnodes() =>
			Enumerable.Range(0, 1 << this.Dim)
				.Select(i => this.NewNode(i, false));

		private AQuadTree<TItem, TCorner> Expand(TItem item) {
			int quadrantMask = this.GetIndex(item);
			int inverseQuadrantMask = this.InverseIndex(quadrantMask);

			AQuadTree<TItem, TCorner> newParent = this.NewNode(quadrantMask, true);
			this.Parent = newParent;

			int i = 0;
			newParent.Count = this.Count;
			newParent.Children = new AQuadTree<TItem, TCorner>[1u << this.Dim];
			foreach (AQuadTree<TItem, TCorner> node in newParent.FormSubnodes()) {
				if (i == inverseQuadrantMask)
					newParent.Children[i] = this;
				else newParent.Children[i] = node;
				i++;
			}

			return newParent;
		}
	}
}