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

		void AddRange(IEnumerable<T> items) {
			foreach (T item in items) this.Add(item); }
		void ICollection<T>.CopyTo(T[] array, int arrayIndex) {
			int i = 0; foreach (T item in this) array[i++ + arrayIndex] = item; }
	}

	public abstract class ATree<T> : ITree<T> {
		public ATree(ATree<T> parent = null) { this.Parent = parent; }
		~ATree() {
			this.Parent = null;
			this.Children = null;
			this._bin = null;
		}

		public override string ToString() => string.Format("{0}Node[{1}]",
			this.IsRoot ? "Root" : this.IsLeaf ? "Leaf" : "Inner",
			this.Count.Pluralize("item"));
		
		public virtual int Capacity => 1;
		public int Count { get; protected set; }

		public bool IsRoot => this.Parent is null;
		public bool IsLeaf => this.Children is null;
		public virtual bool LimitReached => false;

		public ATree<T> Parent { get; protected set; }
		ITree<T> ITree<T>.Parent => this.Parent;
		public ATree<T>[] Children { get; protected set; }
		ICollection<ITree<T>> ITree<T>.Children => this.Children;

		private ICollection<T> _bin = null;

		public abstract bool DoesEncompass(T item);

		protected abstract IEnumerable<ATree<T>> FormSubnodes();
		protected virtual ICollection<T> NewBin() => new HashedContainer<T>();

		protected virtual void Incorporate(T item) { }
		protected virtual void AfterRemove(T item) { }

		protected virtual int GetChildIndex(T item) {
			for (int i = 0; i < this.Children.Length; i++)
				if (this.Children[i].DoesEncompass(item))
					return i;
			throw new Exception("Element does not belong");
		}

		public void Add(T item) {
			ATree<T> node = this;
			while (!node.IsLeaf) {
				node.Count++;
				node.Incorporate(item);
				node = node.Children[node.GetChildIndex(item)];
			}
			node.Count++;
			node.Incorporate(item);

			if (node.Count <= node.Capacity || node.LimitReached)
				(node._bin ??= node.NewBin()).Add(item);
			else node.AddLayer(item);
		}

		private void AddLayer(T item) {
			this.Children = this.FormSubnodes().ToArray();
			foreach (T subItem in this._bin)
				this.Children
					[this.GetChildIndex(subItem)]
					.Add(subItem);
			this._bin = null;
			this.Children
				[this.GetChildIndex(item)]
				.Add(item);
		}

		public bool Remove(T item) {
			if (this.Count > 0) {
				Queue<ATree<T>> chain = new();
				ATree<T> node = this;
				bool encompasses = node.DoesEncompass(item);
				while (encompasses && !node.IsLeaf) {
					chain.Enqueue(node);
					node = node.Children[this.GetChildIndex(item)];
					encompasses = node.DoesEncompass(item);
				}

				if (encompasses && node.Count > 0 && node._bin.Remove(item)) {
					ATree<T> tempNode;
					while (chain.TryDequeue(out tempNode)) {
						tempNode.Count--;
						tempNode.AfterRemove(item);
					}
					return true;
				}
			}
			return false;
		}
		public void UncheckedRemove(T item) {
			ATree<T> node = this;

			node.Count--;
			node.AfterRemove(item);
			while (!node.IsLeaf) {
				node = node.Children[this.GetChildIndex(item)];
				node.Count--;
				node.AfterRemove(item);
			}

			node._bin.Remove(item);
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
			if (!(this._bin is null))
				this._bin.Clear();
		}

		public bool Contains(T item) {
			ATree<T> node = this;
			bool encompasses = node.DoesEncompass(item);
			while (encompasses && !node.IsLeaf) {
				node = node.Children[this.GetChildIndex(item)];
				encompasses = node.DoesEncompass(item);
			}

			if (encompasses && node.Count > 0)
				return node._bin.Contains(item);
			else return false;
		}

		public IEnumerator<T> GetEnumerator() => this.AsEnumerable().GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => this.AsEnumerable().GetEnumerator();

		public IEnumerable<T> AsEnumerable() {
			Queue<ATree<T>> remaining = new Queue<ATree<T>>();
			remaining.Enqueue(this);

			while (remaining.TryDequeue(out ATree<T> node))
				if (!node.IsLeaf)
					for (int i = 0; i < node.Children.Length; i++)
						remaining.Enqueue(node.Children[i]);
				else if (!(node._bin is null))
					foreach (T item in node._bin)
						yield return item;
		}

		public Queue<T> ToQueue() {
			Queue<T> result = new Queue<T>();

			Queue<ATree<T>> remaining = new Queue<ATree<T>>();
			remaining.Enqueue(this);

			while (remaining.TryDequeue(out ATree<T> node))
				if (!node.IsLeaf)
					for (int i = 0; i < node.Children.Length; i++)
						remaining.Enqueue(node.Children[i]);
				else if (!(node._bin is null))
					foreach (T item in node._bin)
						result.Enqueue(item);

			return result;
		}
	}
}