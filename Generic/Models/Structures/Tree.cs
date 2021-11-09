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

	public abstract class ATree<T> : ITree, IEnumerable<T> {
		public int Depth { get; private set; }
		public ATree<T> Parent { get; private set; }

		public ATree() { }
		public ATree(ATree<T> parent) {
			this.Depth = parent is null ? 0 : parent.Depth + 1;
			this.Parent = parent;
		}

		public bool IsRoot { get { return this.Depth == 0; } }
		public virtual bool IsLeaf { get { return this.Children.None(); } }
		
		public abstract IEnumerable<T> NodeElements { get; }
		IEnumerable ITree.NodeElements => this.NodeElements;
		public IEnumerable<T> AllElements { get {
			if (this.IsLeaf)
				foreach (T e in this.NodeElements)
					yield return e;
			else
				foreach (T e in this.Children.SelectMany(c => c.AllElements))
					yield return e;
		} }
		IEnumerable ITree.AllElements => this.AllElements;
		
		public abstract IEnumerable<ATree<T>> Children { get; }
		IEnumerable<ITree> ITree.Children => this.Children;
		public IEnumerable<ATree<T>> AllNodes { get {
			if (this.IsLeaf)
				yield return this;
			else
				foreach (ATree<T> child in this.Children.SelectMany(c => c.AllNodes))
					yield return child;
		} }
		IEnumerable<ITree> ITree.AllNodes => this.AllNodes;
		public IEnumerable<ATree<T>> SiblingNodes { get {
			if (this.IsRoot)
				return Enumerable.Empty<ATree<T>>();
			else
				return this.Parent.Children.Except(n => ReferenceEquals(this, n));
		} }
		IEnumerable<ITree> ITree.SiblingNodes => this.SiblingNodes;
		public IEnumerable<ATree<T>> Leaves { get {
			if (this.IsLeaf)
				yield return this;
			else
				foreach (ATree<T> child in this.Children.SelectMany(c => c.Leaves))
					yield return child;
		} }
		IEnumerable<ITree> ITree.Leaves => this.Leaves;

		public void Add(T element) {
			this.Incorporate(element);
			if (this.IsLeaf)
				this.AddElementToNode(element);
			else
				this.GetContainingChild(element).Add(element);
		}
		public void Add(object element) { this.Add((T)element); }

		public void AddRange(IEnumerable<T> elements) {
			foreach (T e in elements)
				this.Add(e);
		}
		public void AddRange(IEnumerable<object> elements) { this.AddRange(elements.Cast<T>()); }

		protected virtual void Incorporate(T element) { }
		protected abstract void AddElementToNode(T element);

		public abstract bool DoesContain(T element);
		public bool DoesContain(object element) { return this.DoesContain((T)element); }
		public virtual ATree<T> GetContainingChild(T element) {
			foreach (ATree<T> child in this.Children)
				if (child.Contains(element))
					return child;
			throw new Exception("Element is not contained");
		}
		public ITree GetContainingChild(object element) { return this.GetContainingChild((T)element); }
		public ATree<T> GetContainingLeaf(T element) {
			foreach (ATree<T> node in this.Children)
				if (node.Contains(element))
					return node.GetContainingLeaf(element);
			return this;
		}
		public ITree GetContainingLeaf(object element) { return this.GetContainingLeaf((T)element); }

		public IEnumerable<T> GetNeighbors() {
			foreach (T e in this.AllElements)
				yield return e;
			if (!this.IsRoot)
				foreach (T member in this.SeekUpward().SelectMany(n => n.AllElements))
					yield return member;
		}
		public IEnumerable<ATree<T>> GetRefinedNeighborNodes(int depthLimit) {
			yield return this;

			if (depthLimit > 0 && !this.IsLeaf)
				foreach (ATree<T> n in this.Children.SelectMany(q => q.GetRefinedNeighborNodes(depthLimit - 1)))
					yield return n;

			if (this.IsRoot)
				yield break;
			else
				foreach (ATree<T> n in this.SiblingNodes.SelectMany(q => q.GetChildren(depthLimit - 1)))
					yield return n;
		}
		private IEnumerable<ATree<T>> GetChildren(int depthLimit) {
			if (depthLimit > 0)
				foreach (ATree<T> node2 in this.Children.SelectMany(c => c.GetChildren(depthLimit - 1)))
					yield return node2;
			else
				foreach (ATree<T> node in this.Children)
					yield return node;
		}

		private IEnumerable<ATree<T>> SeekUpward() {
			foreach (ATree<T> node in this.SiblingNodes.SelectMany(q => q.AllNodes))
				yield return node;
			if (!this.Parent.IsRoot)
				foreach (ATree<T> node in this.Parent.SeekUpward())
					yield return node;
		}

		public IEnumerator<T> GetEnumerator() { return this.AllElements.GetEnumerator(); }
		IEnumerator IEnumerable.GetEnumerator() { return this.AllElements.GetEnumerator(); }
		public override string ToString() { return string.Format("{0}[Depth {1}]", nameof(ATree<T>), this.Depth); }
	}
}