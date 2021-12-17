using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Generic.Extensions;

namespace Generic.Models {
	public interface ITree : IEnumerable {
		public bool IsRoot { get; }
		public bool IsLeaf { get; }
		public int ElementCount { get; }

		public IEnumerable<ITree> Children { get; }
		public IEnumerable NodeElements { get; }

		public IEnumerable AllElements { get; }
		public IEnumerable<ITree> AllNodes { get; }
		public IEnumerable<ITree> Leaves { get; }
		public IEnumerable<ITree> LeavesNonempty { get; }

		public IEnumerable<ITree> NestedChildren { get; }
		public IEnumerable<ITree> SiblingNodes { get; }

		public ITree Add(object element);
		public void AddRange(IEnumerable<object> elements);
	}

	public abstract class ATree<TElement, TSelf> : ITree, IEnumerable<TElement>
	where TSelf : ATree<TElement, TSelf> {
		public int Depth { get; protected set; }
		public TSelf Parent { get; protected set; }

		public ATree(TSelf parent = null) {
			this.Parent = parent;
			this.Depth = parent is null ? 0 : parent.Depth + 1;
		}

		public bool IsRoot { get { return this.Depth == 0; } }
		public virtual bool IsLeaf { get { return this.Children.None(); } }
		public int ElementCount { get; protected set; }
		
		public abstract IEnumerable<TElement> NodeElements { get; }
		IEnumerable ITree.NodeElements => this.NodeElements;

		public IEnumerable<TElement> AllElements { get {
			if (this.ElementCount > 0) 
				if (this.IsLeaf) foreach (TElement e in this.NodeElements)
					yield return e;
				else foreach (TElement e in this.Children.SelectMany(c => c.AllElements))
					yield return e;
		} }
		IEnumerable ITree.AllElements => this.AllElements;
		
		public abstract IEnumerable<TSelf> Children { get; }
		IEnumerable<ITree> ITree.Children => this.Children;
		public IEnumerable<TSelf> NestedChildren { get {
			foreach (TSelf node in this.Children) {
				yield return node;
				foreach (TSelf subnode in node.NestedChildren)
					yield return subnode;
		} } }
		IEnumerable<ITree> ITree.NestedChildren { get { return this.NestedChildren; } }

		public IEnumerable<TSelf> AllNodes { get {
			if (this.IsLeaf) yield return (TSelf)this;
			else foreach (TSelf child in this.Children.SelectMany(c => c.AllNodes))
				yield return child;
		} }
		IEnumerable<ITree> ITree.AllNodes => this.AllNodes;

		public IEnumerable<TSelf> SiblingNodes { get {
			if (this.IsRoot) return Enumerable.Empty<TSelf>();
			else return this.Parent.Children.Without(n => ReferenceEquals(this, n));
		} }
		IEnumerable<ITree> ITree.SiblingNodes => this.SiblingNodes;

		public IEnumerable<TSelf> Leaves { get {
			if (this.IsLeaf) yield return (TSelf)this;
			else foreach (TSelf child in this.Children.SelectMany(c => c.Leaves))
				yield return child;
		} }
		IEnumerable<ITree> ITree.Leaves => this.Leaves;
		
		public IEnumerable<TSelf> LeavesNonempty { get {
			if (this.ElementCount > 0)
				if (this.IsLeaf) yield return (TSelf)this;
				else foreach (TSelf child in this.Children.SelectMany(c => c.LeavesNonempty))
					yield return child;
		} }
		IEnumerable<ITree> ITree.LeavesNonempty => this.LeavesNonempty;

		public abstract TSelf Add(TElement element);
		ITree ITree.Add(object element) { return this.Add((TElement)element); }

		public void AddRange(IEnumerable<TElement> elements) {
			foreach (TElement e in elements)
				this.Add(e);
		}
		void ITree.AddRange(IEnumerable<object> elements) { this.AddRange(elements.Cast<TElement>()); }

		public IEnumerable<TElement> GetNeighbors() {
			foreach (TElement e in this.AllElements)
				yield return e;
			if (!this.IsRoot)
				foreach (TElement member in this.SeekUpward().SelectMany(n => n.AllElements))
					yield return member;
		}
		private IEnumerable<TSelf> SeekUpward() {
			foreach (TSelf node in this.SiblingNodes.SelectMany(q => q.AllNodes).Where(n => n.ElementCount > 0))
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
			if (this.ElementCount == 0)
				yield break;
			else if (!this.IsLeaf && (limit ?? 1) > 0)
				foreach (TSelf node in this.Children.SelectMany(c => c.GetNeighborhoodNodes_down(limit - 1, start)))
					yield return node;
			else yield return (TSelf)this;
		}

		public Tuple<TSelf[], TSelf[]> RecursiveFilter(Predicate<TSelf> predicate) {
			if (this.ElementCount == 0) {
				return new(Array.Empty<TSelf>(), Array.Empty<TSelf>());
			} else if (this.IsLeaf) {
				if (predicate((TSelf)this))
					return new(new TSelf[] { (TSelf)this }, Array.Empty<TSelf>());
				else return new(Array.Empty<TSelf>(), new TSelf[] { (TSelf)this });
			} else {
				Tuple<bool, TSelf>[] tests = this.Children.Where(c => c.ElementCount > 0).Select(c => new Tuple<bool, TSelf>(predicate(c), c)).ToArray();
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
		public override string ToString() { return string.Format("{0}[Depth {1}]", nameof(TSelf), this.Depth); }
	}
}