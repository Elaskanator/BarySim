using System.Collections.Generic;
using System.Linq;
using Generic.Vectors;

namespace Generic.Models.Trees {
	public abstract class AQuadTree<TItem, TCorner> : ATree<TItem>
	 where TItem : IMultiDimensional<TCorner> {
		protected AQuadTree(int dim, TCorner cornerLeft, TCorner cornerRight, AQuadTree<TItem, TCorner> parent = null)
		: base (parent) {
			this.Dim = dim;
			this.CornerLeft = cornerLeft;
			this.CornerRight = cornerRight;
		}
		protected AQuadTree(int dim) { this.Dim = dim; }

		protected abstract AQuadTree<TItem, TCorner> NewNode(int directionMask, bool isExpansion);

		private AQuadTree<TItem, TCorner> CreateNewNode(int directionMask, bool isExpansion) {
			AQuadTree<TItem, TCorner> result = this.NewNode(directionMask, isExpansion);
			result._limitReached = !isExpansion && this.DetermineIfLimitReached();
			return result;
		}

		public override string ToString() => string.Format("{0}[{1} thru {2}]", base.ToString(), string.Join("", this.CornerLeft), string.Join("", this.CornerRight));
		
		public int Dim { get; private set; }
		public TCorner CornerLeft { get; protected set; }
		public abstract TCorner Center { get; }
		public TCorner CornerRight { get; protected set; }
		private bool _limitReached = false;
		public sealed override bool LimitReached => this._limitReached;

		protected override int GetChildIndex(TItem item) => item.BitmaskGreaterThanOrEqual(this.Center, this.Dim);//left-handed convention [a, b)
		protected int InverseIndex(int idx) => (1 << this.Dim) - idx - 1;
		protected virtual bool DetermineIfLimitReached() => false;
		
		public override bool DoesEncompass(TItem item) {//left-handed convention [a, b)
			return item.BitmaskLessThan(this.CornerLeft, this.Dim) == 0
				&& item.BitmaskGreaterThanOrEqual(this.CornerRight, this.Dim) == 0;
		}
		
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
				.Select(i => this.CreateNewNode(i, false));

		private AQuadTree<TItem, TCorner> Expand(TItem item) {
			int quadrantMask = this.GetChildIndex(item);
			int inverseQuadrantMask = this.InverseIndex(quadrantMask);

			AQuadTree<TItem, TCorner> newParent = this.CreateNewNode(quadrantMask, true);
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