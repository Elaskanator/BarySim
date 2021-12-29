using System;
using System.Numerics;
using Generic.Vectors;

namespace Generic.Models.Trees {
	public class QuadTreeSIMD<TItem> : AQuadTree<TItem, Vector<float>>
	where TItem : IPosition<Vector<float>> {
		protected QuadTreeSIMD(int dim, Vector<float> corner1, Vector<float> corner2, QuadTreeSIMD<TItem> parent = null) 
		: base(dim, corner1, corner2, parent) {//caller needs to ensure all values in x1 are smaller than x2 (the corners of a cubic volume)
			this.Size = corner2 - corner1;
			this._center = this.CornerLeft + (this.Size * 0.5f);
		}
		public QuadTreeSIMD(int dim, Vector<float> startingLength)
		: base(dim) {
			startingLength = Vector.ConditionalSelect(VectorFunctions.DimensionSignals[dim], startingLength, Vector<float>.Zero);
			this.CornerLeft = Vector<float>.Zero;
			this.CornerRight = startingLength;
			this.Size = startingLength;
			this._center = startingLength * 0.5f;
		}
		public QuadTreeSIMD(int dim) : this(dim, Vector<float>.One) { }
		protected virtual QuadTreeSIMD<TItem> NewNode(QuadTreeSIMD<TItem> parent, Vector<float> cornerLeft, Vector<float> cornerRight) =>
			new QuadTreeSIMD<TItem>(this.Dim, cornerLeft, cornerRight, parent);

		protected override bool DetermineIfLimitReached() =>
			this.CornerLeft.EqualsAny(this.Center, this.Dim) || this.CornerRight.EqualsAny(this.Center, this.Dim);

		protected override int BitmaskLessThan(Vector<float> first, Vector<float> second) {
			return first.BitmaskLessThan(second, this.Dim);
		}
		protected override int BitmaskGreaterThanOrEqual(Vector<float> first, Vector<float> second) {
			return first.BitmaskGreaterThanOrEqual(second, this.Dim);
		}

		public Vector<float> Size { get; private set; }

		private readonly Vector<float> _center;
		public override Vector<float> Center => this._center;
		
		private Vector<int> NewLeftBitmask(int directionMask) {
			Span<int> values = stackalloc int[Vector<int>.Count];
			values[0] = (directionMask & 0x01) == 0 ? -1 : 0;
			values[1] = (directionMask & 0x02) == 0 ? -1 : 0;
			values[2] = (directionMask & 0x04) == 0 ? -1 : 0;
			values[3] = (directionMask & 0x08) == 0 ? -1 : 0;
			values[4] = (directionMask & 0x10) == 0 ? -1 : 0;
			values[5] = (directionMask & 0x20) == 0 ? -1 : 0;
			values[6] = (directionMask & 0x40) == 0 ? -1 : 0;
			values[7] = (directionMask & 0x80) == 0 ? -1 : 0;
			return new Vector<int>(values);
		}

		protected override QuadTreeSIMD<TItem> NewNode(int directionMask, bool isExpansion) {
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