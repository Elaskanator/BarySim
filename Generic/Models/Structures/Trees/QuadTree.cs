using System;
using System.Collections.Generic;
using System.Linq;
using Generic.Extensions;
using Generic.Vectors;

namespace Generic.Models {
	public class QuadTree<TElement> : ATree<TElement>
	where TElement : VectorDouble {//supports any dimensionality
		public QuadTree(double[] corner1, double[] corner2, QuadTree<TElement> parent = null) 
		: base(parent) {//caller needs to ensure all values in x1 are smaller than x2 (the corners of a cubic volume)
			this.CornerLeft = corner1;
			this.CornerRight = corner2;

			this.Dimensionality = corner1.Length;
			this.Center = corner1.Zip(corner2, (l, r) => (l + r) / 2d).ToArray();

			this._members = new TElement[this.Capacity];
		}
		protected virtual QuadTree<TElement> NewNode(double[] cornerA, double[] cornerB, QuadTree<TElement> parent) { return new(cornerA, cornerB, parent); }

		public override string ToString() {
			return string.Format("Node[<{0}> thru <{1}>][{2} members]",
				string.Join(", ", this.CornerLeft),
				string.Join(", ", this.CornerRight),
				this.NumMembers);
		}

		public virtual int Capacity => 1;
		//dividing by 2 enough times WILL reach the sig figs limit of System.Double and cause zero-sized subtrees (and that's before reaching the stack frame depth limit due to recursion)
		public virtual int MaxDepth => 40;
		
		public readonly double[] CornerLeft;
		public readonly double[] CornerRight;
		public readonly double[] Center;
		public readonly int Dimensionality;
		
		public override IEnumerable<QuadTree<TElement>> Children { get { return this._quadrants; } }
		public override bool IsLeaf { get { return this._quadrants.Length == 0; } }
		public override IEnumerable<TElement> NodeElements { get {
			if (this.NumMembers <= this.Capacity)
				return this._members.Take(this.NumMembers);
			else if (this.Depth < this.MaxDepth)
				return Enumerable.Empty<TElement>();
			else return this._members.Concat(this._leftovers);
		} }

		protected QuadTree<TElement>[] _quadrants = Array.Empty<QuadTree<TElement>>();
		protected readonly TElement[] _members;//entries in non-leaves are leftovers that must be ignored
		private List<TElement> _leftovers;

		protected QuadTree<TElement> GetContainingChild_Checked(TElement element) {
			return this.Children.First(c => c.Contains(element));
			throw new Exception("Element is not contained");
		}
		protected virtual QuadTree<TElement> GetContainingChild(TElement element) {
			return this._quadrants[
				Enumerable.Range(0, this.Dimensionality).Sum(d =>
					element.Coordinates[d] >= this.Center[d] ? 1 << d : 0)];
		}
		//protected virtual void ShuffleChildren(Random rand = null) {
		//	(rand ?? new Random()).ShuffleInPlace(this._quadrants);
		//}
		//protected QuadTree<E> GetContainingLeaf(E element) {
		//	foreach (QuadTree<E> node in this.Children)
		//		if (node.Contains(element))
		//			return node.GetContainingLeaf(element);
		//	return this;
		//}

		public override void Add(TElement element) {
			this.Incorporate(element);
			if (this.IsLeaf) this.AddElementToNode(element);
			else this.GetContainingChild(element).Add(element);

			this.NumMembers++;
		}

		protected virtual void Incorporate(TElement element) { }

		public virtual bool DoesContain(TElement element) {
			for (int d = 0; d < this.Dimensionality; d++)//all dimensions must overlap, using left-handedl ranges [a, b) = a <= x < b
				if (element.Coordinates[d] < this.CornerLeft[d] || this.CornerRight[d] <= element.Coordinates[d])
					return false;

			return true;//all dimensions overlap (or there are none - should be impossible if derived classes are behaving)
		}

		protected virtual void AddElementToNode(TElement element) {
			if (this.NumMembers < this.Capacity)
				this._members[this.NumMembers] = element;
			else if (this.Depth < this.MaxDepth) {
				if (this.NumMembers == this.Capacity) {//one-time quadrant formation
					this._quadrants = this
						.FormNodeCorners()
						.Select(c => this.NewNode(c.Item1, c.Item2, this))
						.ToArray();
					//this.ArrangeNodes();
					foreach (TElement member in this._members)
						this.GetContainingChild(member).Add(member);
				}
				this.GetContainingChild(element).Add(element);
			} else (this._leftovers ??= new List<TElement>()).Add(element);
		}

		//protected virtual void ArrangeNodes() { }//MUST override GetContainingChild if rearranging nodes

		protected virtual IEnumerable<Tuple<double[], double[]>> FormNodeCorners() {
			double[] center = this.ChooseCenter();
			return Enumerable
				.Range(0, 1 << this.Dimensionality)//the 2^dimension "quadrants" of the Euclidean hyperplane
				.Select(q => {
					bool[] isLeft = Enumerable
						.Range(0, this.Dimensionality)
						.Select(d => (q & (1 << d)) > 0)
						.ToArray();
					return new Tuple<double[], double[]>(
						isLeft.Select((l, i) => l ? this.CornerLeft[i] : center[i]).ToArray(),
						isLeft.Select((l, i) => l ? center[i] : this.CornerRight[i]).ToArray());
				}).ToArray();
		}

		protected virtual double[] ChooseCenter() {
			return this.Center;
		}
		protected double[] ChooseRandomCenter(Random rand = null) {
			return this.CornerLeft.Zip(this.CornerRight, (a, b) => a + (rand ?? new Random()).NextDouble() * (b - a)).ToArray();
		}
		protected double[] ChooseClusterCenter() {
			double minDivision = 1d / (1 << (this.Capacity / 2));
			return Enumerable
				.Range(0, this.Dimensionality)
				.Select(d => this._members.Average(m => m.Coordinates[d]))
				.Select((avg, d) =>
					avg < this.CornerLeft[d] + minDivision * (this.CornerRight[d] - this.CornerLeft[d])
						? this.CornerLeft[d] + minDivision * (this.CornerRight[d] - this.CornerLeft[d])
						: avg > this.CornerRight[d] - minDivision * (this.CornerRight[d] - this.CornerLeft[d])
							? this.CornerRight[d] - minDivision * (this.CornerRight[d] - this.CornerLeft[d])
							: avg)
				.ToArray();
		}
	}
}
