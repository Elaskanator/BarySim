using System;
using System.Linq;
using System.Threading.Tasks;

namespace ParticleSimulator.Simulation.Gravity {
	public class GravitySimulator : ASymmetricalInteractionParticleSimulator<CelestialBody, Clustering, BaryonQuadTree> {
		public GravitySimulator(Random rand = null) : base(rand) { }

		public override bool IsDiscrete => false;
		public override int? NeighborhoodFilteringDepth => Parameters.QUADTREE_NEIGHBORHOOD_FILTER;
		public override BaryonQuadTree NewTree => new BaryonQuadTree(new double[Parameters.DOMAIN.Length], Parameters.DOMAIN);
		public override Clustering NewParticleGroup(Random rand) { return new Clustering(rand); }

		protected override void InteractTree(BaryonQuadTree tree) {
			Parallel.ForEach(tree.Leaves, leaf => {
				BaryonQuadTree[] nodes = leaf.GetRefinedNeighborNodes(this.NeighborhoodFilteringDepth).Cast<BaryonQuadTree>().ToArray();
				foreach (CelestialBody b in leaf.NodeElements)
					foreach (BaryonQuadTree otherNode in nodes) {
						if (otherNode.IsLeaf)
							foreach (CelestialBody b2 in otherNode.NodeElements)
								if (b.ID != b2.ID)
									b.Interact(b2);
						else
							b.InteractSubtree(otherNode);
					}});
		}

		protected override Tuple<char, double>[] ResampleDensities(Tuple<double[], object>[] particles) {
			throw new NotImplementedException();
		}
	}
}