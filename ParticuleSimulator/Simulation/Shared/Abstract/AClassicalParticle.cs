using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Generic.Extensions;
using Generic.Vectors;

namespace ParticleSimulator.Simulation {
	public abstract class AClassicalParticle : VectorDouble, IEquatable<AClassicalParticle>, IEqualityComparer<AClassicalParticle> {
		private static int _globalID = 0;
		public AClassicalParticle(int groupID, double[] position, double[] velocity, double mass = 1d, double charge = 0d)
		: base(position) {
			this.Momentum = new double[Parameters.DIM];
			this.Impulse = new double[Parameters.DIM];
			this.GroupID = groupID;
			this.Velocity = velocity;
			this.Mass = mass;
			this.Charge = charge;
		}

		private int _id = ++_globalID;
		public int ID => this._id;
		public bool IsAlive = true;
		public readonly int GroupID;
		public virtual double Radius => 0d;
		public double Mass { get; set; }
		public double Charge { get; set; }

		internal double[] _coordinates;
		public override double[] Coordinates {
			get => this._coordinates;
			set {
				this._coordinates = value;
				this.LiveCoordinates = (double[])value.Clone(); }}
		public double[] LiveCoordinates { get; set; }
		public double[] Velocity {
			get => this.Momentum.Divide(this.Mass);
			set { this.Momentum = value.Multiply(this.Mass); }}
		public double[] Momentum { get; set; }
		public double[] Impulse { get; set; }

		public bool IsVisible => this.LiveCoordinates.All((x, d) => x + this.Radius >= 0 && x - this.Radius < Parameters.DOMAIN_SIZE[d]);
		public virtual int? InteractionLimit => null;

		protected virtual IEnumerable<AClassicalParticle> Filter(IEnumerable<AClassicalParticle> others) { return others; }

		public void ApplyTimeStep() {
			this.Momentum = this.Momentum.Add(this.Impulse.Multiply(Parameters.TIME_SCALE));
			this.AfterUpdate();
			this.LiveCoordinates = this.LiveCoordinates.Add(this.Velocity.Multiply(Parameters.TIME_SCALE));
		}

		public double GetPhysicalAttribute(PhysicalAttribute attr) {
			switch (attr) {
				case PhysicalAttribute.Mass:
					return this.Mass;
				case PhysicalAttribute.Charge:
					return this.Charge;
				default:
					throw new InvalidEnumArgumentException(nameof(attr), (int)attr, typeof(PhysicalAttribute));
			}
		}

		protected virtual void AfterUpdate() { }

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

		public bool Equals(AClassicalParticle other) { return !(other is null) && this.ID == other.ID; }
		public override bool Equals(object other) { return !(other is null) && (other is AClassicalParticle) && this.ID == (other as AClassicalParticle).ID; }
		public bool Equals(AClassicalParticle x, AClassicalParticle y) { return x.ID == y.ID; }
		public int GetHashCode(AClassicalParticle obj) { return obj.ID.GetHashCode(); }
		public override int GetHashCode() { return this.ID.GetHashCode(); }
		public override string ToString() {
			return string.Format("{0}[ID {1}]<{2}>", nameof(AClassicalParticle), this.ID,
				string.Join(",", this.LiveCoordinates.Select(i => i.ToString("G5"))));
		}
	}
}