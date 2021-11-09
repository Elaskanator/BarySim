using System;
using System.Collections.Generic;
using System.Linq;

using Generic.Models;

namespace Simulation {
	public abstract class AParticle : Vector, IEquatable<AParticle>, IEqualityComparer<AParticle> {
		private static int _id = 0;

		public int ID { get; }
		public virtual double Mass { get; set; }
		
		public AParticle(double[] position, double mass = 1) : base(position) {
			this.ID = ++_id;
			this.Mass = mass;
		}
		public AParticle(Vector position) : this(position.Coordinates) { }

		public bool Equals(AParticle other) { return !(other is null) && this.ID == other.ID; }
		public bool Equals(AParticle x, AParticle y) { return x.ID == y.ID; }
		public int GetHashCode(AParticle obj) { return obj.ID.GetHashCode(); }
		public override string ToString() {
			return string.Format("{0}[ID {1}]<{2}>", nameof(AParticle), this.ID,
				string.Join(",", this.Coordinates.Select(i => i.ToString("G5"))));
		}
	}
}