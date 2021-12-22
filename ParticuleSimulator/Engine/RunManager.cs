using System;
using System.Linq;
using Generic.Extensions;

namespace ParticleSimulator.Engine {
	public class RunManager : IRunnable {
		private static int _globalId = 0;

		public RunManager(params ACalculationHandler[] steps) { this.Evaluators = steps.Without(s => s is null).ToArray(); }

		~RunManager() => this.Dispose(false);

		public override string ToString() {
			return string.Format("{0}<{1}>[{2}]", nameof(RunManager),
				this.Evaluators.Length.Pluralize("step"),
				string.Join(", ", this.Evaluators.AsEnumerable()));//string.Join ambiguous without AsEnumerable() (C# you STOOOPID)
		}
		
		private readonly int _id = ++_globalId;
		public int Id => this._id;
		public string Name => "Run Manager";
		public bool IsOpen { get; private set; }
		public DateTime? StartTimeUtc { get; private set; }
		public DateTime? EndTimeUtc { get; private set; }
		public ACalculationHandler[] Evaluators { get; private set; }

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

		public void Dispose() => this.Dispose(true);
		public void Dispose(bool fromDispose) {
			if (fromDispose)
				for (int i = 0; i < this.Evaluators.Length; i++)
					this.Evaluators[i].Dispose(fromDispose);
		}
	}
}