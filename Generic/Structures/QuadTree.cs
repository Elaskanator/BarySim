using System;
using System.Collections.Generic;
using System.Linq;
using Generic.Abstractions;

namespace Generic.Structures {
	public class QuadTree<T> where T : IVector {
		public const int CAPACITY = 5;
		public const int MAX_DEPTH = 50;//dividing by 2 enough times WILL reach the sig figs limit of System.Double and cause zero-sized subtrees

		public readonly int Depth;
		public readonly int Dimensionality;
		public readonly double[] LeftCorner;
		public readonly double[] Center;
		public readonly double[] RightCorner;

		public bool IsLeaf { get { return this.Quadrants is null; } }
		public bool IsRoot { get { return this._parent is null; } }
		public IEnumerable<T> AllMembers { get {
			if (this.IsLeaf) {
				if (this.Depth < MAX_DEPTH || this.NumMembers < CAPACITY) return this._members.Take(this.NumMembers);
				else return this._members.Concat(this._leftovers);
			} else return this.Quadrants.SelectMany(q => q.AllMembers);
		} }
		public IEnumerable<QuadTree<T>> AllQuadrants { get {
			if (this.IsLeaf) return new[] { this };
			else return this.Quadrants.SelectMany(q => q.AllQuadrants);
		} }

		public int NumMembers { get; private set; }

		private readonly QuadTree<T> _parent;
		public QuadTree<T>[] Quadrants { get; private set; }
		private readonly T[] _members = new T[CAPACITY];//entries in non-leaves are leftovers that must be ignored
		private List<T> _leftovers;

		protected QuadTree(double[] x1, double[] x2, QuadTree<T> parent, IEnumerable<T> init = null) {//make sure all values in x1 are smaller than x2 (the corners of a cubic volume)
			this.Dimensionality = x1.Length;
			if (Enumerable.Range(0, this.Dimensionality).All(d => x1[d] == x2[d])) throw new ArgumentException("Domain has no volume");

			var orderedCornerPoints = x1.Zip(x2, (a, b) => new { Min = a < b ? a : b, Max = a < b ? b : a }).ToArray();

			this.LeftCorner = orderedCornerPoints.Select(t => t.Min).ToArray();
			this.RightCorner = orderedCornerPoints.Select(t => t.Max).ToArray();
			this._parent = parent;
			if (!(parent is null)) this.Depth = parent.Depth + 1;

			this.NumMembers = 0;
			this.Center = x1.Zip(x2, (a, b) => (a + b) / 2d).ToArray();//average of each dimension

			if (!(init is null)) this.AddRange(init);
		}
		public QuadTree(double[] x1, double[] x2) : this(x1, x2, null, null) { }
		public QuadTree(IEnumerable<T> init, double[] x1, double[] x2) : this(x1, x2, null, init) { }
		public QuadTree(IEnumerable<T> init, double[] range) : this(Enumerable.Repeat(0d, range.Length).ToArray(), range, null, init) { }
		public QuadTree(double[] range) : this(Enumerable.Repeat(0d, range.Length).ToArray(), range, null) { }
		protected QuadTree(double[] range, QuadTree<T> parent) : this(Enumerable.Repeat(0d, range.Length).ToArray(), range, parent) { }

		public void Clear() {
			this.NumMembers = 0;
			this.Quadrants = null;
		}

		public void AddRange(IEnumerable<T> entities) {
			foreach (T entry in entities) {
				if (this.NumMembers < CAPACITY) this._members[this.NumMembers] = entry;
				else {
					if (this.Depth < MAX_DEPTH) {
						if (this.NumMembers == CAPACITY) {//one-time quadrant formation
							this.Quadrants = this.FormNodes();
							foreach (T member in this._members)
								this.ChooseQuadrant(member.Coordinates).Add(member);
						}
						this.ChooseQuadrant(entry.Coordinates).Add(entry);
					} else {
						this._leftovers ??= new List<T>();//one-time
						this._leftovers.Add(entry);
					}
				}
				this.NumMembers++;
			}
		}
		public void Add(params T[] entities) { this.AddRange(entities); }

		public IEnumerable<QuadTree<T>> GetLeaves() {
			if (this.IsLeaf) yield return this;
			else foreach (QuadTree<T> n in this.Quadrants.SelectMany(q => q.GetLeaves())) yield return n;
		}
		public QuadTree<T> GetLeaf(double[] v) {
			if (this.IsLeaf) return this;
			else return this.Quadrants.First(q => q.DoesContain(v)).GetLeaf(v);
		}
		public QuadTree<T> GetLeaf(IVector v) { return this.GetLeaf(v.Coordinates); }

		public IEnumerable<T> GetNeighbors(double[] x1, double[] x2) {
			if (this.IsLeaf)
				foreach (T member in this.AllMembers) yield return member;
			else foreach (T member in this.Quadrants
				.Where(q => x1 is null || DoesIntersect(x1, x2, q.LeftCorner, q.RightCorner))
				.SelectMany(q => q.GetNeighbors(x1, x2)))
					yield return member;//recurse down first to get nearer matches faster;
		}
		public IEnumerable<T> GetNeighbors() { return this.GetNeighbors(null, null); }
		public IEnumerable<T> GetNeighbors(double[] v, double radius = 0) {
			if (radius > 0) {
				double[]//cube neighborhood
					x1 = v.Select(x => x - radius).ToArray(),
					x2 = v.Select(x => x + radius).ToArray();
				return this.GetNeighbors(x1, x2);
			} else return this.GetNeighbors(null, null);
		}
		public IEnumerable<T> GetNeighbors(IVector v, double radius = 0) {
			return this.GetNeighbors(v.Coordinates, radius);
		}

