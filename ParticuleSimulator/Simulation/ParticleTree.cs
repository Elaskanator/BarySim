using System.Linq;
using Generic.Models;
using Generic.Extensions;

namespace ParticleSimulator.Simulation {
	public class ParticleTree<P> : AQuadTree<P, ParticleTree<P>>
	where P : AParticle {
		//public WeightedIncrementalVectorAverage Barycenter { get; private set; }
		//public double TotalMass { get; private set; }
		public override int Capacity => Parameters.QUADTREE_NODE_CAPACITY;

		public ParticleTree(P[] particles, double[] corner1 = null, double[] corner2 = null, ParticleTree<P> parent = null)
		: base(corner1 ?? new double[Parameters.DOMAIN.Length], corner2 ?? Parameters.DOMAIN, parent) {
			//this.Barycenter = new WeightedIncrementalVectorAverage(this.LeftCorner.Zip(this.RightCorner, (a, b) => (a + b) / 2d).ToArray());
			if (!(particles is null))
				this.AddRange(particles);
		}
		protected override ParticleTree<P> NewNode(double[] cornerA, double[] cornerB, ParticleTree<P> parent) {
			return new(null, cornerA, cornerB, parent);
		}

		protected override ParticleTree<P>[] OrganizeNodes(ParticleTree<P>[] nodes) {
			Program.Random.ShuffleInPlace(nodes);
			return nodes;
		}

		//protected override void Incorporate(P element) {
		//	this.TotalMass += element.Mass;
		//	this.Barycenter.Update(element.Coordinates, VectorFunctions.Distance(element.Coordinates, this.Barycenter.Current));
		//}
	}
}
