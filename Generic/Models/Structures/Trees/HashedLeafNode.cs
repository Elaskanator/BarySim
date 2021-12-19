using System.Collections.Generic;
using System.Linq;
using Generic.Extensions;

namespace Generic.Models {
	public class HashedLeafNode<TElement> : ILeafNode<TElement> {
		public HashedLeafNode(int maxCapacity = 1) {
			this.MaxCapacity = maxCapacity;
			this._members = new TElement[maxCapacity];
			this.Elements = Enumerable.Empty<TElement>();
		}

		public int MaxCapacity { get; private set; }
		public int Count { get; private set; }
		public IEnumerable<TElement> Elements { get; private set; }

		public TElement[] _members;
		private HashSet<TElement> _leftovers = null;

		public void Add(TElement element) {
			if (this._leftovers is null && this.Count < this.MaxCapacity) {
				this._members[this.Count] = element;
				this.Elements = this._members.Take(this.Count - 1);
			} else {
				if (this._leftovers is null) {
					this._leftovers = new HashSet<TElement>(this._members);
					this.Elements = this._leftovers;
					this._members = null;
				}
				this._leftovers.Add(element);
			}
			this.Count++;
		}
		public void Add(object element) { this.Add((TElement)element); }

		//caller is responsible for ensuring the element is contained
		public bool TryRemove(TElement element) {
			bool found = false;
			if (this._leftovers == null) {
				int i;
				for (i = 0; i < this.Count; i++)
					if ((found = this._members[i].Equals(element)))
						break;
				if (found) {
					this._members = this._members.RemoveShift(i);
					this.Count--;
				}
			} else if ((found = this._leftovers.Remove(element))) this.Count--;

			return found;
		}
		public bool TryRemove(object element) { return this.TryRemove((TElement)element); }

		//public bool TryRemoveAll(IEnumerable<TElement> elements) {
		//	if (this.Count == 0) return true;
		//	else this._leftovers ??= new();
				
		//	foreach (TElement element in elements)
		//		if (this._leftovers.Remove(element))
		//			this.Count--;
		//	return this.Count == 0;
		//}
	}
}