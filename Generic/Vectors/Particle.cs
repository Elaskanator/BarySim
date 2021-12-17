using System;
using System.Collections.Generic;
using System.Numerics;

namespace Generic.Vectors {
	public interface IParticle {
		public int ID { get; }

		public Vector<float> Position { get; }
	}
	public abstract class AParticle<TSelf> : IParticle, IEquatable<TSelf>, IEqualityComparer<TSelf>
	where TSelf : AParticle<TSelf> {
		public AParticle(Vector<float> position) {
			this.Position = position;
		}

		private static int _globalID = 0;
		private readonly int _id = ++_globalID;
		public int ID => this._id;

		public Vector<float> Position { get; set; }

		public bool Equals(IParticle other) { return !(other is null) && this.ID == other.ID; }
		public bool Equals(TSelf other) { return !(other is null) && this.ID == other.ID; }
		public override bool Equals(object other) { return !(other is null) && (other is IParticle) && this.ID == (other as IParticle).ID; }
		public bool Equals(IParticle x, IParticle y) { return x.ID == y.ID; }
		public bool Equals(TSelf x, TSelf y) { return x.ID == y.ID; }
		public int GetHashCode(IParticle obj) { return obj.ID.GetHashCode(); }
		public int GetHashCode(TSelf obj) { return obj.ID.GetHashCode(); }
		public override int GetHashCode() { return this.ID.GetHashCode(); }
	}
}