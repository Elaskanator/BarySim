using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Generic.Extensions;

namespace Generic.Models {
	public interface ITree : IEnumerable {
		public bool IsRoot { get; }
		public bool IsLeaf { get; }
		public int NumMembers { get; }

		public IEnumerable<ITree> Children { get; }
		public IEnumerable NodeElements { get; }

		public IEnumerable AllElements { get; }
		public IEnumerable<ITree> AllNodes { get; }
		public IEnumerable<ITree> Leaves { get; }

		public IEnumerable<ITree> NestedChildren { get; }
		public IEnumerable<ITree> SiblingNodes { get; }

		public void Add(object element);
		public void AddRange(IEnumerable<object> elements);
	}

	public abstract class ATree<TElement> : ITree, IEnumerable<TElement> {
		public int Depth { get; private set; }
		public ATree<TElement> Parent { get; private set; }

		public ATree(ATree<TElement> parent = null) {
			this.Parent = parent;
			this.Depth = parent is null ? 0 : parent.Depth + 1;
		}

		public bool IsRoot { get { return this.Depth == 0; } }
		public virtual bool IsLeaf { get { return this.Children.None(); } }
		public int NumMembers { get; protected set; }
		
		public abstract IEnumerable<TElement> NodeElements { get; }
		IEnumerable ITree.NodeElements => this.NodeElements;
		public IEnumerable<TElement> AllElements { get {
			if (this.IsLeaf) foreach (TElement e in this.NodeElements)
				yield return e;
			else foreach (TElement e in this.Children.SelectMany(c => c.AllElements))
				yield return e;
		} }
		IEnumerable ITree.AllElements => this.AllElements;
		
		public abstract IEnumerable<ATree<TElement>> Children { get; }
		IEnumerable<ITree> ITree.Children => this.Children;
		public IEnumerable<ATree<TElement>> NestedChildren { get {
			foreach (ATree<TElement> node in this.Children) {
				yield return node;
				foreach (ATree<TElement> subnode in node.NestedChildren)
					yield return subnode;
		} } }
		IEnumerable<ITree> ITree.NestedChildren { get { return this.NestedChildren; } }
		public IEnumerable<ATree<TElement>> AllNodes { get {
			if (this.IsLeaf) yield return this;
			else foreach (ATree<TElement> child in this.Children.SelectMany(c => c.AllNodes))
				yield return child;
		} }
		IEnumerable<ITree> ITree.AllNodes => this.AllNodes;
		public IEnumerable<ATree<TElement>> SiblingNodes { get {
			if (this.IsRoot) return Enumerable.Empty<ATree<TElement>>();
			else return this.Parent.Children.Without(n => ReferenceEquals(this, n));
		} }
		IEnumerable<ITree> ITree.SiblingNodes => this.SiblingNodes;
		public IEnumerable<ATree<TElement>> Leaves { get {
			if (this.IsLeaf) yield return this;
			else foreach (ATree<TElement> child in this.Children.SelectMany(c => c.Leaves))
				yield return child;
		} }
		IEnumerable<ITree> ITree.Leaves => this.Leaves;

		public abstract void Add(TElement element);
		void ITree.Add(object element) { this.Add((TElement)element); }

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
		private IEnumerable<ATree<TElement>> SeekUpward() {
			foreach (ATree<TElement> node in this.SiblingNodes.SelectMany(q => q.AllNodes).Where(n => n.NumMembers > 0))
				yield return node;
			if (!this.Parent.IsRoot)
				foreach (ATree<TElement> node in this.Parent.SeekUpward())
					yield return node;
		}
		
		public IEnumerable<ATree<TElement>> GetNeighborhoodNodes(int? limit = null) {//must be used from a leaf
			return this.GetNeighborhoodNodes_up(limit, this);
		}
		private IEnumerable<ATree<TElement>> GetNeighborhoodNodes_up(int? limit, ATree<TElement> start) {
			if (!this.IsRoot) {
				foreach (ATree<TElement> node in this.SiblingNodes.SelectMany(s => s.GetNeighborhoodNodes_down(limit, start)))
					yield return node;
				foreach (ATree<TElement> node in this.Parent.GetNeighborhoodNodes_up(limit - 1, start))
					yield return node;
			}
		}
		private IEnumerable<ATree<TElement>> GetNeighborhoodNodes_down(int? limit, ATree<TElement> start) {
			if (this.NumMembers == 0)
				yield break;
			else if (!this.IsLeaf && (limit ?? 1) > 0)
				foreach (ATree<TElement> node in this.Children.SelectMany(c => c.GetNeighborhoodNodes_down(limit - 1, start)))
					yield return node;
			else yield return this;
		}

		public Tuple<ATree<TElement>[], ATree<TElement>[]> RecursiveFilter(Predicate<ATree<TElement>> predicate) {
			if (this.NumMembers == 0) {
				return new(Array.Empty<ATree<TElement>>(), Array.Empty<ATree<TElement>>());
			} else if (this.IsLeaf) {
				if (predicate(this))
					return new(new ATree<TElement>[] { this }, Array.Empty<ATree<TElement>>());
				else return new(Array.Empty<ATree<TElement>>(), new ATree<TElement>[] { this });
			} else {
				Tuple<bool, ATree<TElement>>[] tests = this.Children.Where(c => c.NumMembers > 0).Select(c => new Tuple<bool, ATree<TElement>>(predicate(c), c)).ToArray();
				Tuple<ATree<TElement>[], ATree<TElement>[]> moreFiltered;
				Tuple<IEnumerable<ATree<TElement>>, IEnumerable<ATree<TElement>>> newJunk =
					tests.Where(test => test.Item1)
						.Select(test => test.Item2)
						.Aggregate(
							new Tuple<IEnumerable<ATree<TElement>>, IEnumerable<ATree<TElement>>>(Enumerable.Empty<ATree<TElement>>(), Enumerable.Empty<ATree<TElement>>()), (agg, node) => {
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
		public override string ToString() { return string.Format("{0}[Depth {1}]", nameof(ATree<TElement>), this.Depth); }
	}
}