using System;
using System.Linq;
using System.Threading.Tasks;
using Generic.Models;

namespace ParticleSimulator {
	public abstract class AParticle : SimpleVector {
		private static int _globalID = 0;

		private readonly int _myId = ++_globalID;
		public int ID { get { return this._myId; } }
		public int GroupID { get; private set; }
		public abstract double Radius { get; }
		public virtual int? InteractionLimit => null;

		private double[] _trueCoordinates;
		public double[] TrueCoordinates {
			get { return this._trueCoordinates; }
			set { this._trueCoordinates = this.BoundPosition(value); } }
		private double[] _velocity;
		public double[] Velocity {
			get { return this._velocity; }
			set { this._velocity = Parameters.MAX_SPEED > 0 ? VectorFunctions.Clamp(value, Parameters.MAX_SPEED) : value; } }
		private double[] _acceleration;
		public double[] Acceleration {
			get { return this._acceleration; }
			set { this._acceleration = Parameters.MAX_ACCELERATION > 0 ? VectorFunctions.Clamp(value, Parameters.MAX_ACCELERATION) : value; } }


		public AParticle(int groupID, double[] position, double[] velocity)
		: base(position) {
			this.GroupID = groupID;

			this.TrueCoordinates = position;//causes coordinates to be bounded
			this.Coordinates = this.TrueCoordinates;//now set values used for quadtree build (after clamping)

			this.Velocity = velocity;
			this._acceleration = new double[this.Dimensionality];
		}
		
		public abstract void Interact(AParticle other);
		public virtual void InteractSubtree(ITree node) { return; }

		public virtual void InteractMany(AParticle[] particles) {
			this._acceleration = new double[this.Dimensionality];

			for (int i = 0; i < particles.Length; i++)
				if (this.GroupID != particles[i].GroupID)
					this.Interact(particles[i]);
		}
		public virtual void InteractMany(ATree<AParticle> tree) {
			this._acceleration = new double[this.Dimensionality];

			Parallel.ForEach(tree.Leaves, leaf => {
				ATree<AParticle>[] nodes = leaf.GetRefinedNeighborNodes(Parameters.QUADTREE_NEIGHBORHOOD_FILTER).ToArray();
				foreach (AParticle b in leaf.NodeElements)
					foreach (ATree<AParticle> otherNode in nodes) {
						if (otherNode.IsLeaf)
							foreach (AParticle b2 in otherNode.NodeElements)
								if (b.ID != b2.ID)
									b.Interact(b2);
						else
							b.InteractSubtree(otherNode);
					}});
		}

		internal virtual void ApplyUpdate() {
			if (Parameters.SPEED_DECAY > 0)
				this.Velocity = this.Velocity.Multiply(Math.Exp(-Parameters.SPEED_DECAY)).Add(this.Acceleration);
			else this.Velocity = this.Velocity.Add(this.Acceleration);

			this.TrueCoordinates = this.Velocity.Add(this.TrueCoordinates);
		}
		private double[] BoundPosition(double[] position) {
			return position
				.Select((x, i) =>
					x < 0d
						? x % Parameters.DOMAIN[i] + Parameters.DOMAIN[i]//don't want symmetric modulus
						: x >= Parameters.DOMAIN[i]
							? x % Parameters.DOMAIN[i]
							: x)
				.ToArray();
		}

		public bool Equals(AParticle other) { return !(other is null) && this.ID == other.ID; }
		public override bool Equals(object other) { return !(other is null) && (other is AParticle) && this.ID == (other as AParticle).ID; }
		public bool Equals(AParticle x, AParticle y) { return x.ID == y.ID; }
		public int GetHashCode(AParticle obj) { return obj.ID.GetHashCode(); }
		public override int GetHashCode() { return this.ID.GetHashCode(); }
		public override string ToString() {
			return string.Format("{0}[ID {1}]<{2}><{3}>", nameof(AParticle), this.ID,
				string.Join(",", this.TrueCoordinates.Select(i => i.ToString("G5"))),
				string.Join(",", this.Coordinates.Select(i => i.ToString("G5"))));
		}
	}

	public abstract class ASymmetricParticle : AParticle {
		public ASymmetricParticle(int groupID, double[] position, double[] velocity)
		: base(groupID, position, velocity) { }
	}
}