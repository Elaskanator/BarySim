namespace ParticleSimulator.Engine {
	public interface IDataGatherer : IRunnable {
		object Value { get; }
	}
}