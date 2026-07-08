using System;
using ParticleSimulator.Engine.Threading.Classes;
using ParticleSimulator.Engine.Threading.Interface;

namespace ParticleSimulator.Engine.Threading.Configs;

public interface IIngestedResource {
	ISynchronousConsumedResource Resource { get; }

	ConsumptionType ReadType { get; set; }
	TimeSpan? ReadTimeout { get; set; }
	int ReuseAmount { get; set; }
	int ReuseTolerance { get; set; }
}

public class IngestedResource<T>(SynchronousBuffer<T> resource, ConsumptionType readType) : IIngestedResource {
	public SynchronousBuffer<T> Resource { get; private set; } = resource;
	ISynchronousConsumedResource IIngestedResource.Resource => this.Resource;

	public ConsumptionType ReadType { get; set; } = readType;
	public TimeSpan? ReadTimeout { get; set; }
	//negative means unlimited
	public int ReuseAmount { get; set; }
	public int ReuseTolerance { get; set; }

	public override string ToString() => string.Format("Prerequisite<{0}>[ReadType {1} Timeout {2} Reuses {3} Slip {4}]",
		this.Resource,
		this.ReadType,
		this.ReadTimeout,
		this.ReuseAmount,
		this.ReuseTolerance);
}