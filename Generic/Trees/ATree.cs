using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Generic.Extensions;

namespace Generic.Models.Trees {
	public interface ITree : IEnumerable {
		ITree Parent { get; }
		ICollection<ITree> Children { get; }
		
		bool IsRoot { get; }
		bool IsLeaf { get; }
		IEnumerable AsEnumerable();
		IEnumerator IEnumerable.GetEnumerator() => this.AsEnumerable().GetEnumerator();
	}

	public abstract class ATree<T> : ITree, ICollection<T>, IEnumerable<T> {
		public ATree(ATree<T> parent = null) {
			this.Parent = parent;
		}

		public override string ToString() => string.Format("{0}Node[{1}]",
			this.IsRoot && this.IsLeaf ? "Sole" : this.IsRoot ? "Root" : this.IsLeaf ? "Leaf" : "Inner",
			this.Count.Pluralize("item"));
		
		public int Count { get; protected set; }
		public bool IsReadOnly => false;

		public abstract bool MaxDepthReached { get; }

		public bool IsRoot => this.Parent is null;
		public bool IsLeaf => this.Children is null;
		protected bool _isReceiving => this.Count == 0 || this.MaxDepthReached;

		public ATree<T> Parent { get; set; }
		ITree ITree.Parent => this.Parent;
		public ATree<T>[] Children { get; protected set; }
		ICollection<ITree> ITree.Children => this.Children;
		public ATree<T> Root { get {
			ATree<T> node = this;
			while (!node.IsRoot)
				node = node.Parent;
			return node; } }

		public ICollection<T> Bin { get; protected set; }

		public abstract bool DoesEncompass(T item);
		protected abstract IEnumerable<ATree<T>> FormSubnodes();
		protected abstract ATree<T> Expand(T item);

		protected virtual ICollection<T> NewBin() => new HashedContainer<T>();

		protected virtual int ChildIndex(T item) {
			for (int i = 0; i < this.Children.Length; i++)
				if (this.Children[i].DoesEncompass(item))
					return i;
			throw new Exception("Element does not belong");
		}

		public ATree<T> GetContainingLeaf(T item) {
			ATree<T> node = this;
			while (!node.IsLeaf)
				node = node.Children[node.ChildIndex(item)];
			return node;
		}

		public void Add(T item) {
			ATree<T> node = this;
			while (!node.DoesEncompass(item)) {
				if (node.IsRoot) 
					node = node.Expand(item);
				else node = node.Parent;
			}
			ATree<T> startingNode = node;
			while (!node.IsLeaf) {
				++node.Count;
				node = node.Children[node.ChildIndex(item)];
			}
			node.AddToLeaf(item);//increments the count
			while (!startingNode.IsRoot) {
				startingNode = startingNode.Parent;
				++startingNode.Count;
			}
		}

		public void Add(IEnumerable<T> items) {
			ATree<T> parent = this;
			foreach (T item in items) {
				parent.Add(item);
				while (!parent.IsRoot)
					parent = parent.Parent;
			}
		}

		public bool Remove(T item) {
			ATree<T> node = this;
			while (!node.DoesEncompass(item))
				if (node.IsRoot)
					return false;
				else node = node.Parent;

			while (!node.IsLeaf) 
				node = node.Children[node.ChildIndex(item)];

			if (node.Bin.Remove(item)) {
				--node.Count;
				while (!node.IsRoot) {
					node = node.Parent;
					if (--node.Count == 0)
						node.Children = null;
				}
				return true;
			} else return false;
		}

		public void MoveFromLeaf(T item) {
			if (!this.DoesEncompass(item)) {
				this.Bin.Remove(item);
				--this.Count;

				ATree<T> node = this;
				bool encompasses;
				do {
					if (node.IsRoot) {
						node = node.Expand(item);
					} else {
						node = node.Parent;
						--node.Count;
					}

					if (node.Count == 0)
						node.Children = null;

					encompasses = node.DoesEncompass(item);
				} while (!encompasses);
				
				while (!node.IsLeaf) {
					++node.Count;
					node = node.Children[node.ChildIndex(item)];
				}
				node.AddToLeaf(item);//increments the count
			}
		}

		protected void AddToLeaf(T item) {//increments the count
			ATree<T> node = this;
			while (!node._isReceiving) {
				node.Refine();
				++node.Count;
				node = node.Children[node.ChildIndex(item)];
			}
			node.Bin ??= this.NewBin();
			node.Bin.Add(item);
			++node.Count;
		}

		private void Refine() {
			this.Children = this.FormSubnodes().ToArray();
			ATree<T> node;
			if (this.Count == 1) {
				node = this.Children[this.ChildIndex(this.Bin.First())];
				++node.Count;
				node.Bin = this.Bin;
			} else if (this.Count > 1) {
				foreach (T item in this.Bin) {
					node = this.Children[this.ChildIndex(item)];
					++node.Count;
					node.Bin ??= node.NewBin();
					node.Bin.Add(item);
				}
			} else ;
			this.Bin = null;
		}
		
		//finds the first sub node with more than one child
		public ATree<T> Prune() {
			ATree<T> node = this.Root;

			int count, idx;
			while (!node.IsLeaf) {
				count = idx = 0;
				for (int i = 0; i < node.Children.Length; i++)
					if (node.Children[i].Count > 0)
						if (++count > 1) break;
						else idx = i;
				if (count == 1)
					node = node.Children[idx];
				else break;
			}
			node.Parent = null;

			return node;
		}

		public void Clear() {
			this.Count = 0;
			this.Children = null;
			if (!(this.Bin is null))
				this.Bin.Clear();
		}

		public bool Contains(T item) {
			ATree<T> node = this;
			bool encompasses = node.DoesEncompass(item);
			while (encompasses && !node.IsLeaf) {
				node = node.Children[node.ChildIndex(item)];
				encompasses = node.DoesEncompass(item);
			}

			if (encompasses && node.Count > 0)
				return node.Bin.Contains(item);
			else return false;
		}

		public IEnumerator<T> GetEnumerator() => this.AsEnumerable().GetEnumerator();

		public void CopyTo(T[] array, int arrayIndex) {
			int i = 0; foreach (T item in this) array[i++ + arrayIndex] = item; }

		public IEnumerable<T> AsEnumerable() {
			Stack<ATree<T>> remaining = new Stack<ATree<T>>();

			remaining.Push(this);

			while (remaining.TryPop(out ATree<T> node))
				if (node.IsLeaf) {
					if (!(node.Bin is null))
						foreach (T item in node.Bin)
							yield return item;
				} else for (int i = 0; i < node.Children.Length; i++)
					if (node.Children[i].Count > 0)
						remaining.Push(node.Children[i]);
		}
		IEnumerable ITree.AsEnumerable() => this.AsEnumerable();

		public T[] AsArray() {
			if (this.Count > 0) {
				T[] result = new T[this.Count];
				Stack<ATree<T>> remaining = new Stack<ATree<T>>();
				remaining.Push(this);

				int idx = 0;
				while (remaining.TryPop(out ATree<T> node))
					if (node.IsLeaf) {
						if (!(node.Bin is null))
							foreach (T item in node.Bin)
								result[idx++] = item;
					} else for (int i = 0; i < node.Children.Length; i++)
						if (node.Children[i].Count > 0)
							remaining.Push(node.Children[i]);

				return result;
			} else return Array.Empty<T>();
		}
	}
}