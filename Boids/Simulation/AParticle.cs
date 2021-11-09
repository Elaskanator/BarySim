using System;
using System.Collections.Generic;
using System.Linq;
using Generic.Extensions;
using Generic.Models;

namespace ParticleSimulator {
	public abstract class AParticle : SimpleVector, IEquatable<AParticle>, IEqualityComparer<AParticle> {
		private static int _id = 0;

		public readonly int ID = ++_id;
		public virtual double Mass { get; set; }
		public virtual SimpleVector Velocity { get; set; }
		private SimpleVector _coordinates;
		public override double[] Coordinates {
			get { return this._coordinates; }
			set { this._coordinates = this.BoundPosition(value).ToArray(); } }
		public abstract double Radius { get; }
		
		public AParticle(SimpleVector position, SimpleVector velocity, double mass = 1)
		: base(position) {
			this.Velocity = velocity;
			this.Mass = mass;
		}

		internal abstract void ApplyUpdate();
		private IEnumerable<double> BoundPosition(double[] position) {
			for (int i = 0; i < Parameters.DOMAIN.Length; i++)
				if (position[i] < 0 || position[i] >= Parameters.DOMAIN[i])
					yield return position[i].ModuloAbsolute(Parameters.DOMAIN[i]);//wrap around
				else yield return position[i];
		}

		public bool Equals(AParticle other) { return !(other is null) && this.ID == other.ID; }
		public override bool Equals(object other) { return !(other is null) && (other is AParticle) && this.ID == (other as AParticle).ID; }
		public bool Equals(AParticle x, AParticle y) { return x.ID == y.ID; }
		public int GetHashCode(AParticle obj) { return obj.ID.GetHashCode(); }
		public override int GetHashCode() { return this.ID.GetHashCode(); }
		public override string ToString() {
			return string.Format("{0}[ID {1}]<{2}>", nameof(AParticle), this.ID,
				string.Join(",", this.Coordinates.Select(i => i.ToString("G5"))));
		}
	}
}