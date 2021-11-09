using System;
using System.Linq;
using System.Threading.Tasks;

using Generic.Extensions;

namespace ParticleSimulator.Threading {
	public class RunManager :IDisposable {
		public readonly AEvaluationStep[] Steps;
		public bool IsActive { get; private set; }
		public DateTime StartTimeUtc { get; private set; }
		public DateTime EndTimeUtc { get; private set; }

		public RunManager(params AEvaluationStep[] runners) {
			this.Steps = (runners ?? Array.Empty<AEvaluationStep>()).Except(r => r is null).ToArray();
		}

		public void Start() {
			this.IsActive = true;
			this.StartTimeUtc = DateTime.UtcNow;
			Parallel.ForEach(this.Steps, r => r.Start());
		}
		public void Dispose() {
			this.IsActive = false;
			this.EndTimeUtc = DateTime.UtcNow;
			Parallel.ForEach(this.Steps, r => r.Dispose());
			GC.SuppressFinalize(this);
		}

		public override string ToString() {
			return string.Format("{0}<{1}>[{2}]", nameof(RunManager),
				this.Steps.Length.Pluralize("step"),
				string.Join(", ", this.Steps.AsEnumerable()));//string.Join ambiguous without AsEnumerable() (C# you STOOOPID)
		}
	}
}