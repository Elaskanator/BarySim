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
		public virtual int MaxDepth => 40;
		
		public readonly double[] LeftCorner;
		public readonly double[] RightCorner;
		protected double[] _center;
		
		public int Dimensionality { get { return this.LeftCorner.Length; } }
		public override IEnumerable<T> Children { get { return this._quadrants; } }
		public override bool IsLeaf { get { return this._quadrants.Length == 0; } }
		public override IEnumerable<E> NodeElements { get {
			if (this.NumMembers <= this.Capacity)
				return this._members.Take(this.NumMembers);
			else if (this.Depth < this.MaxDepth)
				return Enumerable.Empty<E>();
			else return this._members.Concat(this._leftovers);
		} }

		protected T[] _quadrants = Array.Empty<T>();
		protected readonly E[] _members;//entries in non-leaves are leftovers that must be ignored
		private List<E> _leftovers;

		public override bool DoesContain(E element) {
			for (int d = 0; d < this.Dimensionality; d++)//all dimensions must overlap, using left-handedl ranges [a, b) = a <= x < b
				if (element.Coordinates[d] < this.LeftCorner[d] || this.RightCorner[d] <= element.Coordinates[d])
					return false;

			return true;//all dimensions overlap (or there are none - should be impossible if derived classes are behaving)
		}

		protected override void AddElementToNode(E element) {
			if (this.NumMembers < this.Capacity)
				this._members[this.NumMembers] = element;
			else if (this.Depth < this.MaxDepth) {
				if (this.NumMembers == this.Capacity) {//one-time quadrant formation
					this._center = this.MakeCenter();
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
			return this._quadrants[Enumerable.Range(0, this.Dimensionality).Sum(d => element.Coordinates[d] >= this._center[d] ? 1 << d : 0)];
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
						LeftCorner = isLeft.Select((l, i) => l ? this.LeftCorner[i] : this._center[i]).ToArray(),
						RightCorner = isLeft.Select((l, i) => l ? this._center[i] : this.RightCorner[i]).ToArray()
				};}).Select(x => this.NewNode(x.LeftCorner, x.RightCorner, (T)this))
				.ToArray();
		}
	}
}