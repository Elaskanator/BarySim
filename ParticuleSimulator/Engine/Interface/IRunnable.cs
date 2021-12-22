using System;
using System.Collections.Generic;

namespace ParticleSimulator.Engine {
	public interface IRunnable : IDisposable, IEquatable<IRunnable>, IEqualityComparer<IRunnable> {
		int Id { get; }
		string Name { get; }

		bool IsOpen { get; }
		DateTime? StartTimeUtc { get; }
		DateTime? EndTimeUtc { get; }

		void Initialize();
		void Start();
		void Pause();
		void Resume();
		void Stop();
		void Restart();

		void Dispose(bool fromDispose);

		bool IEquatable<IRunnable>.Equals(IRunnable other) => !(other is null) && this.Id == other.Id;
		bool IEqualityComparer<IRunnable>.Equals(IRunnable x, IRunnable y) => x.Id == y.Id;
		int IEqualityComparer<IRunnable>.GetHashCode(IRunnable obj) => obj.Id.GetHashCode();
	}
}