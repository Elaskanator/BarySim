using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Generic.Extensions;
using Generic.Vectors;

namespace ParticleSimulator.Simulation {
	public interface IParticle : IVectorDouble, IEquatable<IParticle>, IEqualityComparer<IParticle> {
		public int ID { get; }
		public int GroupID { get; }
		public bool Enabled { get; set; }

		public double[] LiveCoordinates { get; set; }
		public double[] Velocity { get; set; }

		public double Radius { get; }
		public double Density { get; }
		public double Luminosity { get; }
	}

	public abstract class AParticle<TSelf> : VectorDouble, IParticle
	where TSelf : AParticle<TSelf> {
		private static int _globalID = 0;
		public AParticle(int groupID, double[] position, double[] velocity)
		: base(position) {
			this.GroupID = groupID;
			this.Velocity = velocity;
			this.CollisionAcceleration = new double[Parameters.DIM];
			this.Enabled = true;
		}

		private readonly int _id = ++_globalID;
		public int ID => this._id;
		public bool Enabled { get; set; }
		public int GroupID { get; private set; }
		public virtual double Radius => 0d;
		public virtual double Density => 1d;
		public virtual double Luminosity => 1d;

		internal double[] _coordinates;
		public override double[] Coordinates {
			get => this._coordinates;
			set {
				this._coordinates = value;
				this.LiveCoordinates = (double[])value.Clone(); }}
		public double[] LiveCoordinates { get; set; }
		public virtual double[] Velocity { get; set; }
		public virtual double[] CollisionAcceleration { get; set; }

		public readonly ConcurrentQueue<TSelf> NeighborNodeCollisions = new();
		public readonly Queue<TSelf> NodeCollisions = new();
		public readonly HashSet<TSelf> MergedParticles = new();

		public bool IsVisible => this.LiveCoordinates.All((x, d) => x + this.Radius >= 0d && x - this.Radius < Parameters.DOMAIN_SIZE[d]);
		public virtual int? InteractionLimit => null;

		protected virtual IEnumerable<TSelf> Filter(IEnumerable<TSelf> others) { return others; }

		public void ApplyTimeStep(double[] acceleration, double timeStep) {
			this.Velocity = this.Velocity.Add(acceleration.Add(this.CollisionAcceleration).Multiply(timeStep));
			this.LiveCoordinates = this.LiveCoordinates.Add(this.Velocity.Multiply(timeStep));
			this.AfterUpdate();
		}
		protected virtual void AfterUpdate() { }

		public void CombineWith(TSelf other) {
			other.Enabled = false;
			this.Incorporate(other);
			this.MergedParticles.Add(other);
		}
		protected virtual void Incorporate(TSelf other) { throw new NotSupportedException(); }

		public void WrapPosition() {
			for (int i = 0; i < this.DIM; i++)
				if (this.LiveCoordinates[i] < 0d)
					this.LiveCoordinates[i] = (this.LiveCoordinates[i] % Parameters.DOMAIN_SIZE[i]) + Parameters.DOMAIN_SIZE[i];//don't want symmetric modulus
				else if (this.LiveCoordinates[i] >= Parameters.DOMAIN_SIZE[i])
					this.LiveCoordinates[i] %= Parameters.DOMAIN_SIZE[i];
		}
		public void BoundPosition() {
			for (int i = 0; i < this.DIM; i++)
				if (this.LiveCoordinates[i] < 0d)
					this.LiveCoordinates[i] = 0d;
				else if (this.LiveCoordinates[i] >= Parameters.DOMAIN_SIZE[i])
					this.LiveCoordinates[i] = Parameters.DOMAIN_SIZE[i] - Parameters.WORLD_EPSILON;
		}
		public void BounceVelocity(double weight) {
			double dist;
			for (int d = 0; d < Parameters.DIM; d++) {
				dist = this.LiveCoordinates[d] - Parameters.DOMAIN_CENTER[d];
				if (dist < -Parameters.DOMAIN_MAX_RADIUS)
					this.Velocity[d] += weight * Math.Pow(Parameters.DOMAIN_MAX_RADIUS - dist, 0.5d);
				else if (dist > Parameters.DOMAIN_MAX_RADIUS)
					this.Velocity[d] -= weight * Math.Pow(dist - Parameters.DOMAIN_MAX_RADIUS, 0.5d);
			}
		}

		public bool Equals(IParticle other) { return !(other is null) && this.ID == other.ID; }
		public override bool Equals(object other) { return !(other is null) && (other is IParticle) && this.ID == (other as IParticle).ID; }
		public bool Equals(IParticle x, IParticle y) { return x.ID == y.ID; }
		public int GetHashCode(IParticle obj) { return obj.ID.GetHashCode(); }
		public override int GetHashCode() { return this.ID.GetHashCode(); }
		public override string ToString() {
			return string.Format("{0}[ID {1}]<{2}>", nameof(IParticle), this.ID,
				string.Join(",", this.LiveCoordinates.Select(i => i.ToString("G5"))));
		}
	}
}