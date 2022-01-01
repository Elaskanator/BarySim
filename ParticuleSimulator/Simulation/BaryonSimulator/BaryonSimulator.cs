using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Generic.Extensions;
using Generic.Models;
using Generic.Vectors;

namespace ParticleSimulator.Simulation.Baryon {
	public class BaryonSimulator : ISimulator {//modified Barnes-Hut Algorithm
		public BaryonSimulator() { }

		public Galaxy[] InitialParticleGroups { get; private set; }
		//public BarnesHutTree ParticleTree { get; private set; }
		public Particle[] Particles { get; private set; }

		public int ParticleCount => this.Particles is null ? 0 : this.Particles.Length;//this.ParticleTree is null ? 0 : this.ParticleTree.Count;

		public virtual bool EnableCollisions => false;
		public virtual float WorldBounceWeight => 0f;

		public void Init() {
			this.InitialParticleGroups = Enumerable
				.Range(0, Parameters.PARTICLES_GROUP_COUNT)
				.Select(i => new Galaxy())
				.ToArray();

			this.Particles = this.InitialParticleGroups.SelectMany(g => g.InitialParticles).ToArray();
			if (Parameters.WORLD_BOUNCING)
				for (int i = 0; i < this.ParticleCount; i++)
					this.Particles[i].WrapPosition();

			//this.ParticleTree = (BarnesHutTree)new BarnesHutTree(Parameters.DIM)
			//	.AddUpOrDown(this.InitialParticleGroups.SelectMany(g => g.InitialParticles));
			//this.ParticleTree.Do();
		}

		public ParticleData[] RefreshSimulation() {
			ParticleData[] result = new	ParticleData[this.ParticleCount];
			for (int i = 0; i < this.ParticleCount; i++) {
				this.Particles[i].ApplyTimeStep(Vector<float>.Zero, Parameters.TIME_SCALE);
				result[i] = new(this.Particles[i]);
			}

			/*
			ParticleData pd;
			//Queue<BaryonParticle> particles = this.ParticleTree.AsQueue();
			//BaryonParticle particle;
			//while (particles.TryDequeue(out particle)) {
			int i = 0;
			foreach (Particle particle in this.ParticleTree.AsEnumerable()) {
				if (!Parameters.WORLD_BOUNCING || !particle.BounceWalls(Parameters.TIME_SCALE))
					particle.ApplyTimeStep(Vector<float>.Zero, Parameters.TIME_SCALE);
				//particle.HandleBounds(Parameters.TIME_SCALE);
				pd = new ParticleData(particle);
				result[i++] = pd;
			}
			*/
			return result;
		}
	}
}