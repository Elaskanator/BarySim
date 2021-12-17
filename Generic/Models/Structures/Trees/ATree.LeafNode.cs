using System;
using System.Collections.Generic;
using System.Linq;
using Generic.Extensions;

namespace Generic.Models {
	public abstract partial class ATree<TElement, TSelf>
	where TElement : IEquatable<TElement>, IEqualityComparer<TElement>
	where TSelf : ATree<TElement, TSelf> {
		public class LeafNode {
			public LeafNode(int maxCapacity = 1) {
				this.MaxCapacity = maxCapacity;
				this._members = new TElement[maxCapacity];
				this.Elements = Enumerable.Empty<TElement>();
			}

			public readonly int MaxCapacity;
			public IEnumerable<TElement> Elements { get; private set; }
			public int Count { get; private set; }

			internal TElement[] _members;
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

			//caller is responsible for ensuring the element is contained
			public void Remove(TElement element) {
				if (this._leftovers == null) {
					int i;
					for (i = 0; i < this.Count; i++)
						if (this._members[i].Equals(element))
							break;
					this._members = this._members.RemoveShift(i);
				} else this._leftovers.Remove(element);

				this.Count--;
			}
		}
	}
}