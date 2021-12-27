namespace ParticleSimulator.Simulation {
	public interface ISimulator {
		int ParticleCount { get; }

		void Init();

		ParticleData[] RefreshSimulation();
	}
}