using System;
using System.Collections.Generic;
using System.Linq;

using Generic.Models;

namespace Simulation {
	public abstract class AParticle : IVector, IEquatable<AParticle>, IEqualityComparer<AParticle> {
		private static int _id = 0;

		public int ID { get; }
		public virtual double[] Coordinates { get; set; }
		public virtual double[] Velocity { get; set; }
		public virtual double[] Acceleration { get; set; }
		public virtual double Mass { get; set; }
		public int CoordinateHash { get { return 0; } }
		
		public AParticle(double[] position, double[] velocity = null, double[] acceleration = null, double mass = 1) {
			this.ID = ++_id;
			this.Coordinates = position;
			this.Velocity = velocity ?? new double[position.Length];
			this.Acceleration = acceleration ?? new double[position.Length];
			this.Mass = mass;
		}
		public AParticle(IVector position) : this(position.Coordinates) { }

		public bool Equals(AParticle other) { return !(other is null) && this.ID == other.ID; }
		public bool Equals(AParticle x, AParticle y) { return x.ID == y.ID; }
		public int GetHashCode(AParticle obj) { return obj.ID.GetHashCode(); }
		public override string ToString() {
			return string.Format("{0}[ID {1}]<{2}>", nameof(AParticle), this.ID,
				string.Join(",", this.Coordinates.Select(i => i.ToString("G5"))));
		}
	}
}