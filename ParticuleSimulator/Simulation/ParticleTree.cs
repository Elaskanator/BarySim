using System.Linq;
using Generic.Models;
using Generic.Extensions;

namespace ParticleSimulator.Simulation {
	public sealed class ParticleTree<P> : AQuadTree<P, ParticleTree<P>>
	where P : AParticle {
		//public WeightedIncrementalVectorAverage Barycenter { get; private set; }
		//public double TotalMass { get; private set; }
		public override int Capacity => Parameters.QUADTREE_NODE_CAPACITY;

		public ParticleTree(double[] corner1 = null, double[] corner2 = null, ParticleTree<P> parent = null)
		: base(corner1 ?? new double[Parameters.DOMAIN.Length], corner2 ?? Parameters.DOMAIN, parent) {
			//this.Barycenter = new WeightedIncrementalVectorAverage(this.LeftCorner.Zip(this.RightCorner, (a, b) => (a + b) / 2d).ToArray());
		}
		protected override ParticleTree<P> NewNode(double[] cornerA, double[] cornerB, ParticleTree<P> parent) {
			return new ParticleTree<P>(cornerA, cornerB, parent);
		}
		
		protected override ParticleTree<P>[] FormNodes() {
			double fraction = Program.Random.NextDouble();
			ParticleTree<P>[] newNodes = Enumerable
				.Range(0, 1 << this.Dimensionality)//the 2^dimension "quadrants" of the Euclidean hyperplane
				.Select(q => {
					bool[] isLeft = Enumerable
						.Range(0, this.Dimensionality)
						.Select(d => (q & (1 << d)) > 0)
						.ToArray();
					return this.NewNode(
						isLeft.Zip(this.LeftCorner, this.Size, (iL, LC, S) => LC + S*(iL ? fraction : 0d)).ToArray(),
						isLeft.Zip(this.RightCorner, this.Size, (iL, RC, S) => RC - S*(iL ? 0d : 1 - fraction)).ToArray(),
						this);
				}).ToArray();
			Program.Random.ShuffleInPlace(newNodes);
			return newNodes;

		}

		//protected override void Incorporate(P element) {
		//	this.TotalMass += element.Mass;
		//	this.Barycenter.Update(element.Coordinates, VectorFunctions.Distance(element.Coordinates, this.Barycenter.Current));
		//}
	}
}
