using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Generic.Extensions;
using Generic.Models;
using Generic.Vectors;

namespace ParticleSimulator.Simulation {
	public class BaryonSimulator : ISimulator {//modified Barnes-Hut Algorithm
		public BaryonSimulator() {
			this.InitialParticleGroups = Enumerable
				.Range(0, Parameters.PARTICLES_GROUP_COUNT)
				.Select(i => new Galaxy())
				.ToArray();

			this.ParticleTree = (BarnesHutTree<Particle>)new BarnesHutTree<Particle>(Parameters.DIM)
				.AddUpOrDown(this.InitialParticleGroups.SelectMany(g => g.InitialParticles));
			this.ParticleTree.Do();
		}

		public Galaxy[] InitialParticleGroups { get; private set; }
		public BarnesHutTree<Particle> ParticleTree { get; private set; }

		public int ParticleCount => this.ParticleTree is null ? 0 : this.ParticleTree.Count;

		public virtual bool EnableCollisions => false;
		public virtual float WorldBounceWeight => 0f;

		public Queue<ParticleData> RefreshSimulation() {
			Queue<ParticleData> result = new Queue<ParticleData>();
			ParticleData pd;
			//Queue<BaryonParticle> particles = this.ParticleTree.AsQueue();
			//BaryonParticle particle;
			//while (particles.TryDequeue(out particle)) {
			foreach (Particle particle in this.ParticleTree.AsEnumerable()) {
				if (!Parameters.WORLD_BOUNCING || !particle.BounceWalls(Parameters.TIME_SCALE))
					particle.ApplyTimeStep(Vector<float>.Zero, Parameters.TIME_SCALE);
				//particle.HandleBounds(Parameters.TIME_SCALE);
				pd = new ParticleData(particle);
				result.Enqueue(pd);
			}
			return result;
		}
	}
}