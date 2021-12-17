using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Generic.Extensions;

namespace Generic.Models {
	public interface ITree : IEnumerable {
		public bool IsRoot { get; }
		public bool IsLeaf { get; }
		public int Count { get; }

		public IEnumerable AllElements { get; }
		public IEnumerable<ITree> Children { get; }

		public IEnumerable<ITree> AllNodes { get; }
		public IEnumerable<ITree> Leaves { get; }
		public IEnumerable<ITree> LeavesNonempty { get; }

		public IEnumerable<ITree> NestedChildren { get; }
		public IEnumerable<ITree> SiblingNodes { get; }

		public ITree Add(object element);
		public void AddRange(IEnumerable<object> elements);

		public void Remove(object element);
	}

	public abstract partial class ATree<TElement, TSelf> : ITree, IEnumerable<TElement>
	where TElement : IEquatable<TElement>, IEqualityComparer<TElement>
	where TSelf : ATree<TElement, TSelf> {
		public ATree(TSelf parent = null) {
			this.Parent = parent;
			this.Depth = parent is null ? 0 : parent.Depth + 1;
			this._leafNode = new(this.NodeCapacity);
		}
		public override string ToString() { return string.Format("{0}[Depth {1}]", nameof(TSelf), this.Depth); }

		public virtual int NodeCapacity => 1;
		public virtual bool ResolutionLimitReached => false;
		//TODO - make everything tail-recursive
		public virtual int MaxDepth => 40;

		public int Count { get; private set; }
		public int Depth { get; protected set; }
		public TSelf Parent { get; protected set; }

		public bool IsRoot { get { return this.Depth == 0; } }
		public bool IsLeaf { get { return this._children is null; } }

		#region Accessors
		public IEnumerable<TElement> AllElements { get {
			if (this.Count > 0) 
				if (this.IsLeaf) foreach (TElement e in this._leafNode.Elements)
					yield return e;
				else foreach (TElement e in this.Children.SelectMany(c => c.AllElements))
					yield return e; }}
		IEnumerable ITree.AllElements => this.AllElements;

		public IEnumerable<ITree> AllNodes { get {
			if (this.IsLeaf) yield return (TSelf)this;
			else foreach (TSelf child in this.Children.SelectMany(c => c.AllNodes))
				yield return child; }}
		IEnumerable<ITree> ITree.AllNodes => this.AllNodes;
		
		protected TSelf[] _children = null;
		public IEnumerable<TSelf> Children { get { return this._children; } }
		IEnumerable<ITree> ITree.Children => this.Children;

		public IEnumerable<TSelf> NestedChildren { get {
			foreach (TSelf node in this.Children) {
				yield return node;
				foreach (TSelf subnode in node.NestedChildren)
					yield return subnode; }}}
		IEnumerable<ITree> ITree.NestedChildren { get { return this.NestedChildren; } }

		public IEnumerable<TSelf> Siblings { get {
			if (this.IsRoot) return Enumerable.Empty<TSelf>();
			else return this.Parent.Children.Without(n => ReferenceEquals(this, n)); }}
		IEnumerable<ITree> ITree.SiblingNodes => this.Siblings;

		public IEnumerable<ITree> Leaves { get {
			if (this.IsLeaf) yield return (TSelf)this;
			else foreach (TSelf child in this.Children.SelectMany(c => c.Leaves))
				yield return child; }}
		IEnumerable<ITree> ITree.Leaves => this.Leaves;
		
		public IEnumerable<TSelf> LeavesNonempty { get {
			if (this.Count > 0)
				if (this.IsLeaf) yield return (TSelf)this;
				else foreach (TSelf child in this.Children.SelectMany(c => c.LeavesNonempty))
					yield return child; }}
		IEnumerable<ITree> ITree.LeavesNonempty => this.LeavesNonempty;
		#endregion Accessors
		
		public abstract bool DoesContain(TElement element);

		protected abstract uint ChooseNodeIdx(TElement element);
		protected abstract IEnumerable<TSelf> FormNodes();
		protected virtual TSelf[] ArrangeChildren(IEnumerable<TSelf> nodes) { return nodes.ToArray(); }
		protected virtual void Incorporate(TElement element) { }

		#region Add/Remove
		private LeafNode _leafNode;

		public TSelf Add(TElement element) {
			this.Incorporate(element);
			return this.AddInternal(element);
		}
		ITree ITree.Add(object element) { return this.Add((TElement)element); }
		private TSelf AddInternal(TElement element) {
			TSelf node = (TSelf)this;
			while (!node.IsLeaf) {
				node.Count++;
				node = node._children[this.ChooseNodeIdx(element)];
			}
			return node.AddElementToNode(element);
		}
		private TSelf AddElementToNode(TElement element) {
			this.Count++;
			if (this.Count <= this.NodeCapacity || this.ResolutionLimitReached || this.Depth >= this.MaxDepth) {
				this._leafNode.Add(element);
				return (TSelf)this;
			} else {//add another layer
				this._children = this.ArrangeChildren(this.FormNodes());
				for (int i = 0; i < this.NodeCapacity; i++)
					this._children[this.ChooseNodeIdx(this._leafNode._members[i])]
						.AddElementToNode(this._leafNode._members[i]);
				this._leafNode = null;
				return this
					._children[this.ChooseNodeIdx(element)]
					.AddElementToNode(element);
			}
		}

		public void AddRange(IEnumerable<TElement> elements) {
			foreach (TElement e in elements)
				this.Add(e);
		}
		void ITree.AddRange(IEnumerable<object> elements) { this.AddRange(elements.Cast<TElement>()); }

		public void Remove(TElement element) {
			this._leafNode.Remove(element);
			if (this.Count == 0 && !this.IsRoot)
				this.Parent.SignalRemoval(true);
		}
		public void Remove(object element) { this.Remove((TElement)element); }
		private bool SignalRemoval(bool lowerEmptied) {
			this.Count--;
			if (lowerEmptied && this.Count == 0) {
				this._children = null;
				if (this.IsRoot || !this.Parent.SignalRemoval(true))
					this._leafNode = new(this.NodeCapacity);
				return true;
			} else if (!this.IsRoot)
				this.Parent.SignalRemoval(false);
			return false;
		}
		#endregion Add/Remove

		public TSelf GetContainingLeaf(TElement coordinates) {
			TSelf node = (TSelf)this;
			while (!node.IsLeaf)
				node = this._children[this.ChooseNodeIdx(coordinates)];
			return node;
		}

		public IEnumerable<TElement> GetNeighbors() {
			foreach (TElement e in this.AllElements)
				yield return e;
			if (!this.IsRoot)
				foreach (TElement member in this.SeekUpward().SelectMany(n => n.AllElements))
					yield return member;
		}
		private IEnumerable<TSelf> SeekUpward() {
			foreach (TSelf node in this.Siblings.SelectMany(q => q.AllNodes).Where(n => n.Count > 0))
				yield return node;
			if (!this.Parent.IsRoot)
				foreach (TSelf node in this.Parent.SeekUpward())
					yield return node;
		}
		
		public IEnumerable<TSelf> GetNeighborhoodNodes(int? limit = null) {//must be used from a leaf
			return this.GetNeighborhoodNodes_up(limit, (TSelf)this);
		}
		private IEnumerable<TSelf> GetNeighborhoodNodes_up(int? limit, TSelf start) {
			if (!this.IsRoot) {
				foreach (TSelf node in this.Siblings.SelectMany(s => s.GetNeighborhoodNodes_down(limit, start)))
					yield return node;
				foreach (TSelf node in this.Parent.GetNeighborhoodNodes_up(limit - 1, start))
					yield return node;
			}
		}
		private IEnumerable<TSelf> GetNeighborhoodNodes_down(int? limit, TSelf start) {
			if (this.Count == 0)
				yield break;
			else if (!this.IsLeaf && (limit ?? 1) > 0)
				foreach (TSelf node in this.Children.SelectMany(c => c.GetNeighborhoodNodes_down(limit - 1, start)))
					yield return node;
			else yield return (TSelf)this;
		}

		public Tuple<TSelf[], TSelf[]> RecursiveFilter(Predicate<TSelf> predicate) {
			if (this.Count == 0) {
				return new(Array.Empty<TSelf>(), Array.Empty<TSelf>());
			} else if (this.IsLeaf) {
				if (predicate((TSelf)this))
					return new(new TSelf[] { (TSelf)this }, Array.Empty<TSelf>());
				else return new(Array.Empty<TSelf>(), new TSelf[] { (TSelf)this });
			} else {
				Tuple<bool, TSelf>[] tests = this.Children.Where(c => c.Count > 0).Select(c => new Tuple<bool, TSelf>(predicate(c), c)).ToArray();
				Tuple<TSelf[], TSelf[]> moreFiltered;
				Tuple<IEnumerable<TSelf>, IEnumerable<TSelf>> newJunk =
					tests.Where(test => test.Item1)
						.Select(test => test.Item2)
						.Aggregate(
							new Tuple<IEnumerable<TSelf>, IEnumerable<TSelf>>(Enumerable.Empty<TSelf>(), Enumerable.Empty<TSelf>()), (agg, node) => {
								moreFiltered = node.RecursiveFilter(predicate);
								return new(agg.Item1.Concat(moreFiltered.Item1),
									agg.Item2.Concat(moreFiltered.Item2));
							});
				return new(
					newJunk.Item1.ToArray(),
					newJunk.Item2.Concat(tests.Without(test => test.Item1).Select(test => test.Item2)).ToArray());
			}
		}

		public IEnumerator<TElement> GetEnumerator() { return this.AllElements.GetEnumerator(); }
		IEnumerator IEnumerable.GetEnumerator() { return this.AllElements.GetEnumerator(); }
	}
}