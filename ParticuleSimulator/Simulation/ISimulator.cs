using System.Collections.Generic;
using ParticleSimulator.Simulation.Particles;

namespace ParticleSimulator.Simulation {
	public interface ISimulator {
		int ParticleCount { get; }
		IEnumerable<IParticle> Particles { get; }

		void Init();

		ParticleData[] RefreshSimulation();
	}
}