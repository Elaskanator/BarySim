using System;
using ParticleSimulator.Engine.Threading.Interface;

namespace ParticleSimulator.Engine.Threading.Configs;

public struct EvaluationStep {
	public string Name;

	public Action InitFn;
	public Func<object> GeneratorFn;
	public Action PreProcessFn;
	public Action<EvalResult, object[]> EvaluatorFn;
	public Func<EvalResult, object[], object> CalculatorFn;

	public ISynchronousConsumedResource OutputResource;
	public Action<EvalResult> CallbackFn;//whether calculation was punctual
	public bool IsOutputOverwrite;
	public int OutputSkips;

	public IIngestedResource[]? InputResourceUses;
	public TimeSpan? DataLoadingTimeout;
	public TimeSynchronizer? Synchronizer;

	public override readonly string ToString() => string.Format("EvaluationStep[{0}]", this.Name);
}