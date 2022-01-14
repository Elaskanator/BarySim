using System.Collections.Generic;
using System.Numerics;
using ParticleSimulator.Simulation.Particles;

namespace ParticleSimulator.Simulation {
	public interface ISimulator {
		int IterationCount { get; }
		int ParticleCount { get; }
		IEnumerable<IParticle> Particles { get; }
		Vector<float> Center { get; }

		void Init();

		ParticleData[] RefreshSimulation();
	}
}