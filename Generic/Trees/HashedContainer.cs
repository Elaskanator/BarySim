using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Generic.Extensions;

namespace Generic.Models.Trees {
	public class HashedContainer<T> : ICollection<T> {
		public HashedContainer(int maxCapacity = 1) {
			this.MaxCapacity = maxCapacity;
			this._members = new T[maxCapacity];
			this.Items = Enumerable.Empty<T>();
		}

		public int MaxCapacity { get; private set; }
		public int Count { get; private set; }
		public IEnumerable<T> Items { get; private set; }
		public bool IsReadOnly => false;

		public T[] _members;
		private HashSet<T> _leftovers = null;

		public void Add(T item) {
			if (this._leftovers is null && this.Count < this.MaxCapacity) {
				this._members[this.Count] = item;
				T[] newItems = new T[this.Count + 1];
				Array.Copy(this._members, newItems, this.Count + 1);
				this.Items= newItems;
				this.Items = this._members.Take(this.Count + 1);
			} else {
				if (this._leftovers is null) {
					this._leftovers = new HashSet<T>(this._members);
					this.Items = this._leftovers;
					Array.Clear(this._members, 0, this._members.Length);//should help with garbage collection?
				}
				this._leftovers.Add(item);
			}
			this.Count++;
		}
		public void Add(object item) { this.Add((T)item); }

		public bool Remove(T item) {
			if (this._leftovers is null) {
				for (int i = 0; i < this.Count; i++)
					if (this._members[i].Equals(item)) {
						this._members = this._members.RemoveShift(i);
						this.Count--;
						return true;
					}
			} else if (this._leftovers.Remove(item)) {
				this.Count--;
				return true;
			}
			return false;
		}

		public void Clear() {
			this._members = new T[this.MaxCapacity];
			if (!(this._leftovers is null))
				this._leftovers.Clear();
		}

		public bool Contains(T item) {
			if (this._leftovers is null) {
				for (int i = 0; i < this.Count; i++)
					if (this._members[i].Equals(item))
						return true;
				return false;
			} else return this._leftovers.Contains(item);;
		}

		public void CopyTo(T[] array, int outOffset = 0) {
			if (this._leftovers is null)
				if (!(this._members is null))
					for (int i = 0; i < this.Count; i++)
						array[i + outOffset] = this._members[i];
			else this._leftovers.CopyTo(array, outOffset);
		}

		public IEnumerator<T> GetEnumerator() => this.Items.GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => this.Items.GetEnumerator();

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