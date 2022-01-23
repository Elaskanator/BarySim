using System.Collections.Generic;
using System.Linq;
using ParticleSimulator.Simulation.Baryon;
using ParticleSimulator.Simulation.Particles;

namespace ParticleSimulator.Simulation {
	public abstract class ASimulator<TParticle> : ISimulator
	where TParticle : AParticle<TParticle> {
		public ASimulator() {
			this.IterationCount = -1;
			//initialize all the particles
			this.ParticleGroups = new AParticleGroup<TParticle>[Parameters.PARTICLES_GROUP_COUNT];
			for (int i = 0; i < this.ParticleGroups.Length; i++) {
				this.ParticleGroups[i] = this.NewParticleGroup();
				this.ParticleGroups[i].Init();
			}
		}

		public int IterationCount { get; private set; }
		public AParticleGroup<TParticle>[] ParticleGroups { get; private set; }
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
