using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Generic.Extensions;

namespace Generic.Trees {
	public abstract class ABinaryTree<T> : ITree, ICollection<T>, IEnumerable<T> {
		public ABinaryTree(ABinaryTree<T> parent = null) {
			this.Parent = parent;
		}

		public override string ToString() => string.Format("{0}Node[{1}]",
			this.IsRoot && this.IsLeaf ? "Sole" : this.IsRoot ? "Root" : this.IsLeaf ? "Leaf" : "Inner",
			this.ItemCount.Pluralize("item"));
		
		public int Count => this.ItemCount;
		public int ItemCount;
		public bool IsReadOnly => false;

		public abstract bool MaxDepthReached { get; }
		public virtual int LeafCapacity => 1;

		public bool IsRoot => this.Parent is null;
		public bool IsLeaf => this.Children is null;
		protected bool _isReceiving => this.ItemCount < this.LeafCapacity || this.MaxDepthReached;

		public ABinaryTree<T> Parent;
		ITree ITree.Parent => this.Parent;
		public ABinaryTree<T>[] Children;//srsly why does making this a FIELD instead of a PROEPRTY improve performance by 25%?
		IEnumerable<ITree> ITree.Children => this.Children;
		public ABinaryTree<T> Root { get {
			ABinaryTree<T> node = this;
			while (!node.IsRoot)
				node = node.Parent;
			return node; } }

		public ICollection<T> Bin;

		public abstract bool DoesEncompass(T item);
		protected abstract IEnumerable<ABinaryTree<T>> FormSubnodes();
		protected abstract ABinaryTree<T> Expand(T item);

		protected virtual ICollection<T> NewBin() => new HashedContainer<T>();

		public virtual int ChildIndex(T item) {
			for (int i = 0; i < this.Children.Length; i++)
				if (this.Children[i].DoesEncompass(item))
					return i;
			throw new Exception("Element does not belong");
		}

		public ABinaryTree<T> GetContainingLeaf(T item) {
			ABinaryTree<T> node = this;
			while (!node.DoesEncompass(item))
				if (node.IsRoot)
					throw new Exception("Uncontained");
				else node = node.Parent;
			while (!node.IsLeaf)
				node = node.Children[node.ChildIndex(item)];
			return node;
		}

		public void Add(T item) {
			ABinaryTree<T> node = this;
			while (!node.DoesEncompass(item))
				node = node.IsRoot
					? node.Expand(item)
					: node.Parent;

			ABinaryTree<T> startingNode = node;
			while (!node.IsLeaf) {
				++node.ItemCount;
				node = node.Children[node.ChildIndex(item)];
			}
			node.AddToLeaf(item);//increments the count
			while (!startingNode.IsRoot) {
				startingNode = startingNode.Parent;
				++startingNode.ItemCount;
			}
		}

		public ABinaryTree<T> Add(IEnumerable<T> items) {
			ABinaryTree<T> parent = this;
			foreach (T item in items) {
				parent.Add(item);
				while (!parent.IsRoot)
					parent = parent.Parent;
			}
			return parent;
		}

		public bool Remove(T item, bool prune) {
			ABinaryTree<T> node = this;
			while (!node.DoesEncompass(item))
				if (node.IsRoot)
					return false;
				else node = node.Parent;

			while (!node.IsLeaf) 
				node = node.Children[node.ChildIndex(item)];

			return node.RemoveFromLeaf(item, prune);
		}
		public bool Remove(T item) => this.Remove(item, true);

		public ABinaryTree<T> MoveFromLeaf(T item, bool prune = true) {
			ABinaryTree<T> node = this;
			if (!node.DoesEncompass(item)) {
				node.Bin.Remove(item);
				--node.ItemCount;

				bool encompasses;
				do {
					if (node.IsRoot) {
						node = node.Expand(item);
					} else {
						node = node.Parent;
						--node.ItemCount;
					}

					if (prune && node.ItemCount == 0)
						node.Children = null;

					encompasses = node.DoesEncompass(item);
				} while (!encompasses);
				
				while (!node.IsLeaf) {
					++node.ItemCount;
					node = node.Children[node.ChildIndex(item)];
				}
				node.AddToLeaf(item);//increments the count
			}
			return node;
		}

