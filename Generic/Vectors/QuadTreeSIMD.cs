using System;
using System.Numerics;
using Generic.Trees;

namespace Generic.Vectors {
	public class QuadTreeSIMD<TItem> : ABinaryTree<TItem, Vector<float>>
	where TItem : IPosition<Vector<float>> {
		protected QuadTreeSIMD(int dim, Vector<float> corner1, Vector<float> corner2, QuadTreeSIMD<TItem> parent = null) 
		: base(dim, corner1, corner2, parent) {//caller needs to ensure all values in x1 are smaller than x2 (the corners of a cubic volume)
			this.Size = this.CornerRight - this.CornerLeft;
			this.SizeSquared = this.Size[0] * this.Size[0];
		}
		public QuadTreeSIMD(int dim, Vector<float> startingLength)
		: base(dim, Vector<float>.Zero, Vector.ConditionalSelect(VectorFunctions.DimensionSignals[dim], startingLength, Vector<float>.Zero)) {
			this.Size = this.CornerRight - this.CornerLeft;
			this.SizeSquared = this.Size[0] * this.Size[0];
		}
		public QuadTreeSIMD(int dim) : this(dim, Vector<float>.One) { }
		protected virtual QuadTreeSIMD<TItem> NewNode(QuadTreeSIMD<TItem> parent, Vector<float> cornerLeft, Vector<float> cornerRight) =>
			new QuadTreeSIMD<TItem>(this.Dim, cornerLeft, cornerRight, parent);

		protected override Vector<float> Midpoint(Vector<float> first, Vector<float> second) =>
			(first + second) * 0.5f;
		protected override bool EqualsAny(Vector<float> first, Vector<float> second) =>
			first.EqualsAny(second, this.Dim);
		protected override int BitmaskLessThan(Vector<float> first, Vector<float> second) =>
			first.BitmaskLessThan(second, this.Dim);
		protected override int BitmaskGreaterThanOrEqual(Vector<float> first, Vector<float> second) =>
			first.BitmaskGreaterThanOrEqual(second, this.Dim);

		public readonly Vector<float> Size;
		public readonly float SizeSquared;
		
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