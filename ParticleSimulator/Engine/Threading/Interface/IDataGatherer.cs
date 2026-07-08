namespace ParticleSimulator.Engine.Threading.Interface;

public interface IDataGatherer : IRunnable {
	object Value { get; }
}