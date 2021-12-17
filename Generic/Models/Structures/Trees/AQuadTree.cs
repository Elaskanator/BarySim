using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Generic.Vectors;

namespace Generic.Models {
	public abstract class AQuadTree<TElement, TSelf> : AVectorTree<TElement, TSelf>
	where TElement : AParticle
	where TSelf : AQuadTree<TElement, TSelf> {
		public AQuadTree(int dim, Vector<float> corner1, Vector<float> corner2, TSelf parent = null) 
		: base(dim, corner1, corner2, parent) { }
			//this.MinEdgeLength = this.Size.Min();
		//public readonly T MinEdgeLength;

		public TSelf AddUp(TElement element) {
			TSelf node = (TSelf)this, parent;
			TSelf[] newNodes;
			uint directionMask, antidirectionMask;
			Vector<float> sizeFraction;
			float[] additionalSize;
			while (!node.DoesContainCoordinates(element.Position)) {
				sizeFraction = this.ChooseSizeFraction();
				additionalSize = new float[this.Dim];
				for (int d = 0; d < this.Dim; d++)
					additionalSize[d] = this.Size[d] *  (1f / sizeFraction[d] - 1f);

				directionMask = this.GetQuadrantIdx(element.Position);//nodes MUST be in ascending order
				antidirectionMask = this.InvertQuadrantIdx(directionMask);

				node.Depth++;
				parent = this.NewNode(
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
				foreach (Tuple<Vector<float>, Vector<float>> nodeCorners in parent.FormNewNodeCorners()) {
					newNodes[i] = i == antidirectionMask
						? node
						: this.NewNode(nodeCorners.Item1, nodeCorners.Item2, parent);
					i++;
				}
				parent._children = newNodes;
				node = newNodes[directionMask];
			}

			return node.Add(element);
		}

		protected override uint GetQuadrantIdx(Vector<float> coordinates) {//MUST preserve node order (do not override further)
			return Enumerable
				.Range(0, (int)this.Dim)
				.Aggregate(0u, (agg, d) =>
					coordinates[d].CompareTo(this.Center[d]) >= 0
						? agg | (1u << d)
						: agg);
		}
		protected uint InvertQuadrantIdx(uint quadrantMask) {
			return ~(quadrantMask << (32 - this.Dim)) >> (32 - this.Dim);//complement of only the least significant bits
		}
		
		protected IEnumerable<Tuple<Vector<float>, Vector<float>>> FormNewNodeCorners(Vector<float> sizeFraction) {
			float[] center = Enumerable.Range(0, this.Dim)
				.Select(d => this.CornerLeft[d] + sizeFraction[d] * this.Size[d])
				.ToArray();
			return Enumerable
				.Range(0, 1 << this.Dim)//the 2^dimension "quadrants" of the Euclidean hyperplane
				.Select(q => (uint)q)
				.Select(q => {
					bool[] isLeft = Enumerable
						.Range(0, this.Dim)
						.Select(d => (q & (1u << d)) > 0u)
						.ToArray();
					return new Tuple<Vector<float>, Vector<float>>(
						VectorFunctions.New(isLeft.Select((l, i) => l ? this.CornerLeft[i] : center[i])),
						VectorFunctions.New(isLeft.Select((l, i) => l ? center[i] : this.CornerRight[i])));
				});
		}
		protected override IEnumerable<Tuple<Vector<float>, Vector<float>>> FormNewNodeCorners() { return this.FormNewNodeCorners(this.ChooseSizeFraction()); }
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