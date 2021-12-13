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

		public bool DoesContain(object element);
	}

	public abstract class ATree<E> : ITree, IEnumerable<E> {
		public int Depth { get; private set; }
		public ATree<E> Parent { get; private set; }

		public ATree() { }
		public ATree(ATree<E> parent) {
			this.Depth = parent is null ? 0 : parent.Depth + 1;
			this.Parent = parent;
		}

		public bool IsRoot { get { return this.Depth == 0; } }
		public virtual bool IsLeaf { get { return this.Children.None(); } }
		public int NumMembers { get; private set; }
		
		public abstract IEnumerable<E> NodeElements { get; }
		IEnumerable ITree.NodeElements => this.NodeElements;
		public IEnumerable<E> AllElements { get {
			if (this.IsLeaf) foreach (E e in this.NodeElements)
				yield return e;
			else foreach (E e in this.Children.SelectMany(c => c.AllElements))
				yield return e;
		} }
		IEnumerable ITree.AllElements => this.AllElements;
		
		public abstract IEnumerable<ATree<E>> Children { get; }
		IEnumerable<ITree> ITree.Children => this.Children;
		public IEnumerable<ATree<E>> NestedChildren { get {
			foreach (ATree<E> node in this.Children) {
				yield return node;
				foreach (ATree<E> subnode in node.NestedChildren)
					yield return subnode;
		} } }
		IEnumerable<ITree> ITree.NestedChildren { get { return this.NestedChildren; } }
		public IEnumerable<ATree<E>> AllNodes { get {
			if (this.IsLeaf) yield return this;
			else foreach (ATree<E> child in this.Children.SelectMany(c => c.AllNodes))
				yield return child;
		} }
		IEnumerable<ITree> ITree.AllNodes => this.AllNodes;
		public IEnumerable<ATree<E>> SiblingNodes { get {
			if (this.IsRoot) return Enumerable.Empty<ATree<E>>();
			else return this.Parent.Children.Without(n => ReferenceEquals(this, n));
		} }
		IEnumerable<ITree> ITree.SiblingNodes => this.SiblingNodes;
		public IEnumerable<ATree<E>> Leaves { get {
			if (this.IsLeaf) yield return this;
			else foreach (ATree<E> child in this.Children.SelectMany(c => c.Leaves))
				yield return child;
		} }
		IEnumerable<ITree> ITree.Leaves => this.Leaves;

		public void Add(E element) {
			this.Incorporate(element);
			if (this.IsLeaf) this.AddElementToNode(element);
			else this.GetContainingChild(element).Add(element);

			this.NumMembers++;
		}
		void ITree.Add(object element) { this.Add((E)element); }

		public void AddRange(IEnumerable<E> elements) {
			foreach (E e in elements)
				this.Add(e);
		}
		void ITree.AddRange(IEnumerable<object> elements) { this.AddRange(elements.Cast<E>()); }

		protected virtual void Incorporate(E element) { }
		protected abstract void AddElementToNode(E element);

		public abstract bool DoesContain(E element);
		public bool DoesContain(object element) { return this.DoesContain((E)element); }
		protected virtual ATree<E> GetContainingChild(E element) {
			return this.Children.First(c => c.Contains(element));
			throw new Exception("Element is not contained");
		}
		protected ATree<E> GetContainingLeaf(E element) {
			foreach (ATree<E> node in this.Children)
				if (node.Contains(element))
					return node.GetContainingLeaf(element);
			return this;
		}

		public IEnumerable<E> GetNeighbors() {
			foreach (E e in this.AllElements)
				yield return e;
			if (!this.IsRoot)
				foreach (E member in this.SeekUpward().SelectMany(n => n.AllElements))
					yield return member;
		}
		private IEnumerable<ATree<E>> SeekUpward() {
			foreach (ATree<E> node in this.SiblingNodes.SelectMany(q => q.AllNodes).Where(n => n.NumMembers > 0))
				yield return node;
			if (!this.Parent.IsRoot)
				foreach (ATree<E> node in this.Parent.SeekUpward())
					yield return node;
		}
		
		public IEnumerable<ATree<E>> GetNeighborhoodNodes(int? limit = null) {//must be used from a leaf
			return this.GetNeighborhoodNodes_up(limit, this);
		}
		private IEnumerable<ATree<E>> GetNeighborhoodNodes_up(int? limit, ATree<E> start) {
			if (!this.IsRoot) {
				foreach (ATree<E> node in this.SiblingNodes.SelectMany(s => s.GetNeighborhoodNodes_down(limit, start)))
					yield return node;
				foreach (ATree<E> node in this.Parent.GetNeighborhoodNodes_up(limit - 1, start))
					yield return node;
			}
		}
		private IEnumerable<ATree<E>> GetNeighborhoodNodes_down(int? limit, ATree<E> start) {
			if (this.NumMembers == 0)
				yield break;
			else if (!this.IsLeaf && (limit ?? 1) > 0)
				foreach (ATree<E> node in this.Children.SelectMany(c => c.GetNeighborhoodNodes_down(limit - 1, start)))
					yield return node;
			else yield return this;
		}

		public Tuple<ATree<E>[], ATree<E>[]> RecursiveFilter(Predicate<ATree<E>> predicate) {
			if (this.NumMembers == 0) {
				return new(Array.Empty<ATree<E>>(), Array.Empty<ATree<E>>());
			} else if (this.IsLeaf) {
				if (predicate(this))
					return new(new ATree<E>[] { this }, Array.Empty<ATree<E>>());
				else return new(Array.Empty<ATree<E>>(), new ATree<E>[] { this });
			} else {
				Tuple<bool, ATree<E>>[] tests = this.Children.Where(c => c.NumMembers > 0).Select(c => new Tuple<bool, ATree<E>>(predicate(c), c)).ToArray();
				Tuple<ATree<E>[], ATree<E>[]> moreFiltered;
				Tuple<IEnumerable<ATree<E>>, IEnumerable<ATree<E>>> newJunk =
					tests.Where(test => test.Item1)
						.Select(test => test.Item2)
						.Aggregate(
							new Tuple<IEnumerable<ATree<E>>, IEnumerable<ATree<E>>>(Enumerable.Empty<ATree<E>>(), Enumerable.Empty<ATree<E>>()), (agg, node) => {
								moreFiltered = node.RecursiveFilter(predicate);
								return new(agg.Item1.Concat(moreFiltered.Item1),
									agg.Item2.Concat(moreFiltered.Item2));
							});
				return new(
					newJunk.Item1.ToArray(),
					newJunk.Item2.Concat(tests.Without(test => test.Item1).Select(test => test.Item2)).ToArray());
			}
		}

		public IEnumerator<E> GetEnumerator() { return this.AllElements.GetEnumerator(); }
		IEnumerator IEnumerable.GetEnumerator() { return this.AllElements.GetEnumerator(); }
		public override string ToString() { return string.Format("{0}[Depth {1}]", nameof(ATree<E>), this.Depth); }
	}
}