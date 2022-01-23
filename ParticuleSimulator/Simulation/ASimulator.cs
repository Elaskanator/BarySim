using System.Collections.Generic;
using System.Linq;
using ParticleSimulator.Simulation.Baryon;
using ParticleSimulator.Simulation.Particles;

namespace ParticleSimulator.Simulation {
	public abstract class ASimulator<TParticle> : ISimulator
	where TParticle : AParticle<TParticle> {
		public ASimulator() {
			this.IterationCount = -1;
		}

		public int IterationCount { get; private set; }
		IEnumerable<IParticle> ISimulator.Particles => this.Particles;
		public int ParticleCount => this.Particles.Count;

		public abstract ICollection<TParticle> Particles { get; }
		public abstract BaryCenter Center { get; }

		protected abstract List<ParticleData> Refresh();
		protected abstract AParticleGroup<TParticle> NewParticleGroup();
		
		public abstract void Init();

		public List<ParticleData> Update() {
			++this.IterationCount;
			return this.IterationCount == 0//skip to show starting data on first result (TODO cleanup)
				? this.Particles.Select(p => new ParticleData(p)).ToList()
				: this.Refresh();
		}
	}
}
