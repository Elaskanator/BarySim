using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParticleSimulator.Engine.Threading {
	public class EvalResult {
		public bool PrepPunctual;
		public TimeSpan PrepTime;
		public TimeSpan? PauseDelay;
		public TimeSpan? SyncDelay;
		public TimeSpan ExclusiveTime;
		public TimeSpan TotalTime;
		public TimeSpan? TotalTimePunctual;
	}
}