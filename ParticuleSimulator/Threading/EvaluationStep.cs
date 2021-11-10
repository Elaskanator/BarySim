using System;
using Generic.Models;

namespace ParticleSimulator.Threading {
	public struct EvaluationStep {
		public string Name;

		public Func<object> Initializer;
		public Action<object[]> Evaluator;
		public Func<object[], object> Calculator;

		public SynchronizedDataBuffer OutputResource;
		public Action<StepEvaluator> Callback;//contains result data
		public bool IsOutputOverwrite;
		public int OutputSkips;

		public Prerequisite[] InputResourceUses;
		public TimeSpan? DataLoadingTimeout;
		public TimeSynchronizer Synchronizer;

		public AIncrementalAverage<double> DataAssimilationTicksAverager;
		public AIncrementalAverage<double> SynchronizationTicksAverager;
		public AIncrementalAverage<double> IterationTicksAverager;
	}
}