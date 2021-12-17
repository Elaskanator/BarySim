using System;
using System.Collections.Generic;
using System.Numerics;
using Generic.Vectors;

namespace ParticleSimulator.Simulation {
	public interface ISimulationParticle : IParticle {
		public int GroupID { get; }
		public float Radius { get; }
		public float Density { get; }
		public float Luminosity { get; }
	}

	public abstract class ASimulationParticle<TSelf> : AParticle<TSelf>, ISimulationParticle, IEquatable<TSelf>, IEqualityComparer<TSelf>
	where TSelf : ASimulationParticle<TSelf>, IEquatable<TSelf>, IEqualityComparer<TSelf> {
		public ASimulationParticle(int groupID, Vector<float> position, Vector<float> velocity)
		: base(position) {
			this.GroupID = groupID;
			this.Velocity = velocity;
			this.CollisionAcceleration = Vector<float>.Zero;
			this.IsEnabled = true;
		}
		public override string ToString() { return string.Format("{0}[ID {0}]<{1}>", this.ID, string.Join(",", this.Position)); }
		
		public int GroupID { get; private set; }
		public bool IsEnabled { get; set; }

		public virtual float Radius => 0f;
		public virtual float Density => 1f;
		public virtual float Luminosity => 1f;
		
		public virtual Vector<float> Velocity { get; set; }
		public virtual Vector<float> CollisionAcceleration { get; set; }

		public readonly Queue<TSelf> Collisions = new();
		public readonly HashSet<TSelf> MergedParticles = new();

		public virtual int? InteractionLimit => null;

		public IEnumerable<TSelf> ApplyTimeStep(Vector<float> acceleration, float timeStep) {
			this.Velocity += 
				(acceleration + this.CollisionAcceleration)
				.Clamp(timeStep * Parameters.TIME_SCALE * Parameters.PARTICLE_MAX_ACCELERATION, Parameters.DIM)
				* (timeStep * Parameters.TIME_SCALE);
			this.Position += this.Velocity * timeStep;
			return this.AfterUpdate();
		}
		
		protected virtual IEnumerable<TSelf> Filter(IEnumerable<TSelf> others) { return others; }
		protected virtual IEnumerable<TSelf> AfterUpdate() { return new TSelf[] { (TSelf)this }; }
		public virtual bool CollideCombine(float distance, Vector<float> toOther, TSelf other, ref float strength) { return false; }
	}
}