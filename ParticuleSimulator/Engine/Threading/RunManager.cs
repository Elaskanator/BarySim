using System;
using System.Linq;
using Generic.Extensions;

namespace ParticleSimulator.Engine {
	public class RunManager : IRunnable {
		public RunManager(params ProcessThread[] steps) { this.Evaluators = steps.Without(s => s is null).ToArray(); }
		public RunManager(params EvaluationStep[] steps)
		: this(steps.Without(s => Equals(s, default(EvaluationStep))).Select(s => ProcessThread.New(s)).ToArray()) { }

		public override string ToString() {
			return string.Format("{0}<{1}>[{2}]", nameof(RunManager),
				this.Evaluators.Length.Pluralize("step"),
				string.Join(", ", this.Evaluators.AsEnumerable()));//string.Join ambiguous without AsEnumerable() (C# you STOOOPID)
		}

		public bool IsOpen { get; private set; }
		public DateTime? StartTimeUtc { get; private set; }
		public DateTime? EndTimeUtc { get; private set; }
		public ProcessThread[] Evaluators { get; private set; }

		public void Start() {
			if (this.IsOpen) {
				throw new InvalidOperationException("Already open");
			} else {
				this.IsOpen = true;
				this.StartTimeUtc = DateTime.UtcNow;
				for (int i = 0; i < this.Evaluators.Length; i++)
					this.Evaluators[i].Start();
			}
		}

		public void Pause() {
			if (this.IsOpen) {
				for (int i = 0; i < this.Evaluators.Length; i++)
					this.Evaluators[i].Pause();
			} else throw new InvalidOperationException("Not open");
		}

		public void Resume() {
			if (this.IsOpen) {
				for (int i = 0; i < this.Evaluators.Length; i++)
					this.Evaluators[i].Resume();
			} else throw new InvalidOperationException("Not open");
		}

		public void Stop () {
			if (this.IsOpen) {
				this.EndTimeUtc = DateTime.UtcNow;
				for (int i = 0; i < this.Evaluators.Length; i++)
					this.Evaluators[i].Stop();
			} else throw new InvalidOperationException("Not open");
		}

		public void Restart() {
			if (this.IsOpen) {
				for (int i = 0; i < this.Evaluators.Length; i++)
					this.Evaluators[i].Restart();
			} else throw new InvalidOperationException("Not open");
		}
	}
}