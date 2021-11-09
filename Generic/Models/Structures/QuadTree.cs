using System;
using System.Collections.Generic;
using System.Linq;

namespace Generic.Models {
	public class QuadTree<T> : ATree<T>
	where T : Vector {
		public const int CAPACITY = 5;
		//dividing by 2 enough times WILL reach the sig figs limit of System.Double and cause zero-sized subtrees (before reaching the stack frame depth limit due to recursion)
		public const int MAX_DEPTH = 50;
		
		public readonly Vector LeftCorner;
		public readonly Vector Center;
		public readonly Vector RightCorner;
		
		public int NumMembers { get; private set; }
		public int Dimensionality { get { return this.LeftCorner.Dimensionality; } }
		public override IEnumerable<QuadTree<T>> Children { get { return this._quadrants; } }
		public override bool IsLeaf { get { return this._quadrants.Length == 0; } }
		public override IEnumerable<T> NodeElements { get {
			if (this.Depth < MAX_DEPTH || this.NumMembers < CAPACITY)
				return this._members.Take(this.NumMembers);
			else
				return this._members.Concat(this._leftovers);
		} }

		private QuadTree<T>[] _quadrants = Array.Empty<QuadTree<T>>();
		private readonly T[] _members = new T[CAPACITY];//entries in non-leaves are leftovers that must be ignored
		private List<T> _leftovers;

		public QuadTree(Vector corner1, Vector corner2, QuadTree<T> parent = null) 
		: base(parent) {//make sure all values in x1 are smaller than x2 (the corners of a cubic volume)
			if (Enumerable.Range(0, corner1.Coordinates.Length).All(d => corner1.Coordinates[d] == corner2.Coordinates[d])) throw new ArgumentException("Domain has no volume");

			var orderedCornerPoints = corner1.Coordinates.Zip(corner2.Coordinates, (a, b) => new { Min = a < b ? a : b, Max = a < b ? b : a }).ToArray();
			this.LeftCorner = (Vector)orderedCornerPoints.Select(t => t.Min).ToArray();
			this.RightCorner = (Vector)orderedCornerPoints.Select(t => t.Max).ToArray();
			this.Center = (Vector)corner1.Coordinates.Zip(corner2.Coordinates, (a, b) => (a + b) / 2d).ToArray();//average of each dimension
		}

		public override bool DoesContain(T element) {
			for (int d = 0; d < this.Dimensionality; d++)//all dimensions must overlap
				if (this.LeftCorner.Coordinates[d] > element.Coordinates[d] || element.Coordinates[d] > this.RightCorner.Coordinates[d])
					return false;
			return true;//all dimensions overlap (or there are none)
		}

		protected override void AddElementToNode(T element) {
			if (this.NumMembers < CAPACITY) this._members[this.NumMembers] = element;
			else {
				if (this.Depth < MAX_DEPTH) {
					if (this.NumMembers == CAPACITY) {//one-time quadrant formation
						this._quadrants = this.FormNodes();
						foreach (T member in this._members)
							this.GetContainingChild(member).Add(member);
					}
					this.GetContainingChild(element).Add(element);
				} else {
					this._leftovers ??= new List<T>();//one-time
					this._leftovers.Add(element);
				}
			}
			this.NumMembers++;
		}

		public override QuadTree<T> GetContainingChild(T element) {
			int quadrantIdx = Enumerable.Range(0, this.Dimensionality).Sum(d => element.Coordinates[d] >= this.Center.Coordinates[d] ? 1 << d : 0);
			return this._quadrants[quadrantIdx];
		}

		private QuadTree<T>[] FormNodes() {
			QuadTree<T>[] result = Enumerable.Range(0, 1 << this.Dimensionality)//the 2^dimension "quadrants" of the Euclidean hyperplane
				.Select(q => {
					double[]
						sizeHalved = this.LeftCorner.Coordinates.Zip(this.RightCorner.Coordinates, (a, b) => (b - a) / 2d).ToArray(),
						cornerA = Enumerable
							.Range(0, this.Dimensionality)
							.Select(d =>
								this.LeftCorner.Coordinates[d]
								+ (sizeHalved[d]
									* ((q & (1 << d)) == 0
										? 0//smaller half
										: 1)))
							.ToArray();
					return new QuadTree<T>(
						(Vector)cornerA,
						(Vector)cornerA.Zip(sizeHalved, (a, b) => a + b).ToArray(),
						this);
				})
				.ToArray();
			return result;
		}

		public override string ToString() {
			return string.Format("Node[<{0}> thru <{1}>][{2} members]",
				string.Join(", ", this.LeftCorner),
				string.Join(", ", this.RightCorner),
				this.NumMembers);
		}
	}
}