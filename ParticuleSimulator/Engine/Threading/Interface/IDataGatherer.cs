namespace ParticleSimulator.Engine.Threading {
	public interface IDataGatherer : IRunnable {
		object Value { get; }
	}
}