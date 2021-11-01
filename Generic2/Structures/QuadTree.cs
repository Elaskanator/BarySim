using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Generic.Abstractions;

namespace Generic.Structures {
	public class QuadTree<T>
	where T : IVector{
		public const int CAPACITY = 5;

		private Node _self;
		
		public QuadTree(double[] cornerA, double[] cornerB) {
			this._self = new Node(cornerA, cornerB);
		}

		public IEnumerable<T> GetAll() {
			return this._self.GetAll();
		}

		public QuadTree(IVector domain) : this(domain.Coordinates) { }
		public QuadTree(double[] range) : this(Enumerable.Repeat(0d, range.Length).ToArray(), range) { }
		public QuadTree(IVector cornerA, IVector cornerB) : this(cornerA.Coordinates, cornerB.Coordinates) { }

		public void Add(T v) {
			this._self.Add(v);
		}

		public void Clear() {
			this._self.Clear();
		}

		public IEnumerable<T> GetNeighbors_Fast(double[] v, double radius) {
			double[]//cube neighborhood
				x1 = v.Select(x => x - radius).ToArray(),
				x2 = v.Select(x => x + radius).ToArray();
			if (Node.DoesIntersect(x1, x2, this._self.X1, this._self.X2))
				return this._self.GetNeighbors_Fast(x1, x2);
			else return new T[0];
		}
		public IEnumerable<T> GetNeighbors_Fast(IVector v, double radius) { return this.GetNeighbors_Fast(v.Coordinates, radius); }

		private class Node {
			public readonly double[] X1;
			public readonly double[] X2;
			public readonly double[] Center;

			private Node[] _quadrants = null;
			private readonly List<T> _members = new List<T>(CAPACITY);

			public Node(double[] x1, double[] x2) {//make sure all values in x1 are smaller than x2 (the corners of a cubic volume)
				this.X1 = x1;
				this.X2 = x2;
				this.Center = x1.Zip(x2, (a, b) => (a + b) / 2d).ToArray();//average of each dimension
			}

			public void Add(T v) {
				if (this._members.Count < CAPACITY) {
					this._members.Add(v);
				} else {
					this._quadrants ??= this.FormNodes();//one-time quadrant formation

					Node targetNode = this.ChooseNode(v.Coordinates);
					targetNode.Add(v);
				}
			}

			public IEnumerable<T> GetAll() {
				foreach (T m in this._members)
					yield return m;

				if (!(this._quadrants is null))
					foreach (T m in this._quadrants.SelectMany(q => q.GetAll()))
						yield return m;
			}

			public IEnumerable<T> GetNeighbors_Fast(double[] x1, double[] x2) {
				return this._members
					.Where(m => Node.DoesContain(x1, x2, m.Coordinates))
					.Concat(
						(this._quadrants ?? new Node[0])
							.Where(q => Node.DoesIntersect(x1, x2, q.X1, q.X2))
							.SelectMany(q => q.GetNeighbors_Fast(x1, x2)));
			}

			public void Clear() {
				this._members.Clear();
				this._quadrants = null;
			}

			private Node ChooseNode(double[] coordinates) {
				int quadrantIdx = Enumerable
					.Range(0, this.X1.Length)
					.Sum(d => coordinates[d] >= this.Center[d]
						? 1 << d
						: 0);
				return _quadrants[quadrantIdx];
			}

			private Node[] FormNodes() {
				return Enumerable
					.Range(0, 1 << this.X1.Length)//the 2^dimension "quadrants" of the Euclidean hyperplane
					.Select(q => {
						double[]
							sizeHalved = this.X1.Zip(this.X2, (a, b) => (b - a) / 2d).ToArray(),
							cornerA = Enumerable
								.Range(0, this.X1.Length)
								.Select(d =>
									this.X1[d]
									+ (sizeHalved[d]
										* ((q & (1 << d)) == 0//smaller size of quadrant
											? 0
											: 1)))
								.ToArray();
						return new Node(
							cornerA,
							cornerA.Zip(sizeHalved, (a, b) => a + b).ToArray());
					})
					.ToArray();
			}

			public static bool DoesIntersect(double[] x1, double[] x2, double[] y1, double[] y2) {
				//intersection test uses left-handed range convention [a, b)
				for (int d = 0; d < x1.Length; d++)//all dimensions must overlap
					if (x1[d] >= y2[d] || y1[d] >= x2[d]) return false;//does not overlap

				return true;//no non-overlapping dimensions (I don't care about the stupid case of zero dimensions)
			}
			public static bool DoesContain(double[] x1, double[] x2, double[] p) {
				//intersection test uses left-handed range convention [a, b)
				for (int d = 0; d < x1.Length; d++)//all dimensions must overlap
					if (x1[d] >= p[d] || p[d] >= x2[d]) return false;//does not contain

				return true;//no non-overlapping dimensions (I don't care about the stupid case of zero dimensions)
			}

			public override string ToString() {
				return string.Format("Node[<{0}> thru <{1}>]",
					string.Join(", ", this.X1),
					string.Join(", ", this.X2));
			}
		}
	}
}