using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Generic.Vectors;

namespace Generic.Models {
	public abstract class AQuadTree<TElement, TSelf> : AVectorTree<TElement, TSelf>
	where TElement : AParticle<TElement>, IEquatable<TElement>, IEqualityComparer<TElement>
	where TSelf : AQuadTree<TElement, TSelf> {
		public AQuadTree(int dim, Vector<float> corner1, Vector<float> corner2, TSelf parent = null) 
		: base(dim, corner1, corner2, parent) { }

		public TSelf AddUp(TElement element) {
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
				foreach (Tuple<Vector<float>, Vector<float>> nodeCorners in parent.FormNewNodeCorners(sizeFraction)) {
					newNodes[i] = i == antidirectionMask
						? node//the current layer
						: this.NewNode(nodeCorners.Item1, nodeCorners.Item2, parent);
					i++;
				}
				parent._children = newNodes;
				node = newNodes[directionMask];
			}

			return node.Add(element);
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
				.Select(c => this.NewNode(c.Item1, c.Item2, (TSelf)this));
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