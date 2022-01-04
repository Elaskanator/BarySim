using System.Collections.Generic;

namespace ParticleSimulator.Simulation {
	public interface ISimulator {
		int ParticleCount { get; }
		IEnumerable<Particle> Particles { get; }

		void Init();

		ParticleData[] RefreshSimulation();
	}
}