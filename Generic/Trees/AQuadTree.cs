using System.Collections.Generic;
using System.Linq;
using Generic.Extensions;
using Generic.Vectors;

namespace Generic.Models.Trees {
	public abstract class AQuadTree<TItem, TCorner> : ATree<TItem>
	 where TItem : IMultiDimensional<TCorner> {
		protected AQuadTree(int dim, TCorner cornerLeft, TCorner cornerRight, AQuadTree<TItem, TCorner> parent, int capacity = 1)
		: base (parent, capacity) {
			this.Dim = dim;
			this.CornerLeft = cornerLeft;
			this.CornerRight = cornerRight;
		}
		protected AQuadTree(int dim, int capacity = 1)
		: base (capacity) {
			this.Dim = dim;
		}
		protected abstract AQuadTree<TItem, TCorner> NewNode(int directionMask, bool isExpansion);
		private AQuadTree<TItem, TCorner> CreateNewNode(int directionMask, bool isExpansion) {
			AQuadTree<TItem, TCorner> result = this.NewNode(directionMask, isExpansion);
			result._limitReached = this.DetermineIfLimitReached();
			return result;
		}
		protected virtual bool DetermineIfLimitReached() => false;
		public override string ToString() => string.Format("Node[{0} items {1} thru {2}]", this.Count.Pluralize("item"), string.Join("", this.CornerLeft), string.Join("", this.CornerRight));
		
		public int Dim { get; private set; }
		public TCorner CornerLeft { get; protected set; }
		public abstract TCorner Center { get; }
		public TCorner CornerRight { get; protected set; }
		private bool _limitReached = false;
		public sealed override bool LimitReached => this._limitReached;

		protected override int GetChildIndex(TItem item) => item.BitmaskLessThan(this.Center, this.Dim);//left-handed convention [a, b)
		private int ComplementQuadrantMask(int comparisonMask) =>//complement of only the least significant bits
			~(comparisonMask << (32 - this.Dim)) >> (32 - this.Dim);
		
		public override bool DoesEncompass(TItem item) {//left-handed convention [a, b)
			return item.BitmaskLessThan(this.CornerLeft, this.Dim) == 0
				&& item.BitmaskGreaterThanOrEqual(this.CornerRight, this.Dim) == 0;
		}
		
		public void AddUpOrDown(TItem item) {
			AQuadTree<TItem, TCorner> node = this, newNode;
			while (!node.DoesEncompass(item))
				if (node.IsRoot) {
					newNode = node.Expand(item);
					node.Parent = newNode;
					node = newNode;
				} else node = (AQuadTree<TItem, TCorner>)node.Parent;

			node.Add(item);
		}

		protected override IEnumerable<AQuadTree<TItem, TCorner>> FormSubnodes() =>
			Enumerable.Range(0, 1 << this.Dim)
				.Select(i => this.CreateNewNode(i, false));

		private AQuadTree<TItem, TCorner> Expand(TItem item) {
			int quadrantMask = this.GetChildIndex(item);
			int inverseQuadrantMask = this.ComplementQuadrantMask(quadrantMask);

			AQuadTree<TItem, TCorner> newParent = this.CreateNewNode(quadrantMask, true);

			int i = 0;
			newParent.Children = new AQuadTree<TItem, TCorner>[1 << this.Dim];
			foreach (AQuadTree<TItem, TCorner> node in this.FormSubnodes()) {
				if (i == inverseQuadrantMask)
					newParent.Children[i] = this;
				else newParent.Children[i] = node;
				i++;
			}

			return newParent;
		}
	}
}