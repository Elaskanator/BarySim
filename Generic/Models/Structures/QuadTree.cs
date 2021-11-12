using System;
using System.Collections.Generic;
using System.Linq;

namespace Generic.Models {
	public abstract class AQuadTree<E, T> : ATree<E>
	where E : VectorDouble
	where T : AQuadTree<E, T> {
		public virtual int Capacity => 5;
		//dividing by 2 enough times WILL reach the sig figs limit of System.Double and cause zero-sized subtrees (and that's before reaching the stack frame depth limit due to recursion)
		public const int MAX_DEPTH = 48;
		
		public readonly double[] LeftCorner;
		public readonly double[] Center;
		public readonly double[] RightCorner;

		public AQuadTree(double[] corner1, double[] corner2, T parent = null) 
		: base(parent) {//make sure all values in x1 are smaller than x2 (the corners of a cubic volume)
			this.LeftCorner = corner1;
			this.RightCorner = corner2;

			this._members = new E[this.Capacity];

			//if (Enumerable.Range(0, corner1.Length).All(d => corner1[d] == corner2[d])) throw new ArgumentException("Domain has no volume");

			//var orderedCornerPoints = corner1.Zip(corner2, (a, b) => new { Min = a < b ? a : b, Max = a < b ? b : a }).ToArray();
			//this.LeftCorner = orderedCornerPoints.Select(t => t.Min).ToArray();
			//this.RightCorner = orderedCornerPoints.Select(t => t.Max).ToArray();
			this.Center = corner1.Zip(corner2, (a, b) => (a + b) / 2d).ToArray();//average of each dimension
		}
		
		public int NumMembers { get; private set; }
		public int Dimensionality { get { return this.LeftCorner.Length; } }
		public override IEnumerable<T> Children { get { return this._quadrants; } }
		public override bool IsLeaf { get { return this._quadrants.Length == 0; } }
		public override IEnumerable<E> NodeElements { get {
			if (this.NumMembers < Capacity)
				return this._members.Take(this.NumMembers);
			else if (this.Depth < MAX_DEPTH)
				return Enumerable.Empty<E>();
			else return this._members.Concat(this._leftovers);
		} }

		private T[] _quadrants = Array.Empty<T>();
		private readonly E[] _members;//entries in non-leaves are leftovers that must be ignored
		private List<E> _leftovers;

		protected override void AddElementToNode(E element) {
			if (this.NumMembers < Capacity) {
				this._members[this.NumMembers] = element;
			} else {
				if (this.Depth < MAX_DEPTH) {
					if (this.NumMembers == Capacity) {//one-time quadrant formation
						this._quadrants = this.OrganizeNodes(this.FormNodes());
						foreach (E member in this._members)
							this.GetContainingChild(member).AddElementToNode(member);
					}
					this.GetContainingChild(element).AddElementToNode(element);
				} else {
					this._leftovers ??= new List<E>();//one-time
					this._leftovers.Add(element);
				}
			}
			this.NumMembers++;
		}

		public override bool DoesContain(E element) {
			for (int d = 0; d < this.Dimensionality; d++)//all dimensions must overlap, using left-handedl ranges [a, b) = a <= x < b
				if (element.Coordinates[d] < this.LeftCorner[d] || this.RightCorner[d] <= element.Coordinates[d])
					return false;
			return true;//all dimensions overlap (or there are none - should be impossible if derived classes are behaving)
		}

		public override T GetContainingChild(E element) {
			//return this._quadrants[Enumerable.Range(0, this.Dimensionality).Sum(d => element.Coordinates[d] >= this.Center[d] ? 1 << d : 0)];
			//^^ doesn't work if randomizing nodes (this.OrganizeNodes() method)
			return this._quadrants.Single(q => q.DoesContain(element));
		}
		protected virtual T[] OrganizeNodes(T[] nodes) { return nodes; }

		protected abstract T NewNode(double[] cornerA, double[] cornerB, T parent);

		private T[] FormNodes() {
			double[] sizeHalved = this.LeftCorner.Zip(this.RightCorner, (a, b) => (b - a) / 2d).ToArray();
			return Enumerable
				.Range(0, 1 << this.Dimensionality)//the 2^dimension "quadrants" of the Euclidean hyperplane
				.Select(q => {
					double[] corner = Enumerable
						.Range(0, this.Dimensionality)
						.Select(d =>
							this.LeftCorner[d]
							+ (sizeHalved[d]
								* ((q & (1 << d)) == 0
									? 0d//smaller half
									: 1d)))
						.ToArray();
					return new {
						LeftCorner = corner,
						RightCorner = corner.Zip(sizeHalved, (a, b) => a + b).ToArray()};
				})
				.Select(x => this.NewNode(x.LeftCorner, x.RightCorner, (T)this))
				.ToArray();
		}

		public override string ToString() {
			return string.Format("Node[<{0}> thru <{1}>][{2} members]",
				string.Join(", ", this.LeftCorner),
				string.Join(", ", this.RightCorner),
				this.NumMembers);
		}
	}

	public class QuadTree<E> : AQuadTree<E, QuadTree<E>>
	where E : VectorDouble {
		public QuadTree(double[] corner1, double[] corner2, QuadTree<E> parent = null)
		: base(corner1, corner2, parent) { }

		protected override QuadTree<E> NewNode(double[] cornerA, double[] cornerB, QuadTree<E> parent) {
			return new QuadTree<E>(cornerA, cornerB, parent);
		}
	}
}