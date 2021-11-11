using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using Generic.Extensions;

namespace Generic.Models {
	public interface ITree : IEnumerable {
		public bool IsRoot { get; }
		public bool IsLeaf { get; }

		public IEnumerable NodeElements { get; }
		public IEnumerable AllElements { get; }
		public IEnumerable<ITree> Children { get; }
		public IEnumerable<ITree> AllNodes { get; }
		public IEnumerable<ITree> SiblingNodes { get; }
		public IEnumerable<ITree> Leaves { get; }

		public void Add(object element);
		public void AddRange(IEnumerable<object> elements);

		public bool DoesContain(object element);
		public ITree GetContainingChild(object element);
		public ITree GetContainingLeaf(object element);
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
		
		public abstract IEnumerable<E> NodeElements { get; }
		IEnumerable ITree.NodeElements => this.NodeElements;
		public IEnumerable<E> AllElements { get {
			if (this.IsLeaf)
				foreach (E e in this.NodeElements)
					yield return e;
			else
				foreach (E e in this.Children.SelectMany(c => c.AllElements))
					yield return e;
		} }
		IEnumerable ITree.AllElements => this.AllElements;
		
		public abstract IEnumerable<ATree<E>> Children { get; }
		IEnumerable<ITree> ITree.Children => this.Children;
		public IEnumerable<ATree<E>> AllNodes { get {
			if (this.IsLeaf)
				yield return this;
			else
				foreach (ATree<E> child in this.Children.SelectMany(c => c.AllNodes))
					yield return child;
		} }
		IEnumerable<ITree> ITree.AllNodes => this.AllNodes;
		public IEnumerable<ATree<E>> SiblingNodes { get {
			if (this.IsRoot)
				return Enumerable.Empty<ATree<E>>();
			else
				return this.Parent.Children.Except(n => ReferenceEquals(this, n));
		} }
		IEnumerable<ITree> ITree.SiblingNodes => this.SiblingNodes;
		public IEnumerable<ATree<E>> Leaves { get {
			if (this.IsLeaf)
				yield return this;
			else
				foreach (ATree<E> child in this.Children.SelectMany(c => c.Leaves))
					yield return child;
		} }
		IEnumerable<ITree> ITree.Leaves => this.Leaves;

		public void Add(E element) {
			this.Incorporate(element);
			if (this.IsLeaf)
				this.AddElementToNode(element);
			else
				this.GetContainingChild(element).Add(element);
		}
		public void Add(object element) { this.Add((E)element); }

		public void AddRange(IEnumerable<E> elements) {
			foreach (E e in elements)
				this.Add(e);
		}
		public void AddRange(IEnumerable<object> elements) { this.AddRange(elements.Cast<E>()); }

		protected virtual void Incorporate(E element) { }
		protected abstract void AddElementToNode(E element);

		public abstract bool DoesContain(E element);
		public bool DoesContain(object element) { return this.DoesContain((E)element); }
		public virtual ATree<E> GetContainingChild(E element) {
			return this.Children.First(c => c.Contains(element));
			throw new Exception("Element is not contained");
		}
		public ITree GetContainingChild(object element) { return this.GetContainingChild((E)element); }
		public ATree<E> GetContainingLeaf(E element) {
			foreach (ATree<E> node in this.Children)
				if (node.Contains(element))
					return node.GetContainingLeaf(element);
			return this;
		}
		public ITree GetContainingLeaf(object element) { return this.GetContainingLeaf((E)element); }

		public IEnumerable<E> GetNeighbors() {
			foreach (E e in this.AllElements)
				yield return e;
			if (!this.IsRoot)
				foreach (E member in this.SeekUpward().SelectMany(n => n.AllElements))
					yield return member;
		}
		public IEnumerable<ATree<E>> GetRefinedNeighborNodes(int? depthLimit = null) {
			yield return this;

			if (!depthLimit.HasValue || depthLimit.Value > 0 && !this.IsLeaf)
				foreach (ATree<E> n in this.Children.SelectMany(q => q.GetRefinedNeighborNodes(depthLimit - 1)))
					yield return n;

			if (this.IsRoot)
				yield break;
			else
				foreach (ATree<E> n in this.SiblingNodes.SelectMany(q => q.GetChildren(depthLimit - 1)))
					yield return n;
		}
		private IEnumerable<ATree<E>> GetChildren(int? depthLimit = null) {
			if (!depthLimit.HasValue || depthLimit > 0)
				foreach (ATree<E> node2 in this.Children.SelectMany(c => c.GetChildren(depthLimit - 1)))
					yield return node2;
			else
				foreach (ATree<E> node in this.Children)
					yield return node;
		}

		private IEnumerable<ATree<E>> SeekUpward() {
			foreach (ATree<E> node in this.SiblingNodes.SelectMany(q => q.AllNodes))
				yield return node;
			if (!this.Parent.IsRoot)
				foreach (ATree<E> node in this.Parent.SeekUpward())
					yield return node;
		}

		public IEnumerator<E> GetEnumerator() { return this.AllElements.GetEnumerator(); }
		IEnumerator IEnumerable.GetEnumerator() { return this.AllElements.GetEnumerator(); }
		public override string ToString() { return string.Format("{0}[Depth {1}]", nameof(ATree<E>), this.Depth); }
	}
}