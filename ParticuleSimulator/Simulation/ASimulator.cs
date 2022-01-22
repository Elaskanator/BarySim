using System.Collections.Generic;
using System.Linq;
using ParticleSimulator.Simulation.Baryon;
using ParticleSimulator.Simulation.Particles;

namespace ParticleSimulator.Simulation {
	public abstract class ASimulator<TParticle> : ISimulator
	where TParticle : AParticle<TParticle> {
		public int IterationCount { get; private set; }
		public AParticleGroup<TParticle>[] ParticleGroups { get; private set; }
		IEnumerable<IParticle> ISimulator.Particles => this.Particles;
		public int ParticleCount => this.Particles.Count;

		public abstract ICollection<TParticle> Particles { get; }
		public abstract BaryCenter Center { get; }

		protected abstract void Refresh();
		protected abstract AParticleGroup<TParticle> NewParticleGroup();
		
		public void Init() {
			this.IterationCount = -1;
			//initialize all the particles
			this.ParticleGroups = new AParticleGroup<TParticle>[Parameters.PARTICLES_GROUP_COUNT];
			for (int i = 0; i < this.ParticleGroups.Length; i++) {
				this.ParticleGroups[i] = this.NewParticleGroup();
				this.ParticleGroups[i].Init();
			}

			foreach (TParticle particle in this.ParticleGroups.SelectMany(g => g.InitialParticles))
				this.Particles.Add(particle);
		}

		public ParticleData[] Update() {
			if (++this.IterationCount > 0)//skip to show starting data on first result (TODO cleanup)
				this.Refresh();
			//return a deep copy of the particle data for rendering so simulation can continue concurrently
			return this.Particles.Select(p => new ParticleData(p)).ToArray();
		}
	}
}
