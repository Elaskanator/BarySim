using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Generic.Extensions;

namespace Generic.Models {
	public interface ITree : IEnumerable {
		bool IsRoot { get; }
		bool IsLeaf { get; }
		int Count { get; }

		IEnumerable AllElements { get; }
		IEnumerable AllNodes { get; }
		IEnumerable Children { get; }

		ITree Parent { get; }

		IEnumerable LeafNodes { get; }
		IEnumerable LeafNodesNonEmpty { get; }

		IEnumerable NestedChildren { get; }
		IEnumerable SiblingNodes { get; }

		ITree Add(object element);
		void AddRange(IEnumerable elements);

		ITree GetContainingLeaf(object element);
		ITree GetContainingLeafUnchecked(object element);

		IEnumerator IEnumerable.GetEnumerator() { return this.AllElements.GetEnumerator(); }
	}

	public interface ITree<TElement, TSelf> : ITree, IEnumerable<TElement>
	where TSelf : ITree<TElement, TSelf> {
		new IEnumerable<TElement> AllElements { get; }
		IEnumerable ITree.AllElements => this.AllElements;

		new IEnumerable<TSelf> AllNodes { get; }
		IEnumerable ITree.AllNodes => this.AllNodes;

		new IEnumerable<TSelf> Children { get; }
		IEnumerable ITree.Children => this.Children;

		new TSelf Parent { get; }
		ITree ITree.Parent => this.Parent;

		new IEnumerable<TSelf> LeafNodes { get; }
		IEnumerable ITree.LeafNodes => this.LeafNodes;
		new IEnumerable<TSelf> LeafNodesNonEmpty { get; }
		IEnumerable ITree.LeafNodesNonEmpty => this.LeafNodesNonEmpty;

		new IEnumerable<TSelf> NestedChildren { get; }
		IEnumerable ITree.NestedChildren => this.NestedChildren;
		new IEnumerable<TSelf> SiblingNodes { get; }
		IEnumerable ITree.SiblingNodes => this.SiblingNodes;

		TSelf Add(TElement element);
		ITree ITree.Add(object element) => this.Add((TElement)element);
		void AddRange(IEnumerable<TElement> elements) { foreach (TElement e in elements) this.Add(e); }
		void ITree.AddRange(IEnumerable elements) => this.AddRange(elements.Cast<TElement>());

		TSelf GetContainingLeaf(TElement element);
		ITree ITree.GetContainingLeaf(object element) => this.GetContainingLeaf((TElement)element);
		TSelf GetContainingLeafUnchecked(TElement element);
		ITree ITree.GetContainingLeafUnchecked(object element) => this.GetContainingLeafUnchecked((TElement)element);

		IEnumerator<TElement> IEnumerable<TElement>.GetEnumerator() => this.AllElements.GetEnumerator();
	}

	public interface IMutableTree<TElement, TSelf> : ITree<TElement, TSelf>
	where TSelf : IMutableTree<TElement, TSelf> {
		bool TryRemove(TElement element);

		TSelf Prune();
	}

