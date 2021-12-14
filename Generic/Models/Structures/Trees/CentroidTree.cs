using System;
using System.Collections.Generic;
using System.Linq;
using Generic.Vectors;

namespace Generic.Models {
	public class CentroidTree<TElement> : QuadTree<TElement>
	where TElement : VectorDouble {
		public CentroidTree(double[] corner1, double[] corner2, QuadTree<TElement> parent = null)
		: base(corner1, corner2, parent) {
			this.Size = this.CornerLeft.Zip(this.CornerRight, (l, r) => r - l).ToArray();
		}
		protected override QuadTree<TElement> NewNode(double[] cornerA, double[] cornerB, QuadTree<TElement> parent) {
			return new CentroidTree<TElement>(cornerA, cornerB, parent);
		}

		public readonly double[] Size;

		protected override QuadTree<TElement> GetContainingChild(TElement element) {
			int idx = 0, b = 1;
			for (int d = 0; d < this.Dimensionality; d++) {
				idx += b * (int)(3d * (element.Coordinates[d] - this.CornerLeft[d]) / this.Size[d]);
				b *= 3;
			}
			return this._quadrants[idx];
		}
		protected override IEnumerable<Tuple<double[], double[]>> FormNodeCorners() {
			var shite = this
				.RecursiveNodeFormation(this.Dimensionality - 1, Enumerable.Empty<double>())
				.ToArray();
			return this
				.RecursiveNodeFormation(this.Dimensionality - 1, Enumerable.Empty<double>())
				.Select(leftCorner =>
					new Tuple<double[], double[]>(
						leftCorner,
						leftCorner.Zip(this.Size, (l, s) => l + s/3d).ToArray()));
		}
		private IEnumerable<double[]> RecursiveNodeFormation(int d, IEnumerable<double> running) {
			double coord = this.CornerLeft[d];
			for (int i = 0; i < 3; i++) {
				if (d == 0)
					yield return running.Prepend(coord).ToArray();
				else foreach (double[] fullCoord in this.RecursiveNodeFormation(d - 1, running.Prepend(coord)))
					yield return fullCoord;
				coord += this.Size[d]/3;
			}
		}
	}
}