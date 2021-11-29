using System;
using System.Collections.Generic;
using System.Linq;
using Generic.Extensions;
using Generic.Models.Vectors;

namespace Generic.Models {
	public abstract class AVectorQuadTree<E, T> : ATree<E, T>
	where E : VectorDouble
	where T : AVectorQuadTree<E, T> {
		public AVectorQuadTree(double[] corner1, double[] corner2, T parent = null) 
		: base(parent) {//make sure all values in x1 are smaller than x2 (the corners of a cubic volume)
			this.LeftCorner = corner1;
			this.RightCorner = corner2;
			this.Center = this.MakeCenter();

			this._members = new E[this.Capacity];
		}
		public override string ToString() {
			return string.Format("Node[<{0}> thru <{1}>][{2} members]",
				string.Join(", ", this.LeftCorner),
				string.Join(", ", this.RightCorner),
				this.NumMembers);
		}

		public virtual int Capacity => 5;
		//dividing by 2 enough times WILL reach the sig figs limit of System.Double and cause zero-sized subtrees (and that's before reaching the stack frame depth limit due to recursion)
		public const int MAX_DEPTH = 40;
		
		public readonly double[] LeftCorner;
		public readonly double[] RightCorner;
		public readonly double[] Center;
		
		public int Dimensionality { get { return this.LeftCorner.Length; } }
		public override IEnumerable<T> Children { get { return this._quadrants; } }
		public override bool IsLeaf { get { return this._quadrants.Length == 0; } }
		public override IEnumerable<E> NodeElements { get {
			if (this.NumMembers <= this.Capacity)
				return this._members.Take(this.NumMembers);
			else if (this.Depth < MAX_DEPTH)
				return Enumerable.Empty<E>();
			else return this._members.Concat(this._leftovers);
		} }

		protected T[] _quadrants = Array.Empty<T>();
		private readonly E[] _members;//entries in non-leaves are leftovers that must be ignored
		private List<E> _leftovers;

		public override bool DoesContain(E element) {
			for (int d = 0; d < this.Dimensionality; d++)//all dimensions must overlap, using left-handedl ranges [a, b) = a <= x < b
				if (element.Coordinates[d] < this.LeftCorner[d] || this.RightCorner[d] <= element.Coordinates[d])
					return false;

			return true;//all dimensions overlap (or there are none - should be impossible if derived classes are behaving)
		}
		
		//Tuple<nodes within depth limit, other nodes>
		public Tuple<T[], T[]> GetNeighborhoodNodes(int depth = 3) {//must be used from a leaf
			if (this.IsRoot)
				return new(Array.Empty<T>(), Array.Empty<T>());
			else {
				Tuple<bool, T>[] parentNeighbors = this.Parent.GetNeighborhoodNodes_up(depth - 1).ToArray();
				return new(
					this.SiblingNodes
						.SelectMany(s => s.GetNeighborhoodNodes_down(depth))
						.Concat(parentNeighbors.Where(t => t.Item1))
						.Select(t => t.Item2)
						.ToArray(),
					parentNeighbors.Without(t => t.Item1).Select(t => t.Item2).ToArray());
			}
		}
		private IEnumerable<Tuple<bool, T>> GetNeighborhoodNodes_up(int depth) {
			if (!this.IsRoot) {
				foreach (Tuple<bool, T> node in this.SiblingNodes.SelectMany(s => s.GetNeighborhoodNodes_down(depth)))
					yield return node;
				foreach (Tuple<bool, T> node in this.Parent.GetNeighborhoodNodes_up(depth - 1))
					yield return node;
			}
		}
		private IEnumerable<Tuple<bool, T>> GetNeighborhoodNodes_down(int depth) {
			if (this.NumMembers == 0)
				yield break;
			else if (this.IsLeaf || depth <= 0)
				yield return new(depth >= 0, (T)this);
			else foreach (Tuple<bool, T> node in this.Children.SelectMany(c => c.GetNeighborhoodNodes_down(depth - 1)))
				yield return node;
		}

		protected override void AddElementToNode(E element) {
			if (this.NumMembers < Capacity)
				this._members[this.NumMembers] = element;
			else if (this.Depth < MAX_DEPTH) {
				if (this.NumMembers == Capacity) {//one-time quadrant formation
					this._quadrants = this.FormNodes();
					this.ArrangeNodes();
					foreach (E member in this._members)
						this.GetContainingChild(member).Add(member);
				}
				this.GetContainingChild(element).Add(element);
			} else (this._leftovers ??= new List<E>()).Add(element);
		}
		protected virtual double[] MakeCenter() {
			return this.LeftCorner.Zip(this.RightCorner, (a, b) => (a + b) / 2d).ToArray();
		}
		protected override T GetContainingChild(E element) {
			return this._quadrants[Enumerable.Range(0, this.Dimensionality).Sum(d => element.Coordinates[d] >= this.Center[d] ? 1 << d : 0)];
		}

		protected abstract T NewNode(double[] cornerA, double[] cornerB, T parent);
		protected virtual void ArrangeNodes() { return; }//MUST override GetContainingChild if rearranging nodes

		protected virtual T[] FormNodes() {
			return Enumerable
				.Range(0, 1 << this.Dimensionality)//the 2^dimension "quadrants" of the Euclidean hyperplane
				.Select(q => {
					bool[] isLeft = Enumerable
						.Range(0, this.Dimensionality)
						.Select(d => (q & (1 << d)) > 0)
						.ToArray();
					return new {
						LeftCorner = isLeft.Select((l, i) => l ? this.LeftCorner[i] : this.Center[i]).ToArray(),
						RightCorner = isLeft.Select((l, i) => l ? this.Center[i] : this.RightCorner[i]).ToArray()
					};
				}).Select(x => this.NewNode(x.LeftCorner, x.RightCorner, (T)this))
				.ToArray();
		}
	}
}