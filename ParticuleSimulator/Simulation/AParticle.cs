using System;
using System.Collections.Generic;
using System.Linq;
using Generic.Models;

namespace ParticleSimulator {
	public abstract class AParticle : VectorDouble {
		private static int _globalID = 0;

		private readonly int _myId = ++_globalID;
		public int ID => this._myId;
		public virtual int? InteractionLimit => null;
		public virtual double SpeedDecay => 0d;
		public int GroupID { get; private set; }
		public double Mass { get; private set; }

		public AParticle(int groupID, double[] position, double[] velocity, double mass = 1d)
		: base(position) {
			this.GroupID = groupID;
			this.Mass = mass;
			this.Velocity = velocity;
			this.AccumulatedImpulse = new double[this.DIMENSIONALITY];

			this._actuallyTrueCoordinates = this.WrapPosition(position);
			this.Coordinates = (double[])this.TrueCoordinates.Clone();//now set values used for quadtree build (after clamping)
		}

		private static double[] _bounceMax;
		static AParticle() {
			if (!Parameters.WORLD_WRAPPING) {
				if (Parameters.WORLD_BOUNCE_SIZE < Parameters.WORLD_EPSILON)
					_bounceMax = Parameters.DOMAIN.Select(x => x - Parameters.WORLD_EPSILON).ToArray();//need min <= x < max (exclude domain max)
				else _bounceMax = Parameters.DOMAIN.Select(x => x - Parameters.WORLD_BOUNCE_SIZE).ToArray();
			}
		}

		private double[] _actuallyTrueCoordinates;//base Vector.Coordinates value is a (stale) snapshot for tree/spatial mapping structures
		public double[] TrueCoordinates {
			get { return this._actuallyTrueCoordinates; }
			private set {
				this._actuallyTrueCoordinates = Parameters.WORLD_WRAPPING
					? this.WrapPosition(value)
					: this.BounceEdge(value);//and clamp position
		} }
		public double[] Velocity { get; set; }
		public virtual double[] AccumulatedImpulse { get; set; }
		
		public abstract void Interact(IEnumerable<AParticle> others);//returns whether to stop evaluating more
		public virtual void AfterInteract() { }
		public virtual void InteractSubtree(ITree node) { }

		public void ApplyTimeStep() {
			this.Velocity = this.Velocity.Multiply(this.SpeedDecay).Add(this.AccumulatedImpulse.Divide(this.Mass));
			
			this.AfterInteract();
			
			this.TrueCoordinates = this.TrueCoordinates.Add(this.Velocity);
		}
		private double[] WrapPosition(double[] p) {
			for (int i = 0; i < this.DIMENSIONALITY; i++)
				if (p[i] < 0d)
					p[i] = (p[i] % Parameters.DOMAIN[i]) + Parameters.DOMAIN[i];//don't want symmetric modulus
				else if (p[i] >= Parameters.DOMAIN[i])
					p[i] %= Parameters.DOMAIN[i];
			return p;
		}
		private double[] BounceEdge(double[] p) {
			double diffFraction;
			for (int i = 0; i < this.DIMENSIONALITY; i++) {
				diffFraction = 0;
				if (p[i] < 0d) {
					p[i] = 0d;
					this.Velocity[i] = this.Velocity[i] < 0d ? 0d : this.Velocity[i];
					diffFraction = 1d;
				} else if (p[i] < Parameters.WORLD_BOUNCE_SIZE){
					diffFraction = (Parameters.WORLD_BOUNCE_SIZE - p[i]) / Parameters.WORLD_BOUNCE_SIZE;
				} else if (p[i] >= Parameters.DOMAIN[i]) {//position MUST be strictly less than domain max
					p[i] = Parameters.DOMAIN[i] - Parameters.WORLD_EPSILON;
					this.Velocity[i] = this.Velocity[i] > 0d ? 0d : this.Velocity[i];
					diffFraction = -1d;
				} else if (p[i] > _bounceMax[i]) {
					diffFraction = (_bounceMax[i] - p[i]) / Parameters.WORLD_BOUNCE_SIZE;
				}
				if (diffFraction != 0)
					this.Velocity[i] += Parameters.WORLD_BOUNCE_WEIGHT * Math.Sign(diffFraction) * Math.Pow(diffFraction, 2);
			}
			return p;
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
}