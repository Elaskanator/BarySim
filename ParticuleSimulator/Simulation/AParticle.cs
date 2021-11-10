using System.Linq;
using Generic.Models;

namespace ParticleSimulator {
	public abstract class AParticle : SimpleVector {
		private static int _globalID = 0;

		private readonly int _myId = ++_globalID;
		public int ID { get { return this._myId; } }
		public virtual double Mass { get; set; }
		public virtual double[] Velocity { get; set; }
		private double[] _coordinates;
		public override double[] Coordinates {
			get { return this._coordinates; }
			set { this._coordinates = this.BoundPosition(value); } }
		public abstract double Radius { get; }

		public AParticle(double[] position, double[] velocity, double mass)
		: base(position) {
			this.Velocity = velocity;
			this.Mass = mass;
		}

		internal abstract void ApplyUpdate();
		protected double[] BoundPosition(double[] position) {
			return position
				.Select((x, i) =>
					x < 0d
						? x % Parameters.DOMAIN[i] + Parameters.DOMAIN[i]
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
			return string.Format("{0}[ID {1}]<{2}>", nameof(AParticle), this.ID,
				string.Join(",", this.Coordinates.Select(i => i.ToString())));
		}
	}
}