using System;
using System.Numerics;
using Generic.Vectors;

namespace Generic.Models.Trees {
	public class QuadTreeSIMD<TItem, N> : AQuadTree<TItem, Vector<float>>
	where TItem : IMultiDimensional<Vector<float>> {
		public QuadTreeSIMD(int dim, Vector<float> corner1, Vector<float> corner2, QuadTreeSIMD<TItem, N> parent = null) 
		: base(dim, corner1, corner2, parent) {//caller needs to ensure all values in x1 are smaller than x2 (the corners of a cubic volume)
			this.Size = corner2 - corner1;
			this._center = this.CornerLeft + (this.Size * (1f / 2f));
		}
		public QuadTreeSIMD(int dim, Vector<float> startingLength)
		: base(dim) {
			startingLength = Vector.ConditionalSelect(VectorFunctions.DimensionSignals[dim], startingLength, Vector<float>.Zero);

			this.CornerLeft = Vector<float>.Zero;
			this.CornerRight = Vector<float>.One * startingLength;
			this.Size = startingLength;
			this._center = startingLength * (1f / 2f);
		}
		public QuadTreeSIMD(int dim) : this(dim, Vector<float>.One) { }
		protected virtual QuadTreeSIMD<TItem, N> NewNode(QuadTreeSIMD<TItem, N> parent, Vector<float> cornerLeft, Vector<float> cornerRight) =>
			new QuadTreeSIMD<TItem, N>(this.Dim, cornerLeft, cornerRight, parent);
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
		}

		protected override QuadTreeSIMD<TItem, N> NewNode(int directionMask, bool isExpansion) {
			Vector<int> leftMask = this.NewLeftBitmask(directionMask);
			//Vector<float> sizeFraction = Vector<float>.One / (Vector<float>.One + Vector<float>.One);
			//Vector<float> additionalSize = this.Size * ((Vector<float>.One / sizeFraction) - Vector<float>.One);
			return isExpansion
				? this.NewNode(null,
					Vector.ConditionalSelect(leftMask, this.CornerLeft - this.Size, this.CornerLeft),
					Vector.ConditionalSelect(leftMask, this.CornerRight, this.CornerRight + this.Size))
				: this.NewNode(this,
					Vector.ConditionalSelect(leftMask, this.CornerLeft, this.Center),
					Vector.ConditionalSelect(leftMask, this.Center, this.CornerRight));
		}
	}
}