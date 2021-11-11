using System;
using Generic.Models;
using Generic.Extensions;

namespace ParticleSimulator.Simulation {
	public abstract class ASimulationQuadTree<P, T> : AQuadTree<P, T>
	where P : AParticle
	where T : ASimulationQuadTree<P, T>{
		public override int Capacity => Parameters.QUADTREE_NODE_CAPACITY;

		public ASimulationQuadTree(double[] corner1, double[] corner2, T parent = null)
		: base(corner1, corner2, parent) { }

		protected override T[] OrganizeNodes(T[] nodes, Random rand) {
			rand.ShuffleInPlace(nodes);
			return nodes;
		}
	}
}
