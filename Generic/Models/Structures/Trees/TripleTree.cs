using System;
using System.Collections.Generic;
using System.Linq;
using Generic.Vectors;

namespace Generic.Models {
	//public class TripleTree<T, TElement> : AQuadTree<T, TElement, TripleTree<T, TElement>>
	//where T : IComparable<T>
	//where TElement : AParticle<T> {
	//	public TripleTree(AVector<float> corner1, AVector<float> corner2, TripleTree<T, TElement> parent = null)
	//	: base(corner1, corner2, parent) { }
	//	protected override TripleTree<T, TElement> NewNode(AVector<float> cornerA, AVector<float> cornerB, TripleTree<T, TElement> parent) {
	//		return new TripleTree<T, TElement>(cornerA, cornerB, parent);
	//	}
		
	//	public override int MaxDepth => 30;

	//	protected override uint GetChildIndex(AVector<float> coordinates) {
	//		uint idx = 0u, b = 1u;
	//		for (uint d = 0u; d < this.Dim; d++) {
	//			idx += b * (uint)(3d * (coordinates[d] - this.CornerLeft[d]) / this.Size[d]);
	//			b *= 3u;
	//		}
	//		return idx;
	//	}
	//	protected override IEnumerable<Tuple<AVector<float>, AVector<float>>> FormNewNodeCorners() {
	//		return this
	//			.RecursiveNodeFormation(this.Dim - 1, Enumerable.Empty<T>())
	//			.Select(leftCorner =>
	//				new Tuple<AVector<float>, AVector<float>>(
	//					leftCorner,
	//					leftCorner.Zip(this.Size, (l, s) => l + s/3d).ToArray()));
	//	}
	//	private IEnumerable<AVector<float>> RecursiveNodeFormation(int d, IEnumerable<T> running) {
	//		T coord = this.CornerLeft[d];
	//		for (int i = 0; i < 3; i++) {
	//			if (d == 0)
	//				yield return running.Prepend(coord).ToArray();
	//			else foreach (AVector<float> fullCoord in this.RecursiveNodeFormation(d - 1, running.Prepend(coord)))
	//				yield return fullCoord;
	//			coord += this.Size[d]/3;
	//		}
	//	}
	//}
}