using System;
using System.Linq;
using Generic.Extensions;

namespace ParticleSimulator.Threading {
	public class RunManager : IDisposable {
		public readonly StepEvaluator[] Evaluators;
		public DateTime StartTimeUtc { get; private set; }
		public DateTime EndTimeUtc { get; private set; }

		public RunManager(params StepEvaluator[] steps) {
			this.Evaluators = steps;
		}
		public RunManager(params EvaluationStep[] steps)
		: this(steps.Select(s => new StepEvaluator(s)).ToArray()) { }

		public void Start() {
			this.StartTimeUtc = DateTime.UtcNow;
			foreach (StepEvaluator evaluator in this.Evaluators)
				evaluator.Start();
		}
		public void Dispose() {
			this.EndTimeUtc = DateTime.UtcNow;
			foreach (StepEvaluator evaluator in this.Evaluators)
				evaluator.Dispose();
		}

		public override string ToString() {
			return string.Format("{0}<{1}>[{2}]", nameof(RunManager),
				this.Evaluators.Length.Pluralize("step"),
				string.Join(", ", this.Evaluators.AsEnumerable()));//string.Join ambiguous without AsEnumerable() (C# you STOOOPID)
		}
	}
}