using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Generic.Extensions;
using ParticleSimulator.Rendering;

namespace ParticleSimulator.Simulation.Boids {
	public class BoidSimulator : AParticleSimulator<Boid, BoidQuadTree> {
		public BoidSimulator(Random rand = null) : base(rand) {
			this.Flocks = Enumerable.Range(0, Parameters.NUM_PARTICLE_GROUPS).Select(i => new Flock(this._rand)).ToArray();
		}

		public override bool IsDiscrete => true;
		public Flock[] Flocks { get; private set; }
		public override IEnumerable<Boid> AllParticles => this.Flocks.SelectMany(f => f.Particles);
		public override BoidQuadTree NewTree => new BoidQuadTree(new double[Parameters.DOMAIN_DOUBLE.Length], Parameters.DOMAIN_DOUBLE);

		protected override void ComputeUpdate(BoidQuadTree tree) {
			Parallel.ForEach(tree.Leaves, leaf => {
				foreach (Boid b in leaf.AllElements)
					b.UpdateDeltas(leaf.GetNeighbors()); });
		}
	}
}