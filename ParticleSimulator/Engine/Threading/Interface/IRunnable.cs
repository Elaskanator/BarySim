using System;
using System.Collections.Generic;

namespace ParticleSimulator.Engine.Threading.Interface;

public interface IRunnable : IDisposable, IEquatable<IRunnable>, IEqualityComparer<IRunnable> {
	int Id { get; }
	string? Name { get; }

	bool IsOpen { get; }
	DateTime? StartTimeUtc { get; }
	DateTime? EndTimeUtc { get; }

	void Start(bool running = true);
	void Pause();
	void SetRunningState(bool running);
	void Resume();
	void Stop();
	void Restart(bool running = true);

	void Dispose(bool fromDispose);

	bool IEquatable<IRunnable>.Equals(IRunnable? other) => other is not null && this.Id == other.Id;
	bool IEqualityComparer<IRunnable>.Equals(IRunnable? x, IRunnable? y) => (x is null && y is null) || (x is not null && y is not null && x.Id == y.Id);
	int IEqualityComparer<IRunnable>.GetHashCode(IRunnable obj) => obj.Id.GetHashCode();
}