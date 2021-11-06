using System;
using System.Linq;
using System.Threading.Tasks;
using Generic;

namespace Boids {
	public class RunManager {
		public readonly AEvaluationStep[] Steps;
		public bool IsActive { get; private set; }
		public DateTime StartTime { get; private set; }
		public DateTime EndTime { get; private set; }

		public RunManager(params AEvaluationStep[] runners) {
			this.Steps = (runners ?? Array.Empty<AEvaluationStep>()).Except(r => r is null).ToArray();
		}

		public void Start() {
			this.IsActive = true;
			this.StartTime = DateTime.Now;
			Parallel.ForEach(this.Steps, r => r.Start());
		}
		public void Stop() {
			this.IsActive = false;
			this.EndTime = DateTime.Now;
			Parallel.ForEach(this.Steps, r => r.Dispose());
		}

		public override string ToString() {
			return string.Format("{0}<{1}>[{2}]", nameof(RunManager),
				this.Steps.Length.Pluralize("step"),
				string.Join(", ", this.Steps.AsEnumerable()));//string.Join ambiguous without AsEnumerable() (C# you STOOOPID)
		}
	}
}