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

		public ICollection<T> Bin { get; protected set; }

		public abstract bool DoesEncompass(T item);

		protected abstract IEnumerable<ATree<T>> FormSubnodes();
		protected abstract ATree<T> Expand(T item);
		protected virtual ICollection<T> NewBin() => new HashedContainer<T>();

		protected virtual int GetIndex(T item) {
			for (int i = 0; i < this.Children.Length; i++)
				if (this.Children[i].DoesEncompass(item))
					return i;
			throw new Exception("Element does not belong");
		}

		public void Add(T item) {
			ATree<T> node = this;
			while (!node.DoesEncompass(item))
				if (node.IsRoot)
					node = node.Expand(item);
				else node = node.Parent;

			node.AddContained(item);
		}
		public void AddRange(IEnumerable<T> items) {
			foreach (T item in items)
				this.Add(item);
		}

		protected void AddContained(T item) {
			Stack<ATree<T>> path = new();
			ATree<T> node = this;
			while (!node.IsLeaf) {
				path.Push(node);
				node = node.Children[node.GetIndex(item)];
			}

			if (node.Count == 0 || !node.TryMerge(item)) {
				node.Count++;
				if (node.Count <= node.Capacity || node.LimitReached)
					(node.Bin ??= node.NewBin()).Add(item);
				else node.AddLayer(item);

				while (path.TryPop(out node))
					node.Count++;
			}
		}
		protected virtual bool TryMerge(T p1) => false;

		public void Move(T item) {
			if (!this.DoesEncompass(item)) {
				this.Count--;
				this.Bin.Remove(item);
				
				ATree<T> targetNode = null, node = this;
				while (!node.IsRoot && targetNode is null) {
					node = node.Parent;
					node.Count--;
					if (node.DoesEncompass(item))
						targetNode = node;
				}
				(targetNode ?? node).AddContained(item);
			}
		}

		protected void AddLayer(T item) {
			this.Children = this.FormSubnodes().ToArray();
			foreach (T subItem in this.Bin)
				this.Add(subItem);
			this.Bin = null;
			this.Children
				[this.GetIndex(item)]
				.Add(item);
		}

		public bool Remove(T item) {
			if (this.Count > 0) {
				Queue<ATree<T>> chain = new();
				ATree<T> node = this;
				bool encompasses = node.DoesEncompass(item);
				while (encompasses && !node.IsLeaf) {
					chain.Enqueue(node);
					node = node.Children[this.GetIndex(item)];
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

		public ATree<T> Contract() {
			ATree<T> node = this, subNode;
			while (!(node.Children is null)) {
				subNode = null;
				for (int i = 0; i < node.Children.Length; i++)
					if (node.Children[i].Count > 0)
						if (subNode is null)
							subNode = node.Children[i];
						else return node;
				node = subNode;
			}
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
				node = node.Children[this.GetIndex(item)];
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