using System.Collections.Generic;
using ParticleSimulator.Simulation.Baryon;
using ParticleSimulator.Simulation.Particles;

namespace ParticleSimulator.Simulation {
	public interface ISimulator {
		int IterationCount { get; }
		int ParticleCount { get; }
		IEnumerable<IParticle> Particles { get; }
		BaryCenter Center { get; }

		void Init();

		List<ParticleData> Update();
	}
}