using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Generic.Vectors;

namespace Generic.Models {
	public abstract class AVectorTree<TElement, TSelf> : ATree<TElement, TSelf>
	where TElement : AParticle
	where TSelf : AVectorTree<TElement, TSelf> {//supports any dimensionality
		public AVectorTree(int dim, Vector<float> corner1, Vector<float> corner2, TSelf parent = null) 
		: base(parent) {//caller needs to ensure all values in x1 are smaller than x2 (the corners of a cubic volume)
			this.Dim = dim;
			this.CornerLeft = corner1;
			this.CornerRight = corner2;

			this.Size = corner2 - corner1;

			this._members = new TElement[this.Capacity];
		}
		protected abstract TSelf NewNode(Vector<float> cornerA, Vector<float> cornerB, TSelf parent = null);

		public override string ToString() {
			return string.Format("Node[<{0}> thru <{1}>][{2} members]",
				string.Join(", ", this.CornerLeft),
				string.Join(", ", this.CornerRight),
				this.ElementCount);
		}

		public virtual int Capacity => 1;
		//dividing by 2 enough times WILL reach the sig figs limit of System.Double and cause zero-sized subtrees (and that's before reaching the stack frame depth limit due to recursion)
		public virtual int MaxDepth => 40;
		
		public readonly int Dim;
		public readonly Vector<float> CornerLeft;
		public readonly Vector<float> CornerRight;
		public readonly Vector<float> Size;
		
		public override IEnumerable<TSelf> Children { get { return this._children; } }
		public override bool IsLeaf { get { return this._children is null; } }
		public override IEnumerable<TElement> NodeElements { get {
			if (this.ElementCount <= this.Capacity)
				return this._members.Take(this.ElementCount);
			else if (this.Depth < this.MaxDepth)
				return Enumerable.Empty<TElement>();
			else return this._members.Concat(this._leftovers);
		} }

		protected TSelf[] _children = null;
		protected TElement[] _members = null;//entries are removed from non-leaves
		private List<TElement> _leftovers = null;

		protected abstract IEnumerable<Tuple<Vector<float>, Vector<float>>> FormNewNodeCorners();
		protected virtual void Incorporate(TElement element) { }
		protected virtual void ArrangeChildren() { }

		public override TSelf Add(TElement element) {
			this.Incorporate(element);

			TSelf leaf;
			if (this.IsLeaf)
				leaf = this.AddElementToNode(element);
			else leaf = this._children[this.GetChildIndex(element.Position)].Add(element);

			this.ElementCount++;
			return leaf;
		}

		protected virtual uint GetChildIndex(Vector<float> coordinates) {
			for (int i = 0; i < this._children.Length; i++)
				if (this._children[i].DoesContainCoordinates(coordinates))
					return (uint)i;
			throw new Exception("Element is not contained");
		}


		public bool DoesContainCoordinates(Vector<float> coordinates) {
			for (int d = 0; d < this.Dim; d++)
				if (coordinates[d] < this.CornerLeft[d] || coordinates[d] >= this.CornerRight[d])
					return false;
			return true;
		}

		public TSelf GetContainingLeaf(Vector<float> coordinates) {
			TSelf node = (TSelf)this;
			while (!node.IsLeaf)
				node = this._children[this.GetChildIndex(coordinates)];
			return node;
		}

		protected TSelf AddElementToNode(TElement element) {
			if (this.ElementCount < this.Capacity) {
				this._members[this.ElementCount] = element;
				return (TSelf) this;
			} else if (this.Depth < this.MaxDepth) {
				if (this.ElementCount == this.Capacity) {//one-time quadrant formation
					this._children = this
						.FormNewNodeCorners()
						.Select(c => this.NewNode(c.Item1, c.Item2, (TSelf)this))
						.ToArray();
					this.ArrangeChildren();
					foreach (TElement member in this._members)
						this._children[this.GetChildIndex(member.Position)].Add(member);
					this._members = null;
				}
				return this._children[this.GetChildIndex(element.Position)].AddElementToNode(element);
			} else {
				(this._leftovers ??= new List<TElement>()).Add(element);
				return (TSelf) this;
			}
		}
	}
}