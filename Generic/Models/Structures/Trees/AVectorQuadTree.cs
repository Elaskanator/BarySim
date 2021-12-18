using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Generic.Vectors;

namespace Generic.Models {
	public abstract class AVectorQuadTree<TElement, TSelf> : ATree<TElement, TSelf>, IMutableTree<TElement, TSelf>
	where TElement : AParticle<TElement>
	where TSelf : AVectorQuadTree<TElement, TSelf> {
		public AVectorQuadTree(int dim, Vector<float> corner1, Vector<float> corner2, TSelf parent = null) 
		: base(parent) {//caller needs to ensure all values in x1 are smaller than x2 (the corners of a cubic volume)
			this.Dim = dim;
			this.CornerLeft = corner1;
			this.CornerRight = corner2;

			this.Size = corner2 - corner1;
			this.Center = (corner1 + corner2) * (1f / 2f);
			
			Vector<int> zeros = Vector.Equals(VectorFunctions.New(0f, 0.3f, 0f, -2f), Vector<float>.Zero);
			this._resolutionLimitReached = dim > VectorFunctions.VECT_CAPACITY - Vector.Dot(zeros, zeros);
		}
		public AVectorQuadTree(int dim, bool makeSquare, IEnumerable<TElement> elements) {
			float[]
				leftCorner = Enumerable.Repeat(float.PositiveInfinity, dim).ToArray(),
				rightCorner = Enumerable.Repeat(float.NegativeInfinity, dim).ToArray();
			foreach (TElement element in elements)
				for (int d = 0; d < dim; d++) {
					leftCorner[d] = leftCorner[d] < element.Position[d] ? leftCorner[d] : element.Position[d];
					rightCorner[d] = rightCorner[d] > element.Position[d] ? rightCorner[d] : element.Position[d];
				}
			if (makeSquare) {
				float maxSize = leftCorner.Zip(rightCorner, (l, r) => r - l).Max();
				rightCorner = leftCorner.Select(l => l + maxSize).ToArray();
			}

			this.Dim = dim;
			this.CornerLeft = VectorFunctions.New(leftCorner);
			this.CornerRight = VectorFunctions.New(rightCorner.Select(c => c * (1f + 1E-5f)));

			this.Size = this.CornerRight - this.CornerLeft;
			this.Center = (this.CornerLeft + this.CornerRight) * (1f / 2f);
			
			Vector<int> zeros = Vector.Equals(VectorFunctions.New(0f, 0.3f, 0f, -2f), Vector<float>.Zero);
			this._resolutionLimitReached = dim > VectorFunctions.VECT_CAPACITY - Vector.Dot(zeros, zeros);

			this.AddUpOrDown(elements);
		}
		public AVectorQuadTree(int dim, IEnumerable<TElement> elements) : this(dim, true, elements) { }
		protected abstract TSelf NewInstance(Vector<float> leftCorner, Vector<float> rightCorner, TSelf parent = null);
		public override string ToString() {
			return string.Format("Node[<{0}> thru <{1}>][{2} members]",
				string.Join(", ", this.CornerLeft),
				string.Join(", ", this.CornerRight),
				this.Count);
		}

		public readonly int Dim;
		private readonly bool _resolutionLimitReached;
		public override bool ResolutionLimitReached => this._resolutionLimitReached;
		
		public Vector<float> CornerLeft { get; private set; }
		public Vector<float> CornerRight { get; private set; }
		public Vector<float> Size { get; private set; }
		public Vector<float> Center { get; private set; }

		public override bool DoesContain(TElement element) {
			//Vector<int> a = Vector.LessThan(element.Position, this.Center);
			//Vector<int> b = Vector.GreaterThanOrEqual(element.Position, this.Center);

			for (int d = 0; d < this.Dim; d++)//left-handed range [a, b)
				if (element.Position[d] < this.CornerLeft[d] || element.Position[d] >= this.CornerRight[d])
					return false;
			return true;
		}

		public TSelf AddUpOrDown(TElement element) {
			TSelf node = (TSelf)this, parent;
			TSelf[] newNodes;
			uint directionMask, antidirectionMask;
			Vector<float> sizeFraction;
			float[] additionalSize;
			while (!node.DoesContain(element)) {
				sizeFraction = this.ChooseSizeFraction();
				additionalSize = new float[this.Dim];
				for (int d = 0; d < this.Dim; d++)
					additionalSize[d] = this.Size[d] *  (1f / sizeFraction[d] - 1f);

				directionMask = this.ChooseNodeIdx(element);//the direction to build into
				antidirectionMask = this.InvertQuadrantIdx(directionMask);//where the current node is

				node.Depth++;
				parent = this.NewInstance(
					VectorFunctions.New(
						Enumerable.Range(0, this.Dim)
							.Select(d => (directionMask & (1u << d)) > 0
								? node.CornerRight[d]
								: node.CornerLeft[d] - additionalSize[d])),
					VectorFunctions.New(
						Enumerable.Range(0, this.Dim)
							.Select(d => (directionMask & (1u << d)) > 0
								? node.CornerRight[d] + additionalSize[d]
								: node.CornerLeft[d])));

				newNodes = new TSelf[1u << this.Dim];
				int i = 0;
				foreach (Tuple<Vector<float>, Vector<float>> nodeCorners in parent.FormNewNodeCorners(sizeFraction)) {
					newNodes[i] = i == antidirectionMask
						? node//the current layer
						: this.NewInstance(nodeCorners.Item1, nodeCorners.Item2, parent);
					i++;
				}
				parent._children = newNodes;
				node = newNodes[directionMask];
			}

			node.Add(element);
			return node;
		}
		public void AddUpOrDown(IEnumerable<TElement> elements) {
			foreach (TElement element in elements)
				this.AddUpOrDown(element);
		}

		protected override uint ChooseNodeIdx(TElement element) {//MUST preserve node order (do not override further)
			uint result = Enumerable
				.Range(0, this.Dim)
				.Aggregate(0u, (agg, d) =>
					element.Position[d].CompareTo(this.Center[d]) >= 0
						? agg | (1u << d)
						: agg);
			return result;
		}
		protected uint InvertQuadrantIdx(uint quadrantMask) {
			return ~(quadrantMask << (32 - this.Dim)) >> (32 - this.Dim);//complement of only the least significant bits
		}

		protected override IEnumerable<TSelf> FormNodes() {
			return this.FormNewNodeCorners(this.ChooseSizeFraction())
				.Select(c => this.NewInstance(c.Item1, c.Item2, (TSelf)this));
		}
		
		protected IEnumerable<Tuple<Vector<float>, Vector<float>>> FormNewNodeCorners(Vector<float> sizeFraction) {
			Vector<float> center = this.CornerLeft + this.Size * sizeFraction;
			return Enumerable
				.Range(0, 1 << this.Dim)//the 2^dimension "quadrants" of the Euclidean hyperplane
				.Select(q => (uint)q)
				.Select(q => {
					bool[] isLeft = Enumerable
						.Range(0, this.Dim)
						.Select(d => (q & (1u << d)) == 0u)
						.ToArray();
					return new Tuple<Vector<float>, Vector<float>>(
						VectorFunctions.New(isLeft.Select((l, i) => l ? this.CornerLeft[i] : center[i])),
						VectorFunctions.New(isLeft.Select((l, i) => l ? center[i] : this.CornerRight[i])));
				});
		}
		protected virtual Vector<float> ChooseSizeFraction() {
			return VectorFunctions.New(Enumerable.Repeat(0.5f, this.Dim));
		}

		//public IEnumerable<TSelf> RefineToSize(T size) {
		//	if (this.ElementCount > 0)
		//		if (size < this.MinEdgeLength)
		//			foreach (TSelf node in this.Children.Cast<TSelf>().SelectMany(child => child.RefineToSize(size)))
		//				yield return node;
		//		else yield return (TSelf)this;
		//}
		//protected double[] ChooseRandomCenter(Random rand = null) {
		//	return this.CornerLeft.Zip(this.CornerRight, (a, b) => a + (rand ?? new Random()).NextDouble() * (b - a)).ToArray();
		//}
		//protected double[] ChooseClusterCenter() {
		//	double minDivision = 1d / (1 << (this.Capacity / 2));
		//	return Enumerable
		//		.Range(0, this.Dimensionality)
		//		.Select(d => this._members.Average(m => m.Coordinates[d]))
		//		.Select((avg, d) =>
		//			avg < this.CornerLeft[d] + minDivision * (this.CornerRight[d] - this.CornerLeft[d])
		//				? this.CornerLeft[d] + minDivision * (this.CornerRight[d] - this.CornerLeft[d])
		//				: avg > this.CornerRight[d] - minDivision * (this.CornerRight[d] - this.CornerLeft[d])
		//					? this.CornerRight[d] - minDivision * (this.CornerRight[d] - this.CornerLeft[d])
		//					: avg)
		//		.ToArray();
		//}

		//protected virtual void ShuffleChildren(Random rand = null) {
		//	(rand ?? new Random()).ShuffleInPlace(this._quadrants);
		//}
		//protected QuadTree<E> GetContainingLeaf(E element) {
		//	foreach (QuadTree<E> node in this.Children)
		//		if (node.Contains(element))
		//			return node.GetContainingLeaf(element);
		//	return this;
		//}
	}
}