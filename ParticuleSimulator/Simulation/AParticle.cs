using System;
using System.Collections.Generic;
using System.Linq;
using Generic.Extensions;
using Generic.Vectors;

namespace ParticleSimulator.Simulation {
	public abstract class AParticle : VectorDouble, IEquatable<AParticle>, IEqualityComparer<AParticle> {
		private static int _globalID = 0;
		public AParticle(int groupID, double[] position, double[] velocity, double mass = 1d)
		: base(position) {
			this.GroupID = groupID;
			this.Mass = mass;
			this.Velocity = velocity;
			this.NetForce = new double[this.DIM];
		}

		private int _id = ++_globalID;
		public int ID => this._id;
		public bool IsActive = true;
		public int GroupID { get; private set; }
		public virtual double Mass { get; set; }
		public virtual double Radius { get; protected set; }

		internal double[] _coordinates;
		public override double[] Coordinates {
			get => this._coordinates;
			set {
				this._coordinates = value;
				this.LiveCoordinates = (double[])value.Clone();
		}}
		public double[] LiveCoordinates { get; set; }
		public double[] Velocity { get; set; }
		public double[] Acceleration => this.NetForce.Divide(this.Mass);
		public double[] NetForce { get; internal set; }

		public bool IsVisible => this.LiveCoordinates.All((x, d) => x + this.Radius >= 0 && x - this.Radius < Parameters.DOMAIN_SIZE[d]);
		public virtual int? InteractionLimit => null;

		protected virtual IEnumerable<AParticle> Filter(IEnumerable<AParticle> others) { return others; }
		protected virtual void AfterUpdate() { }

		public void ApplyTimeStep() {
			this.Velocity = this.Velocity.Add(this.Acceleration.Multiply(Parameters.TIME_SCALE));
			
			this.AfterUpdate();
			
			this.LiveCoordinates = this.LiveCoordinates.Add(this.Velocity);
		}
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
		public void BounceVelocity() {
			double dist;
			for (int d = 0; d < Parameters.DIM; d++) {
				dist = this.LiveCoordinates[d] - Parameters.DOMAIN_CENTER[d];
				if (dist < -Parameters.DOMAIN_MAX_RADIUS)
					this.Velocity[d] += Parameters.WORLD_BOUNCE_WEIGHT * Math.Pow(Parameters.DOMAIN_MAX_RADIUS - dist, 0.5d);
				else if (dist > Parameters.DOMAIN_MAX_RADIUS)
					this.Velocity[d] -= Parameters.WORLD_BOUNCE_WEIGHT * Math.Pow(dist - Parameters.DOMAIN_MAX_RADIUS, 0.5d);
			}
		}

		public bool Equals(AParticle other) { return !(other is null) && this.ID == other.ID; }
		public override bool Equals(object other) { return !(other is null) && (other is AParticle) && this.ID == (other as AParticle).ID; }
		public bool Equals(AParticle x, AParticle y) { return x.ID == y.ID; }
		public int GetHashCode(AParticle obj) { return obj.ID.GetHashCode(); }
		public override int GetHashCode() { return this.ID.GetHashCode(); }
		public override string ToString() {
			return string.Format("{0}[ID {1}]<{2}>", nameof(AParticle), this.ID,
				string.Join(",", this.LiveCoordinates.Select(i => i.ToString("G5"))));
		}
	}
}