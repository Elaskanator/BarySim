using System.Linq;
using Generic.Models;

namespace ParticleSimulator.Simulation.Gravity {
	public class BaryonQuadTree : AVectorQuadTree<CelestialBody, BaryonQuadTree> {
		public BaryonQuadTree(double[] corner1 = null, double[] corner2 = null, BaryonQuadTree parent = null)
		: base(corner1 ?? new double[Parameters.DIM], corner2 ?? Parameters.DOMAIN, parent) {
			this.Barycenter = new VectorIncrementalWeightedAverage();
		}

		public override int Capacity => Parameters.GRAVITY_QUADTREE_NODE_CAPACITY;
		public VectorIncrementalWeightedAverage Barycenter { get; private set; }

		protected override BaryonQuadTree NewNode(double[] cornerA, double[] cornerB, BaryonQuadTree parent) {
			return new(cornerA, cornerB, parent);
		}
		
		protected override double[] MakeCenter() {
			double minDivision = 1d / (1 << (this.Capacity / 2));
			return Enumerable
				.Range(0, Parameters.DIM)
				.Select(d => this._members.Average(m => m.Coordinates[d]))
				.Select((avg, d) =>
					avg < this.LeftCorner[d] + minDivision*(this.RightCorner[d] - this.LeftCorner[d])
						? this.LeftCorner[d] + minDivision*(this.RightCorner[d] - this.LeftCorner[d])
						: avg > this.RightCorner[d] - minDivision*(this.RightCorner[d] - this.LeftCorner[d])
							? this.RightCorner[d] - minDivision*(this.RightCorner[d] - this.LeftCorner[d])
							: avg)
				.ToArray();
		}
		
		protected override void Incorporate(CelestialBody element) {
			this.Barycenter.Update(element.LiveCoordinates, element.Mass);
		}
	}
}
