//using System;
//using System.Linq;
//using System.Threading.Tasks;

//namespace ParticleSimulator.Simulation.Gravity {
//	public class GravitySimulator : AParticleSimulator<CelestialBody, Clustering> {
//		public GravitySimulator(Random rand = null) : base(rand) { }

//		public override bool IsDiscrete => false;
//		public override int? NeighborhoodFilteringDepth => Parameters.NEIGHBORHOOD_FILTER;
//		public override Clustering NewParticleGroup(Random rand) { return new Clustering(rand); }

//		protected override void InteractTree(ParticleTree<CelestialBody> tree) {
//			Parallel.ForEach(tree.Leaves, leaf => {
//				ParticleTree<CelestialBody>[] nodes = leaf.GetRefinedNeighborNodes(this.NeighborhoodFilteringDepth).Cast<ParticleTree<CelestialBody>>().ToArray();
//				foreach (CelestialBody b in leaf.NodeElements)
//					foreach (ParticleTree<CelestialBody> otherNode in nodes) {
//						if (otherNode.IsLeaf)
//							foreach (CelestialBody b2 in otherNode.NodeElements)
//								if (b.ID != b2.ID)
//									b.Interact(b2);
//						else
//							b.InteractSubtree(otherNode);
//					}});
//		}

//		protected override Tuple<char, double>[] ResampleDensities(Tuple<double[], AParticle>[] particles) {
//			throw new NotImplementedException();
//		}
//	}
//}