using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Generic.Extensions;

namespace ParticleSimulator.Simulation.Gravity {
	public class GravitySimulator : ASymmetricalInteractionParticleSimulator<CelestialBody, BaryonQuadTree> {
		public GravitySimulator(Random rand = null) : base(rand) {
			this.Clusterings = Enumerable.Range(0, Parameters.NUM_PARTICLE_GROUPS).Select(i => new Clustering(this._rand)).ToArray();
		}

		public override bool IsDiscrete => false;
		public Clustering[] Clusterings { get; private set; }
		public override IEnumerable<CelestialBody> AllParticles => this.Clusterings.SelectMany(c => c.Particles);
		public override BaryonQuadTree NewTree => new BaryonQuadTree(new double[Parameters.DOMAIN.Length], Parameters.DOMAIN);

		protected override void InteractTree(BaryonQuadTree tree) {
			BaryonQuadTree[] nodes;
			Parallel.ForEach(tree.Leaves, leaf => {
				nodes = leaf.GetRefinedNeighborNodes(Parameters.QUADTREE_NEIGHBORHOOD_FILTER).Cast<BaryonQuadTree>().ToArray();
				foreach (CelestialBody b in leaf.NodeElements)
					foreach (BaryonQuadTree otherNode in nodes) {
						if (otherNode.IsLeaf)
							foreach (CelestialBody b2 in otherNode.NodeElements)
								if (b.ID != b2.ID)
									b.Interact(b2);
						else
							b.InteractNode(otherNode);
					}});
		}

		protected override Tuple<char, double>[] Resample(Tuple<double[], double>[] particles) {
			throw new NotImplementedException();
		}
	}
}