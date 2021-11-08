using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Generic.Models {
	public abstract class ATree<T> : IEnumerable<T> {
		public readonly int Depth;
		public readonly ATree<T> Parent;

		public ATree() { }
		public ATree(ATree<T> parent) {
			this.Depth = parent is null ? 0 : parent.Depth + 1;
			this.Parent = parent;
		}

		public bool IsRoot { get { return this.Depth == 0; } }
		public virtual bool IsLeaf { get { return this.Children.None(); } }
		
		public abstract IEnumerable<T> NodeElements { get; }
		public IEnumerable<T> AllElements { get {
			if (this.IsLeaf)
				foreach (T e in this.NodeElements)
					yield return e;
			else
				foreach (T e in this.Children.SelectMany(c => c.AllElements))
					yield return e;
		} }
		
		public abstract IEnumerable<ATree<T>> Children { get; }
		public IEnumerable<ATree<T>> AllNodes { get {
			if (this.IsLeaf)
				yield return this;
			else
				foreach (ATree<T> child in this.Children.SelectMany(c => c.AllNodes))
					yield return child;
		} }
		public IEnumerable<ATree<T>> SiblingNodes { get {
			if (this.IsRoot)
				return Enumerable.Empty<ATree<T>>();
			else
				return this.Parent.Children.Except(n => ReferenceEquals(this, n));
		} }
		public IEnumerable<ATree<T>> Leaves { get {
			if (this.IsLeaf)
				yield return this;
			else
				foreach (ATree<T> child in this.Children.SelectMany(c => c.Leaves))
					yield return child;
		} }


		public abstract void Add(T element);
		public void Add(T element, params T[] more) {
			this.Add(element);
			for (int i = 0; i < more.Length; i++)
				this.Add(more[i]);
		}
		public void AddRange(IEnumerable<T> elements) { foreach (T e in elements) this.Add(e); }

		public abstract bool DoesContain(T element);
		public virtual ATree<T> GetContainingChild(T element) {
			foreach (ATree<T> child in this.Children)
				if (child.Contains(element))
					return child;
			throw new Exception("Element is not contained");
		}
		public ATree<T> GetContainingLeaf(T element) {
			foreach (ATree<T> node in this.Children)
				if (node.Contains(element))
					return node.GetContainingLeaf(element);
			return this;
		}

		public IEnumerable<T> GetNeighbors() {
			foreach (T e in this.AllElements)
				yield return e;
			if (!this.IsRoot)
				foreach (T member in this.SeekUpward().SelectMany(n => n.AllElements))
					yield return member;
		}
		public IEnumerable<T> GetNeighbors(Predicate<ATree<T>> nodeTest) {
			foreach (T e in this.AllElements)
				yield return e;
			if (!this.IsRoot)
				foreach (T member in this.SeekUpward(nodeTest).SelectMany(n => n.AllElements))
					yield return member;
		}

		private IEnumerable<ATree<T>> SeekUpward() {
			foreach (ATree<T> node in this.Parent.Children
			.Except(q => ReferenceEquals(this, q))
			.SelectMany(q => q.AllNodes))
				yield return node;
			if (!this.Parent.IsRoot)
				foreach (ATree<T> node in this.Parent.SeekUpward())
					yield return node;
		}
		private IEnumerable<ATree<T>> SeekUpward(Predicate<ATree<T>> nodeTest) {
			foreach (ATree<T> node in this.Parent.Children
			.Except(q => ReferenceEquals(this, q) || nodeTest(q))
			.SelectMany(q => q.AllNodes))
				yield return node;
			if (!this.Parent.IsRoot)
				foreach (ATree<T> node in this.Parent.SeekUpward(nodeTest))
					yield return node;
		}

		public IEnumerator<T> GetEnumerator() { return this.AllElements.GetEnumerator(); }
		IEnumerator IEnumerable.GetEnumerator() { return this.AllElements.GetEnumerator(); }
		public override string ToString() { return string.Format("{0}[Depth {1}]", nameof(ATree<T>), this.Depth); }
	}
}