	public abstract partial class ATree<TElement, TSelf> : ITree<TElement, TSelf>, IMutableTree<TElement, TSelf>
	where TSelf : ATree<TElement, TSelf> {
		public ATree(TSelf parent = null) {
			this.Parent = parent;
			this.Depth = parent is null ? 0 : parent.Depth + 1;
		}
		public override string ToString() { return string.Format("{0}[Depth {1}]", nameof(TSelf), this.Depth); }

		public virtual int NodeCapacity => 1;
		public virtual bool ResolutionLimitReached => false;
		//TODO - make everything tail-recursive
		public virtual int MaxDepth => 40;

		public int Count { get; protected set; }
		public int Depth { get; protected set; }
		public TSelf Parent { get; protected set; }
		protected ILeafNode<TElement> LeafContainer;
		protected abstract ILeafNode<TElement> NewLeafContainer();

		public bool IsRoot { get { return this.Depth == 0; } }
		public bool IsLeaf { get { return this._children is null; } }

		#region Accessors
		public IEnumerable<TElement> AllElements { get {
			if (this.Count > 0) 
				if (this.IsLeaf) foreach (TElement e in this.LeafContainer.Elements)
					yield return e;
				else foreach (TElement e in this.Children.SelectMany(c => c.AllElements))
					yield return e; }}

		public IEnumerable<TSelf> AllNodes { get {
			if (this.IsLeaf) yield return (TSelf)this;
			else foreach (TSelf child in this.Children.SelectMany(c => c.AllNodes))
				yield return child; }}
		
		protected TSelf[] _children = null;
		public IEnumerable<TSelf> Children { get { return this._children; } }

		public IEnumerable<TSelf> NestedChildren { get {
			foreach (TSelf node in this.Children) {
				yield return node;
				foreach (TSelf subnode in node.NestedChildren)
					yield return subnode; }}}

		public IEnumerable<TSelf> SiblingNodes { get {
			if (this.IsRoot) return Enumerable.Empty<TSelf>();
			else return this.Parent.Children.Without(n => ReferenceEquals(this, n)); }}

		public IEnumerable<TSelf> LeafNodes { get {
			if (this.IsLeaf) yield return (TSelf)this;
			else foreach (TSelf child in this.Children.SelectMany(c => c.LeafNodes))
				yield return child; }}
		
		public IEnumerable<TSelf> LeafNodesNonEmpty { get {
			if (this.Count > 0)
				if (this.IsLeaf) yield return (TSelf)this;
				else foreach (TSelf child in this.Children.SelectMany(c => c.LeafNodesNonEmpty))
					yield return child; }}
		#endregion Accessors

		public abstract bool DoesContain(TElement element);

		protected abstract uint ChooseNodeIdx(TElement element);
		protected abstract IEnumerable<TSelf> FormNodes();
		protected virtual TSelf[] ArrangeChildren(IEnumerable<TSelf> nodes) { return nodes.ToArray(); }
		protected virtual void Incorporate(TElement element) { }

		#region Add
		public TSelf Add(TElement element) {
			this.Incorporate(element);
			return this.AddInternal(element);
		}
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
				(this.LeafContainer ??= this.NewLeafContainer()).Add(element);
				return (TSelf)this;
			} else {//add another layer
				this._children = this.ArrangeChildren(this.FormNodes());
				TElement[] myMembers = this.LeafContainer.Elements.ToArray();
				for (int i = 0; i < myMembers.Length; i++)
					this._children[this.ChooseNodeIdx(myMembers[i])]
						.AddElementToNode(myMembers[i]);
				this.LeafContainer = null;
				return this
					._children[this.ChooseNodeIdx(element)]
					.AddElementToNode(element);
			}
		}

		public void AddRange(IEnumerable<TElement> elements) {
			foreach (TElement e in elements)
				this.Add(e);
		}
		#endregion Add

		#region Remove
		public bool TryRemove(TElement element) {
			if (this.Count > 0) {
				TSelf node = this.GetContainingLeafUnchecked(element);
				if (node.LeafContainer.TryRemove(element)) {
					node.SignalRemoval();
					return true;
				}
			}
			return false;
		}
		protected void SignalRemoval() {
			if (--this.Count == 0) {
				this._children = null;
				this.LeafContainer = null;
			}
			if (!this.IsRoot) this.Parent.SignalRemoval();
		}
		
		public TSelf Prune() {
			TSelf node = (TSelf)this;
			TSelf[] nonEmptyChildren;
			while (!(node.Children is null)) {
				nonEmptyChildren = node.Children.Where(c => c.Count > 0).Take(2).ToArray();
				if (nonEmptyChildren.Length == 1)
					node = nonEmptyChildren[0];
				else break;
			}
			return node;
		}
		#endregion Remove
		
		public TSelf GetContainingLeaf(TElement element) {
			TSelf node = this.GetContainingLeafUnchecked(element);
			if (node.DoesContain(element)) return node;
			else throw new KeyNotFoundException();
		}
		public TSelf GetContainingLeafUnchecked(TElement element) {
			TSelf node = (TSelf)this;
			while (!node.IsLeaf)
				node = this._children[this.ChooseNodeIdx(element)];
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
			foreach (TSelf node in this.SiblingNodes.SelectMany(q => q.AllNodes).Where(n => n.Count > 0))
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
				foreach (TSelf node in this.SiblingNodes.SelectMany(s => s.GetNeighborhoodNodes_down(limit, start)))
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
	}
}