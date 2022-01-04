using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Generic.Extensions;

namespace Generic.Models.Trees {
	public interface ITree<T> : ICollection<T>, IEnumerable<T> {
		ITree<T> Parent { get; }
		ICollection<ITree<T>> Children { get; }
		
		bool IsRoot { get; }
		bool IsLeaf { get; }
		bool ICollection<T>.IsReadOnly => false;

		void ICollection<T>.CopyTo(T[] array, int arrayIndex) {
			int i = 0; foreach (T item in this) array[i++ + arrayIndex] = item; }
	}

	public abstract class ATree<T> : ITree<T> {
		public ATree(ATree<T> parent = null) {
			this.Parent = parent;
		}

		public override string ToString() => string.Format("{0}Node[{1}]",
			this.IsRoot && this.IsLeaf ? "Sole" : this.IsRoot ? "Root" : this.IsLeaf ? "Leaf" : "Inner",
			this.Count.Pluralize("item"));
		
		public virtual int Capacity => 1;
		public int Count { get; protected set; }
		public abstract bool LimitReached { get; }

		public bool IsRoot => this.Parent is null;
		public bool IsLeaf => this.Children is null;

		public ATree<T> Parent { get; protected set; }
		ITree<T> ITree<T>.Parent => this.Parent;
		public ATree<T>[] Children { get; protected set; }
		ICollection<ITree<T>> ITree<T>.Children => this.Children;
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
		protected virtual bool TryMerge(T item1) => false;

		public virtual int ChildIndex(T item) {
			for (int i = 0; i < this.Children.Length; i++)
				if (this.Children[i].DoesEncompass(item))
					return i;
			throw new Exception("Element does not belong");
		}

		public void Add(T item) {
			ATree<T> node = this;
			bool includes = node.DoesEncompass(item);
			if (!includes) {
				while (!includes) {
					if (node.IsRoot)
						node = node.Expand(item);
					else node = node.Parent;
					includes = node.DoesEncompass(item);
				}
			}
			while (!node.IsLeaf)
				node = node.Children[node.ChildIndex(item)];
			node.AddToLeaf(item);
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
			if (this.Count > 0) {
				Queue<ATree<T>> chain = new();
				ATree<T> node = this;
				bool encompasses = node.DoesEncompass(item);
				while (encompasses && !node.IsLeaf) {
					chain.Enqueue(node);
					node = node.Children[this.ChildIndex(item)];
					encompasses = node.DoesEncompass(item);
				}

				if (encompasses && node.Count > 0 && node.Bin.Remove(item)) {
					ATree<T> tempNode;
					while (chain.TryDequeue(out tempNode))
						tempNode.Count--;
					return true;
				}
			}
			return false;
		}

		public void MoveFromLeaf(T item) {
			if (!this.DoesEncompass(item)) {
				this.Count--;
				this.Bin.Remove(item);

				ATree<T> targetNode = null, node = this;
				while (!node.IsRoot) {
					if (node.Count == 0)
						node.Children = null;
					node = node.Parent;
					node.Count--;
					if (node.DoesEncompass(item)) {
						targetNode = node;
						break;
					}
				}
				if (targetNode is null) {
					node.Add(item);
				} else {
					while (!node.IsRoot) {
						node = node.Parent;
						node.Count--;
					}
					while (!targetNode.IsLeaf)
						targetNode = targetNode.Children[targetNode.ChildIndex(item)];
					targetNode.AddToLeaf(item);
				}
			}
		}

		public void AddToLeaf(T addition) {
			ATree<T> node;
			if (this.Count < this.Capacity || this.LimitReached) {
				(this.Bin ??= this.NewBin()).Add(addition);
				this.Count++;
				node = this;
				while (!node.IsRoot) {
					node = node.Parent;
					node.Count++;
				}
			} else {
				Queue<T> additions = new(this.Bin);
				additions.Enqueue(addition);
				this.Bin = null;

				int startingCount = this.Count;
				this.Count = 0;
				this.Children = this.FormSubnodes().ToArray();

				bool unmerged;
				Stack<ATree<T>> path = new();
				ATree<T> parentNode;
				T item;
				while (additions.TryDequeue(out item)) {
					if (this.DoesEncompass(item)) {
						node = this.Children[this.ChildIndex(item)];
						path.Clear();
						unmerged = true;
						while (node.Count >= node.Capacity) {
							path.Push(node);
							if (node.IsLeaf) {
								if (node.TryMerge(item)) {
									unmerged = false;
									break;
								} else {
									parentNode = node;
									while (!ReferenceEquals(this, parentNode)) {
										parentNode = parentNode.Parent;
										parentNode.Count -= node.Count;
									}
									if (node.LimitReached) {
										break;
									} else {
										node.Count = 0;
										foreach (T item2 in node.Bin)
											additions.Enqueue(item2);
										node.Bin = null;
										node.Children = node.FormSubnodes().ToArray();
									}
								}
							}
							node = node.Children[node.ChildIndex(item)];
						}
						if (unmerged) {
							node.Bin ??= node.NewBin();
							node.Bin.Add(item);
							node.Count++;
							while (path.TryPop(out node))
								node.Count++;
							this.Count++;
						}
					} else this.Add(item);
				}
				if (startingCount != this.Count) {
					int diff = this.Count - startingCount;
					node = this;
					while (!node.IsRoot) {
						node = node.Parent;
						node.Count += diff;
					}
				}
			}
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
				node = node.Children[this.ChildIndex(item)];
				encompasses = node.DoesEncompass(item);
			}

			if (encompasses && node.Count > 0)
				return node.Bin.Contains(item);
			else return false;
		}

		public IEnumerator<T> GetEnumerator() => this.AsEnumerable().GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => this.AsEnumerable().GetEnumerator();

		public IEnumerable<T> AsEnumerable() {
			Stack<ATree<T>> remaining = new Stack<ATree<T>>();

			remaining.Push(this);

			while (remaining.TryPop(out ATree<T> node))
				if (node.IsLeaf) {
					if (!(node.Bin is null))
						foreach (T item in node.Bin)
							yield return item;
				} else for (int i = 0; i < node.Children.Length; i++)
					remaining.Push(node.Children[i]);
		}

		public Queue<T> AsQueue() {
			Queue<T> result = new Queue<T>();

			Stack<ATree<T>> remaining = new Stack<ATree<T>>();
			remaining.Push(this);

			while (remaining.TryPop(out ATree<T> node))
				if (node.IsLeaf) {
					if (!(node.Bin is null))
						foreach (T item in node.Bin)
							result.Enqueue(item);
				} else for (int i = 0; i < node.Children.Length; i++)
					remaining.Push(node.Children[i]);

			return result;
		}
	}
}