		public bool RemoveFromLeaf(T item, bool prune = true) {
			ABinaryTree<T> node = this;
			if (node.Bin.Remove(item)) {
				--node.ItemCount;
				while (!node.IsRoot) {
					node = node.Parent;
					--node.ItemCount;
					if (prune && node.ItemCount == 0)
						node.Children = null;
				}
				return true;
			} else return false;
		}

		protected void AddToLeaf(T item) {//increments the count
			ABinaryTree<T> node = this;
			while (!node._isReceiving) {
				node.Refine();
				++node.ItemCount;
				node = node.Children[node.ChildIndex(item)];
			}
			node.Bin ??= this.NewBin();
			node.Bin.Add(item);
			++node.ItemCount;
		}

		private void Refine() {
			this.Children = this.FormSubnodes().ToArray();
			ABinaryTree<T> node;
			if (this.ItemCount == 1) {
				node = this.Children[this.ChildIndex(this.Bin.First())];
				++node.ItemCount;
				node.Bin = this.Bin;
			} else if (this.ItemCount > 1) {
				foreach (T item in this.Bin) {
					node = this.Children[this.ChildIndex(item)];
					++node.ItemCount;
					node.Bin ??= node.NewBin();
					node.Bin.Add(item);
				}
			}
			this.Bin = null;
		}
		
		//finds the first sub node with more than one child
		public virtual ABinaryTree<T> PruneTop() {
			ABinaryTree<T> node = this.Root;

			int count, idx;
			while (!node.IsLeaf) {
				count = idx = 0;
				for (int i = 0; i < node.Children.Length; i++)
					if (node.Children[i].ItemCount > 0)
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
			this.ItemCount = 0;
			this.Children = null;
			if (!(this.Bin is null))
				this.Bin.Clear();
		}

		public bool Contains(T item) {
			ABinaryTree<T> node = this;
			bool encompasses = node.DoesEncompass(item);
			while (encompasses && !node.IsLeaf) {
				node = node.Children[node.ChildIndex(item)];
				encompasses = node.DoesEncompass(item);
			}

			if (encompasses && node.ItemCount > 0)
				return node.Bin.Contains(item);
			else return false;
		}

		public IEnumerator<T> GetEnumerator() => this.AsEnumerable().GetEnumerator();

		public void CopyTo(T[] array, int arrayIndex) {
			int i = 0; foreach (T item in this) array[i++ + arrayIndex] = item; }

		public IEnumerable<T> AsEnumerable() {
			Stack<ABinaryTree<T>> remaining = new Stack<ABinaryTree<T>>();

			remaining.Push(this);

			while (remaining.TryPop(out ABinaryTree<T> node))
				if (node.IsLeaf) {
					if (!(node.Bin is null))
						foreach (T item in node.Bin)
							yield return item;
				} else for (int i = 0; i < node.Children.Length; i++)
					if (node.Children[i].ItemCount > 0)
						remaining.Push(node.Children[i]);
		}
		IEnumerable ITree.AsEnumerable() => this.AsEnumerable();

		public T[] AsArray() {
			if (this.ItemCount > 0) {
				T[] result = new T[this.ItemCount];
				Stack<ABinaryTree<T>> remaining = new Stack<ABinaryTree<T>>();
				remaining.Push(this);

				int idx = 0;
				while (remaining.TryPop(out ABinaryTree<T> node))
					if (node.IsLeaf) {
						if (!(node.Bin is null))
							foreach (T item in node.Bin)
								result[idx++] = item;
					} else for (int i = 0; i < node.Children.Length; i++)
						if (node.Children[i].ItemCount > 0)
							remaining.Push(node.Children[i]);

				return result;
			} else return Array.Empty<T>();
		}
	}
}