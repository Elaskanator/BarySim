using System;
using System.Collections.Generic;
using System.Numerics;
using Generic.Vectors;

namespace Generic.Models.Trees {
	public class QuadTreeSIMD<TItem, N> : AQuadTree<TItem, Vector<float>>
	where TItem : IMultiDimensional<Vector<float>> {
		public QuadTreeSIMD(int dim, Vector<float> corner1, Vector<float> corner2, QuadTreeSIMD<TItem, N> parent, int capacity = 1) 
		: base(dim, corner1, corner2, parent, capacity) {//caller needs to ensure all values in x1 are smaller than x2 (the corners of a cubic volume)
			this.Size = corner2 - corner1;

			this._center = this.CornerLeft + (this.Size / (Vector<float>.One + Vector<float>.One));
		}
		public QuadTreeSIMD(int dim, Vector<float> startingLength, IEnumerable<TItem> items, int capacity = 1)
		: base(dim, capacity) {
			this.Size = startingLength;
			
			IEnumerator<TItem> iterator;
			if (!(items is null) && (iterator = items.GetEnumerator()).MoveNext()) {
				this.CornerLeft = iterator.Current.Position;
				this.CornerRight = this.CornerLeft + Vector<float>.One * startingLength;

				this.AddUpOrDown(iterator.Current);
				while (iterator.MoveNext())
					this.AddUpOrDown(iterator.Current);
			} else {
				this.CornerLeft = Vector<float>.Zero;
				this.CornerRight = Vector<float>.One * startingLength;
			}

			this.Size = this.CornerRight - this.CornerLeft;

			this._center = this.CornerLeft + (this.Size / (Vector<float>.One + Vector<float>.One));
		}
		public QuadTreeSIMD(int dim, IEnumerable<TItem> items, int capacity = 1) : this(dim, Vector<float>.One, items, capacity) { }
		public QuadTreeSIMD(int dim, int capacity = 1) : this(dim, Vector<float>.One, null, capacity) { }
		protected virtual QuadTreeSIMD<TItem, N> NewNode(QuadTreeSIMD<TItem, N> parent, Vector<float> cornerLeft, Vector<float> cornerRight) =>
			new QuadTreeSIMD<TItem, N>(this.Dim, cornerLeft, cornerRight, parent, this.Capacity);
		protected override bool DetermineIfLimitReached() =>
			Vector.EqualsAny(this.CornerLeft, this.Center) || Vector.EqualsAny(this.CornerRight, this.Center);

		public Vector<float> Size { get; private set; }

		private readonly Vector<float> _center;
		public override Vector<float> Center => this._center;
		
		private Vector<int> NewLeftBitmask(int directionMask) {
			Span<int> values = stackalloc int[Vector<int>.Count];
			values[0] = (directionMask & 0x01) > 0 ? -1 : 0;
			values[1] = (directionMask & 0x02) > 0 ? -1 : 0;
			values[2] = (directionMask & 0x04) > 0 ? -1 : 0;
			values[3] = (directionMask & 0x08) > 0 ? -1 : 0;
			values[4] = (directionMask & 0x10) > 0 ? -1 : 0;
			values[5] = (directionMask & 0x20) > 0 ? -1 : 0;
			values[6] = (directionMask & 0x40) > 0 ? -1 : 0;
			values[7] = (directionMask & 0x80) > 0 ? -1 : 0;
			return new Vector<int>(values);
			//Span<int> values = stackalloc int[Vector<int>.Count];
			//for (int i = 0; i < Vector<int>.Count; i++)
			//	values[i] = (directionMask & (1 << i)) == 0 ? -1 : 0;
			//return new Vector<int>(values);
		}

		protected override QuadTreeSIMD<TItem, N> NewNode(int directionMask, bool isExpansion) {
			//left side for each dimension d when (directionMask & (1 << d)) == 0
			Vector<int> leftMask = this.NewLeftBitmask(directionMask);
			//Vector<float> sizeFraction = Vector<float>.One / (Vector<float>.One + Vector<float>.One);
			//Vector<float> additionalSize = this.Size * ((Vector<float>.One / sizeFraction) - Vector<float>.One);
			return isExpansion
				? this.NewNode(this,
					Vector.ConditionalSelect(leftMask, this.CornerLeft - this.Size, this.CornerRight),
					Vector.ConditionalSelect(leftMask, this.CornerLeft, this.CornerRight + Size))
				: this.NewNode(this,
					Vector.ConditionalSelect(leftMask, this.CornerLeft, this.Center),
					Vector.ConditionalSelect(leftMask, this.Center, this.CornerRight));
		}
	}
}