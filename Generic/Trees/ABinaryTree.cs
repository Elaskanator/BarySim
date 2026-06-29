using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Generic.Extensions;

namespace Generic.Trees;

public abstract class ABinaryTree<TSelf, T>(TSelf parent = null) : ITree, ICollection<T>, IEnumerable<T>
	where TSelf : ABinaryTree<TSelf, T> {
	public override string ToString() => string.Format("{0}Node[{1}]",
		this.IsRoot && this.IsLeaf ? "Sole" : this.IsRoot ? "Root" : this.IsLeaf ? "Leaf" : "Inner",
		this.ItemCount.Pluralize("item"));
		
	public int Count => this.ItemCount;
	public int ItemCount;
	public bool IsReadOnly => false;

	public abstract bool MaxDepthReached { get; }
	public virtual int LeafCapacity => 1;
	protected bool IsReceiving => this.ItemCount < this.LeafCapacity || this.MaxDepthReached;

	public bool IsRoot => this.Parent is null;
	public bool IsLeaf => this.Children is null;

	public TSelf Parent = parent;
	ITree ITree.Parent => this.Parent;
	public TSelf[] Children;//srsly why does making this a FIELD instead of a PROEPRTY improve performance by 25%?
	IEnumerable<ITree> ITree.Children => this.Children;
	public TSelf Root { get {
		var node = this;
		while (!node.IsRoot)
			node = node.Parent;
		return (TSelf)node;
	} }
	public IEnumerable<TSelf> AllLeaves { get {
		Stack<TSelf> stack = new();
		stack.Push((TSelf)this);

		while (stack.TryPop(out TSelf node)) {
			if (node.IsLeaf) {
				yield return node;
			} else {
				for (int i = 0; i < node.Children.Length; i++) {
					if (node.Children[i].ItemCount > 0)
						stack.Push(node.Children[i]);
				}
			}
		}
	} }

	public List<T> Bin = null;

	public abstract bool DoesEncompass(T item);
	public int ParentChildIndex { get; private set; } = -1;

	protected abstract TSelf[] FormSubnodes();
	protected abstract TSelf Expand(T item);

	public virtual int ChildIndex(T item) {
		for (int i = 0; i < this.Children.Length; i++)
			if (this.Children[i].DoesEncompass(item))
				return i;
		throw new Exception("Element does not belong");
	}

	public TSelf FindContainingLeaf(T item) {
		var node = this;
		while (!node.DoesEncompass(item))
			node = node.IsRoot
				? throw new Exception("Uncontained")
				: node.Parent;
		while (!node.IsLeaf)
			node = node.Children[node.ChildIndex(item)];
		return (TSelf)node;
	}

	public void Add(T item) {
		var node = this;
		while (!node.DoesEncompass(item))
			node = node.IsRoot
				? node.Expand(item)
				: node.Parent;

		var startingNode = node;
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

	public TSelf Add(T[] items) {
		var parent = this.Root;
		for (int i = 0; i < items.Length; ++i) {
			var item = items[i];
			parent.Add(item);
			while (!parent.IsRoot)
				parent = parent.Parent;
		}
		return parent;
	}

	public bool Remove(T item, bool prune) {
		var node = this;
		while (!node.DoesEncompass(item))
			if (node.IsRoot)
				return false;
			else
				node = node.Parent;

		while (!node.IsLeaf) 
			node = node.Children[node.ChildIndex(item)];

		return node.RemoveFromLeaf(item, prune);
	}
	public bool Remove(T item) => this.Remove(item, true);

	public TSelf MoveFromLeaf(T item, bool prune = true) {
		var node = (TSelf)this;
		if (!node.DoesEncompass(item)) {
			RemoveFromNode(node, item);

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
			node = node.AddToLeaf(item);//increments the count
		}
		return node;
	}

	private static bool RemoveFromNode(TSelf node, T item) {
		//if (node.Bin.Count == 1)
		//	node.Bin.Clear();
		//else node.Bin.Remove(item);
		//return true;
		if (node.Bin.Remove(item)) {
			--node.ItemCount;
			return true;
		} else return false;
	}

	public bool RemoveFromLeaf(T item, bool prune = true) {
		var node = (TSelf)this;
		if (RemoveFromNode(node, item)) {
			while (!node.IsRoot) {
				node = node.Parent;
				--node.ItemCount;
				if (prune && node.ItemCount == 0)
					node.Children = null;
			}
			return true;
		} else return false;
	}

	protected TSelf AddToLeaf(T item) {//increments the count
		var node = this;
		while (!node.IsReceiving) {
			node.Refine();
			++node.ItemCount;
			node = node.Children[node.ChildIndex(item)];
		}
		node.Bin ??= [];
		node.Bin.Add(item);
		++node.ItemCount;
		return (TSelf)node;
	}

	private void Refine() {
		this.Children = this.FormSubnodes();
		for (int i = 0; i < this.Children.Length; i++)
			this.Children[i].ParentChildIndex = i;

		TSelf node;
		if (this.ItemCount == 1) {
			node = this.Children[this.ChildIndex(this.Bin[0])];
			++node.ItemCount;
			node.Bin = this.Bin;
		} else if (this.ItemCount > 1) {
			for (int i = 0; i < this.Bin.Count; i++) {
				var item = this.Bin[i];
				node = this.Children[this.ChildIndex(item)];
				++node.ItemCount;
				node.Bin ??= [];
				node.Bin.Add(item);
			}
		}
		this.Bin = null;
	}
	protected void SetChild(int i, TSelf child) {
		this.Children[i] = child;
		child.Parent = (TSelf)this;
		child.ParentChildIndex = i;
	}
		
	//finds the first sub node with more than one child
	public virtual TSelf PruneTop() {
		var node = this.Root;

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
		node.ParentChildIndex = -1;

		return node;
	}

	public void Clear() {
		this.ItemCount = 0;
		this.Children = null;
		this.Bin?.Clear();
	}

	public bool Contains(T item) {
		var node = this;
		bool encompasses = node.DoesEncompass(item);
		while (encompasses && !node.IsLeaf) {
			node = node.Children[node.ChildIndex(item)];
			encompasses = node.DoesEncompass(item);
		}

		return encompasses && node.ItemCount > 0
		                   && node.Bin.Contains(item);
	}

	public IEnumerator<T> GetEnumerator() => this.AsEnumerable().GetEnumerator();

	public void CopyTo(T[] array, int arrayIndex) {
		int i = 0;
		foreach (T item in this)
			array[i++ + arrayIndex] = item;
	}

	public IEnumerable<T> AsEnumerable() {
		Stack<TSelf> remaining = new();

		remaining.Push((TSelf)this);

		while (remaining.TryPop(out TSelf node))
			if (node.IsLeaf) {
				if (node.Bin is not null)
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
			Stack<TSelf> remaining = new();
			remaining.Push((TSelf)this);

			int idx = 0;
			while (remaining.TryPop(out TSelf node))
				if (node.IsLeaf) {
					if (node.Bin is not null)
						foreach (T item in node.Bin)
							result[idx++] = item;
				} else for (int i = 0; i < node.Children.Length; i++)
					if (node.Children[i].ItemCount > 0)
						remaining.Push(node.Children[i]);

			return result;
		} else return [];
	}
}