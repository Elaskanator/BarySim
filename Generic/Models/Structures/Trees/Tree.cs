﻿using System;
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

	public abstract class ATree<E, T> : ITree, IEnumerable<E>
	where T : ATree<E, T> {
		public int Depth { get; private set; }
		public T Parent { get; private set; }

		public ATree() { }
		public ATree(T parent) {
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
		
		public abstract IEnumerable<T> Children { get; }
		IEnumerable<ITree> ITree.Children => this.Children;
		public IEnumerable<T> NestedChildren { get {
			foreach (T node in this.Children) {
				yield return node;
				foreach (T subnode in node.NestedChildren)
					yield return subnode;
		} } }
		IEnumerable<ITree> ITree.NestedChildren { get { return this.NestedChildren; } }
		public IEnumerable<T> AllNodes { get {
			if (this.IsLeaf) yield return (T)this;
			else foreach (T child in this.Children.SelectMany(c => c.AllNodes))
				yield return child;
		} }
		IEnumerable<ITree> ITree.AllNodes => this.AllNodes;
		public IEnumerable<T> SiblingNodes { get {
			if (this.IsRoot) return Enumerable.Empty<T>();
			else return this.Parent.Children.Without(n => ReferenceEquals(this, n));
		} }
		IEnumerable<ITree> ITree.SiblingNodes => this.SiblingNodes;
		public IEnumerable<T> Leaves { get {
			if (this.IsLeaf) yield return (T)this;
			else foreach (T child in this.Children.SelectMany(c => c.Leaves))
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
		protected virtual T GetContainingChild(E element) {
			return this.Children.First(c => c.Contains(element));
			throw new Exception("Element is not contained");
		}
		protected T GetContainingLeaf(E element) {
			foreach (T node in this.Children)
				if (node.Contains(element))
					return node.GetContainingLeaf(element);
			return (T)this;
		}
		
		public IEnumerable<T> GetNeighborhoodNodes(int? limit = null) {//must be used from a leaf
			return this.GetNeighborhoodNodes_up(limit, this);
		}
		private IEnumerable<T> GetNeighborhoodNodes_up(int? limit, ATree<E, T> start) {
			if (!this.IsRoot) {
				foreach (T node in this.SiblingNodes.SelectMany(s => s.GetNeighborhoodNodes_down(limit, start)))
					yield return node;
				foreach (T node in this.Parent.GetNeighborhoodNodes_up(limit - 1, start))
					yield return node;
			}
		}
		private IEnumerable<T> GetNeighborhoodNodes_down(int? limit, ATree<E, T> start) {
			if (this.NumMembers == 0)
				yield break;
			else if (!this.IsLeaf && (limit ?? 1) > 0)
				foreach (T node in this.Children.SelectMany(c => c.GetNeighborhoodNodes_down(limit - 1, start)))
					yield return node;
			else yield return (T)this;
		}

		public Tuple<T[], T[]> RecursiveFilter(Predicate<T> predicate) {
			if (this.NumMembers == 0) {
				return new(Array.Empty<T>(), Array.Empty<T>());
			} else if (this.IsLeaf) {
				if (predicate((T)this))
					return new(new T[] { (T)this }, Array.Empty<T>());
				else return new(Array.Empty<T>(), new T[] { (T)this });
			} else {
				Tuple<bool, T>[] tests = this.Children.Where(c => c.NumMembers > 0).Select(c => new Tuple<bool, T>(predicate(c), c)).ToArray();
				Tuple<T[], T[]> moreFiltered;
				Tuple<IEnumerable<T>, IEnumerable<T>> newJunk =
					tests.Where(t => t.Item1)
						.Select(t => t.Item2)
						.Aggregate(
							new Tuple<IEnumerable<T>, IEnumerable<T>>(Enumerable.Empty<T>(), Enumerable.Empty<T>()), (agg, node) => {
								moreFiltered = node.RecursiveFilter(predicate);
								return new(agg.Item1.Concat(moreFiltered.Item1),
									agg.Item2.Concat(moreFiltered.Item2));
							});
				return new(
					newJunk.Item1.ToArray(),
					newJunk.Item2.Concat(tests.Without(t => t.Item1).Select(t => t.Item2)).ToArray());
			}
		}

		public IEnumerator<E> GetEnumerator() { return this.AllElements.GetEnumerator(); }
		IEnumerator IEnumerable.GetEnumerator() { return this.AllElements.GetEnumerator(); }
		public override string ToString() { return string.Format("{0}[Depth {1}]", nameof(T), this.Depth); }
	}
}