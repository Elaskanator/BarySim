using System;
using System.Collections.Generic;
using System.Linq;

namespace Generic.Models {
	public abstract class AQuadTree<E, T> : ATree<E>
	where E : SimpleVector
	where T : AQuadTree<E, T> {
		public const int CAPACITY = 5;
		//dividing by 2 enough times WILL reach the sig figs limit of System.Double and cause zero-sized subtrees (before reaching the stack frame depth limit due to recursion)
		public const int MAX_DEPTH = 50;
		
		public readonly SimpleVector LeftCorner;
		public readonly SimpleVector Center;
		public readonly SimpleVector RightCorner;
		
		public int NumMembers { get; private set; }
		public int Dimensionality { get { return this.LeftCorner.Dimensionality; } }
		public override IEnumerable<T> Children { get { return this._quadrants; } }
		public override bool IsLeaf { get { return this._quadrants.Length == 0; } }
		public override IEnumerable<E> NodeElements { get {
			if (this.Depth < MAX_DEPTH || this.NumMembers < CAPACITY)
				return this._members.Take(this.NumMembers);
			else
				return this._members.Concat(this._leftovers);
		} }

		private T[] _quadrants = Array.Empty<T>();
		private readonly E[] _members = new E[CAPACITY];//entries in non-leaves are leftovers that must be ignored
		private List<E> _leftovers;

		public AQuadTree(SimpleVector corner1, SimpleVector corner2, T parent = null) 
		: base(parent) {//make sure all values in x1 are smaller than x2 (the corners of a cubic volume)
			if (Enumerable.Range(0, corner1.Coordinates.Length).All(d => corner1.Coordinates[d] == corner2.Coordinates[d])) throw new ArgumentException("Domain has no volume");

			var orderedCornerPoints = corner1.Coordinates.Zip(corner2.Coordinates, (a, b) => new { Min = a < b ? a : b, Max = a < b ? b : a }).ToArray();
			this.LeftCorner = orderedCornerPoints.Select(t => t.Min).ToArray();
			this.RightCorner = orderedCornerPoints.Select(t => t.Max).ToArray();
			this.Center = corner1.Coordinates.Zip(corner2.Coordinates, (a, b) => (a + b) / 2d).ToArray();//average of each dimension
		}

		public override bool DoesContain(E element) {
			for (int d = 0; d < this.Dimensionality; d++)//all dimensions must overlap
				if (this.LeftCorner.Coordinates[d] > element.Coordinates[d] || element.Coordinates[d] > this.RightCorner.Coordinates[d])
					return false;
			return true;//all dimensions overlap (or there are none)
		}

		protected override void AddElementToNode(E element) {
			if (this.NumMembers < CAPACITY) this._members[this.NumMembers] = element;
			else {
				if (this.Depth < MAX_DEPTH) {
					if (this.NumMembers == CAPACITY) {//one-time quadrant formation
						this._quadrants = this.FormNodes().ToArray();
						foreach (E member in this._members)
							this.GetContainingChild(member).Add(member);
					}
					this.GetContainingChild(element).Add(element);
				} else {
					this._leftovers ??= new List<E>();//one-time
					this._leftovers.Add(element);
				}
			}
			this.NumMembers++;
		}

		public override T GetContainingChild(E element) {
			int quadrantIdx = Enumerable.Range(0, this.Dimensionality).Sum(d => element.Coordinates[d] >= this.Center.Coordinates[d] ? 1 << d : 0);
			return this._quadrants[quadrantIdx];
		}

		protected abstract T NewNode(double[] cornerA, double[] cornerB, T parent);

		private IEnumerable<T> FormNodes() {
			foreach (var newNodeData in Enumerable
				.Range(0, 1 << this.Dimensionality)//the 2^dimension "quadrants" of the Euclidean hyperplane
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
					return new {
						LeftCorner = cornerA,
						RightCorner = cornerA.Zip(sizeHalved, (a, b) => a + b).ToArray(),
						Parent = (T)this };
				}))
				yield return this.NewNode(newNodeData.LeftCorner, newNodeData.RightCorner, newNodeData.Parent);
		}

		public override string ToString() {
			return string.Format("Node[<{0}> thru <{1}>][{2} members]",
				string.Join(", ", this.LeftCorner),
				string.Join(", ", this.RightCorner),
				this.NumMembers);
		}
	}

	public class QuadTree<E> : AQuadTree<E, QuadTree<E>>
	where E : SimpleVector {
		public QuadTree(SimpleVector corner1, SimpleVector corner2, QuadTree<E> parent = null)
		: base(corner1, corner2, parent) { }

		protected override QuadTree<E> NewNode(double[] cornerA, double[] cornerB, QuadTree<E> parent) {
			return new QuadTree<E>(cornerA, cornerB, parent);
		}
	}
}