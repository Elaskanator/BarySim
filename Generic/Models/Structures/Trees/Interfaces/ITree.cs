using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Generic.Models {
	public interface ITree : IEnumerable {
		bool IsRoot { get; }
		bool IsLeaf { get; }
		int Count { get; }

		IEnumerable AllElements { get; }
		IEnumerable AllNodes { get; }
		IEnumerable Children { get; }

		ITree Parent { get; }

		IEnumerable LeafNodes { get; }
		IEnumerable LeafNodesNonEmpty { get; }

		IEnumerable NestedChildren { get; }
		IEnumerable SiblingNodes { get; }

		void Add(object element);
		void AddRange(IEnumerable elements);

		ITree GetContainingLeaf(object element);
		ITree GetContainingLeafUnchecked(object element);

		IEnumerator IEnumerable.GetEnumerator() { return this.AllElements.GetEnumerator(); }
	}

	public interface ITree<TElement, TSelf> : ITree, IEnumerable<TElement>
	where TSelf : ITree<TElement, TSelf> {
		new IEnumerable<TElement> AllElements { get; }
		IEnumerable ITree.AllElements => this.AllElements;

		new IEnumerable<TSelf> AllNodes { get; }
		IEnumerable ITree.AllNodes => this.AllNodes;

		new IEnumerable<TSelf> Children { get; }
		IEnumerable ITree.Children => this.Children;

		new TSelf Parent { get; }
		ITree ITree.Parent => this.Parent;

		new IEnumerable<TSelf> LeafNodes { get; }
		IEnumerable ITree.LeafNodes => this.LeafNodes;
		new IEnumerable<TSelf> LeafNodesNonEmpty { get; }
		IEnumerable ITree.LeafNodesNonEmpty => this.LeafNodesNonEmpty;

		new IEnumerable<TSelf> NestedChildren { get; }
		IEnumerable ITree.NestedChildren => this.NestedChildren;
		new IEnumerable<TSelf> SiblingNodes { get; }
		IEnumerable ITree.SiblingNodes => this.SiblingNodes;

		void Add(TElement element);
		void ITree.Add(object element) => this.Add((TElement)element);
		void AddRange(IEnumerable<TElement> elements) { foreach (TElement e in elements) this.Add(e); }
		void ITree.AddRange(IEnumerable elements) => this.AddRange(elements.Cast<TElement>());

		TSelf GetContainingLeaf(TElement element);
		ITree ITree.GetContainingLeaf(object element) => this.GetContainingLeaf((TElement)element);
		TSelf GetContainingLeafUnchecked(TElement element);
		ITree ITree.GetContainingLeafUnchecked(object element) => this.GetContainingLeafUnchecked((TElement)element);

		IEnumerator<TElement> IEnumerable<TElement>.GetEnumerator() => this.AllElements.GetEnumerator();
	}
}