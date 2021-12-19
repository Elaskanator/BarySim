using System.Collections.Generic;

namespace Generic.Models {
	public interface ILeafNode<TElement> {
		int MaxCapacity { get; }
		int Count { get; }

		IEnumerable<TElement> Elements { get; }

		void Add(object element);
		bool TryRemove(object element);
		//bool TryRemoveAll(IEnumerable<TElement> elements);
	}
}