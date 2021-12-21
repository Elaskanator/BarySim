using System;

namespace ParticleSimulator.Engine {
	public interface IRunnable {
		bool IsOpen { get; }
		DateTime? StartTimeUtc { get; }
		DateTime? EndTimeUtc { get; }

		void Start();
		void Pause();
		void Resume();
		void Stop();
		void Restart();
	}
}