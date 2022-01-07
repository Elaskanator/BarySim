using System.Collections.Generic;
using Generic.Models.Trees;
using ParticleSimulator.Simulation.Particles;

namespace ParticleSimulator.Simulation {
	public interface ISimulator {
		int IterationCount { get; }
		int ParticleCount { get; }
		ITree ParticleTree { get; }
		IEnumerable<IParticle> Particles { get; }

		void Init();

		ParticleData[] RefreshSimulation();
	}
}