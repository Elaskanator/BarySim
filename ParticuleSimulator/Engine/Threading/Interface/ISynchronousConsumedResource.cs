using System;
using System.Collections.Generic;
using System.Threading;

namespace ParticleSimulator.Engine {
	public interface ISynchronousConsumedResource : IDisposable, IEquatable<ISynchronousConsumedResource>, IEqualityComparer<ISynchronousConsumedResource>
	//ICollection, IEnumerable,
	//IProducerConsumerCollection<object>, IReadOnlyCollection<object>,
	{
		int Id { get; }
		string Name { get; }
		Type DataType { get; }

		object Current { get; }

		void Enqueue(object item);
		void Overwrite(object item);

		object Peek();
		object Dequeue();

		AutoResetEvent AddRefreshListener();
		//AutoResetEvent[] RefreshListeners { get; }

		bool Equals(object other) => !(other is null) && (other is ISynchronousConsumedResource) && this.Id == (other as ISynchronousConsumedResource).Id;
		int GetHashCode() => this.Id.GetHashCode();

		bool IEquatable<ISynchronousConsumedResource>.Equals(ISynchronousConsumedResource other) => !(other is null) && this.Id == other.Id;
		bool IEqualityComparer<ISynchronousConsumedResource>.Equals(ISynchronousConsumedResource x, ISynchronousConsumedResource y) => x.Id == y.Id;
		int IEqualityComparer<ISynchronousConsumedResource>.GetHashCode(ISynchronousConsumedResource obj) => obj.Id.GetHashCode();
	}
}