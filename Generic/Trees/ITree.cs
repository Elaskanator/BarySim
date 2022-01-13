using System.Collections;
using System.Collections.Generic;

namespace Generic.Trees {
	public interface ITree : IEnumerable {
		ITree Parent { get; }
		IEnumerable<ITree> Children { get; }
		
		bool IsRoot { get; }
		bool IsLeaf { get; }
		IEnumerable AsEnumerable();
		IEnumerator IEnumerable.GetEnumerator() => this.AsEnumerable().GetEnumerator();
	}
}