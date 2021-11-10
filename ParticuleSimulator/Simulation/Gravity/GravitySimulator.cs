using System;
using System.Collections.Generic;
using System.Linq;
using Generic.Extensions;

namespace ParticleSimulator.Simulation.Gravity {
	public class GravitySimulator : AParticleSimulator<CelestialBody, BaryonQuadTree> {
		public GravitySimulator(Random rand = null) : base(rand) {
			this.Clusterings = Enumerable.Range(0, Parameters.NUM_PARTICLE_GROUPS).Select(i => new Clustering(this._rand)).ToArray();
		}

		public override bool IsDiscrete => false;
		public Clustering[] Clusterings { get; private set; }
		public override IEnumerable<CelestialBody> AllParticles => this.Clusterings.SelectMany(c => c.Particles);
		public override BaryonQuadTree NewTree => new BaryonQuadTree(new double[Parameters.DOMAIN_DOUBLE.Length], Parameters.DOMAIN_DOUBLE);

		protected override void ComputeUpdate(BaryonQuadTree tree) {
			BaryonQuadTree[] nodes;
			foreach (BaryonQuadTree leaf in tree.Leaves) {
				nodes = leaf.GetRefinedNeighborNodes(3).Cast<BaryonQuadTree>().ToArray();
				foreach (CelestialBody b in leaf.NodeElements)
					foreach (BaryonQuadTree otherNode in nodes) {
						if (otherNode.IsLeaf)
							foreach (CelestialBody b2 in otherNode.NodeElements)
								if (b.ID != b2.ID)
									b.Interact(b2);
						else
							b.Interact(otherNode);
					}
			}
		}

		protected override Tuple<char, double>[] Resample(Tuple<double[], double>[] particles) {
			throw new NotImplementedException();
		}
	}
}