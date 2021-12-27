using System;
using Generic.Models;

namespace ParticleSimulator.Engine {
	public struct EvaluationStep {
		public string Name;

		public Func<object> GeneratorFn;
		public Action<bool, object[]> EvaluatorFn;
		public Func<bool, object[], object> CalculatorFn;

		public ISynchronousConsumedResource OutputResource;
		public Action<bool> CallbackFn;//whether calculation was punctual
		public bool IsOutputOverwrite;
		public int OutputSkips;

		public IPrerequisite[] InputResourceUses;
		public TimeSpan? DataLoadingTimeout;
		public TimeSynchronizer Synchronizer;

		public override string ToString() => string.Format("EvaluationStep[{0}]", this.Name);
	}
}