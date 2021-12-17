using System;
using System.Collections.Generic;
using System.Numerics;
using Generic.Vectors;

namespace Generic.Models {
	public abstract class AVectorTree<TElement, TSelf> : ATree<TElement, TSelf>
	where TElement : AParticle<TElement>, IEquatable<TElement>, IEqualityComparer<TElement>
	where TSelf : AVectorTree<TElement, TSelf> {//supports any dimensionality
		public AVectorTree(int dim, Vector<float> corner1, Vector<float> corner2, TSelf parent = null) 
		: base(parent) {//caller needs to ensure all values in x1 are smaller than x2 (the corners of a cubic volume)
			this.Dim = dim;
			this.CornerLeft = corner1;
			this.CornerRight = corner2;

			this.Size = corner2 - corner1;
			this.Center = (corner1 + corner2) * (1f / 2f);
			
			Vector<int> zeros = Vector.Equals(VectorFunctions.New(0f, 0.3f, 0f, -2f), Vector<float>.Zero);
			this._resolutionLimitReached = dim > VectorFunctions.VECT_CAPACITY - Vector.Dot(zeros, zeros);
		}
		protected abstract TSelf NewNode(Vector<float> cornerA, Vector<float> cornerB, TSelf parent = null);
		public override string ToString() {
			return string.Format("Node[<{0}> thru <{1}>][{2} members]",
				string.Join(", ", this.CornerLeft),
				string.Join(", ", this.CornerRight),
				this.Count);
		}

		public readonly int Dim;
		private readonly bool _resolutionLimitReached;
		public override bool ResolutionLimitReached => this._resolutionLimitReached;
		
		public readonly Vector<float> CornerLeft;
		public readonly Vector<float> CornerRight;
		public readonly Vector<float> Size;
		public readonly Vector<float> Center;

		public override bool DoesContain(TElement element) {
			for (int d = 0; d < this.Dim; d++)
				if (element.Position[d] < this.CornerLeft[d] || element.Position[d] >= this.CornerRight[d])
					return false;
			return true;
		}

		protected override uint ChooseNodeIdx(TElement element) {
			for (int i = 0; i < this._children.Length; i++)
				if (this._children[i].DoesContain(element))
					return (uint)i;
			throw new Exception("Element is not contained");
		}
	}
}