		/// <summary>
		/// Gets neighbors roughly in order of closest by seeking through nearby quadrants recursively upward
		/// </summary>
		/// <param name="v">The location to seek around</param>
		/// <param name="radiusFilter">An optional maximum offset per dimension allowed (fast approximation to Euclidean distance filtering)</param>
		public IEnumerable<T> GetNeighborsAlt(IVector v, bool sort = false) { return this.GetNeighborsAlt(v.Coordinates, sort); }
		public IEnumerable<T> GetNeighborsAlt(double[] v, bool sort = false) {
			if (v is null || !this.DoesContain(v)) {
				if (this.IsRoot) foreach (T member in this.GetNeighborsAlt(this.LeftCorner, sort)) yield return member;
				else foreach (T member in this._parent.GetNeighborsAlt(v, sort)) yield return member;
			} else {
				if (!this.IsLeaf) foreach (T member in this.Quadrants.First(q => q.DoesContain(v)).GetNeighborsAlt(v, sort)) yield return member;
				else {
					foreach (T member in this.AllMembers) yield return member;
					if (!this.IsRoot) foreach (T member in this.SeekUpward(v, sort).SelectMany(n => n.AllMembers)) yield return member;
				}
			}
		}
		private IEnumerable<QuadTree<T>> SeekUpward(double[] v, bool sort) {
			IEnumerable<QuadTree<T>> neighborNodes = this._parent.Quadrants.Except(q => ReferenceEquals(this, q));
			if (sort) neighborNodes = neighborNodes.OrderBy(q => q.Center.Distance(v));
			foreach (QuadTree<T> node in neighborNodes.SelectMany(q => q.AllQuadrants)) yield return node;
			if (!this._parent.IsRoot) foreach (QuadTree<T> node in this._parent.SeekUpward(v, sort)) yield return node;
		}

		private QuadTree<T> ChooseQuadrant(double[] coordinates) {
			int quadrantIdx = Enumerable.Range(0, this.Dimensionality).Sum(d => coordinates[d] >= this.Center[d] ? 1 << d : 0);
			return this.Quadrants[quadrantIdx];
		}

		private QuadTree<T>[] FormNodes() {
			QuadTree<T>[] result = Enumerable.Range(0, 1 << this.Dimensionality)//the 2^dimension "quadrants" of the Euclidean hyperplane
				.Select(q => {
					double[]
						sizeHalved = this.LeftCorner.Zip(this.RightCorner, (a, b) => (b - a) / 2d).ToArray(),
						cornerA = Enumerable
							.Range(0, this.Dimensionality)
							.Select(d =>
								this.LeftCorner[d]
								+ (sizeHalved[d]
									* ((q & (1 << d)) == 0
										? 0//smaller half
										: 1)))
							.ToArray();
					return new QuadTree<T>(
						cornerA,
						cornerA.Zip(sizeHalved, (a, b) => a + b).ToArray(),
						this);
				})
				.ToArray();
			return result;
		}

		public bool DoesContain(double[] v) { return DoesContain(this.LeftCorner, this.RightCorner, v); }

		public static bool DoesIntersect(double[] x1, double[] x2, double[] y1, double[] y2) {
			//intersection test uses left-handed range convention [a, b)
			for (int d = 0; d < x1.Length; d++)//all dimensions must overlap
				if (x1[d] > y2[d] || y1[d] > x2[d]) return false;//does not overlap

			return true;//no non-overlapping dimensions (I don't care about the stupid case of zero dimensions)
		}
		public static bool DoesContain(double[] x1, double[] x2, double[] v) {
			//intersection test uses left-handed range convention [a, b)
			for (int d = 0; d < x1.Length; d++)//all dimensions must overlap
				if (x1[d] > v[d] || v[d] > x2[d]) return false;//does not contain

			return true;//no non-overlapping dimensions (I don't care about the stupid case of zero dimensions)
		}

		public override string ToString() {
			return string.Format("Node[<{0}> thru <{1}>][{2} members]",
				string.Join(", ", this.LeftCorner),
				string.Join(", ", this.RightCorner),
				this.NumMembers);
		}
	}

	public class QuadTree : QuadTree<SimpleVector> {
		public QuadTree(double[] x1, double[] x2, QuadTree parent = null) : base(x1, x2, parent) { }

		public void Add(params double[][] entries) { base.Add(Array.ConvertAll(entries, entry => (SimpleVector)entry)); }
	}
}

/*
int numElements = 10000;
double[] v = new double[] { 0.5, 0.5 };
var tree = new Generic.Structures.QuadTree(new double[] { 0, 0 }, new double[] { 1, 1 });
tree.Add(Enumerable.Range(0, numElements).Select(i => new double[] { rand.NextDouble(), rand.NextDouble() }).ToArray());
var neighbors = tree.Quadrants[2].Quadrants[1].GetNeighborsAlt(v, true).ToArray();
var neighborDistances = neighbors.Select(x => x.Coordinates.Distance(v)).ToArray();
*/