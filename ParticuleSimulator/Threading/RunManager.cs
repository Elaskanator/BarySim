using System;
using System.Linq;
using Generic.Extensions;

namespace ParticleSimulator.Threading {
	public class RunManager : IDisposable {
		public readonly StepEvaluator[] Evaluators;
		public DateTime StartTimeUtc { get; private set; }
		public DateTime EndTimeUtc { get; private set; }

		public RunManager(params StepEvaluator[] steps) {
			this.Evaluators = steps.Without(s => s is null).ToArray();
		}
		public RunManager(params EvaluationStep[] steps)
		: this(steps.Without(s => Equals(s, default(EvaluationStep))).Select(s => new StepEvaluator(s)).ToArray()) { }

		public void Start() {
			this.StartTimeUtc = DateTime.UtcNow;
			foreach (StepEvaluator evaluator in this.Evaluators)
				evaluator.Start();
		}
		public void Stop () {
			this.EndTimeUtc = DateTime.UtcNow;
			foreach (StepEvaluator evaluator in this.Evaluators)
				evaluator.Stop();
		}
		public void Dispose() {
			foreach (SynchronizedDataBuffer resource in this.Evaluators
				.SelectMany(s => s.Step.InputResourceUses.Select(ir => ir.Resource))
				.Concat(this.Evaluators.Select(s => s.Step.OutputResource))
				.Without(r => r is null)
				.Distinct())
				resource.Dispose();
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