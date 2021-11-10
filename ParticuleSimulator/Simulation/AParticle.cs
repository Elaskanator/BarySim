using System;
using System.Linq;
using Generic.Models;

namespace ParticleSimulator {
	public interface IParticle : IVector {
		public IComparable Mass { get; set; }
		public IVector Velocity { get; set; }
		public IComparable Radius { get; }
	}

	public abstract class AParticle<T> : AVector<T>, IParticle
	where T :IComparable<T> {
		private static int _globalID = 0;

		private readonly int _myId = ++_globalID;
		public int ID { get { return this._myId; } }
		public virtual T Mass { get; set; }
		public virtual IVector<T> Velocity { get; set; }
		private T[] _coordinates;
		public override T[] Coordinates {
			get { return this._coordinates; }
			set { this._coordinates = this.BoundPosition(value); } }
		public abstract T Radius { get; }
		IComparable IParticle.Mass { get => this.Mass as IComparable; set => this.Mass = (T)value; }
		IVector IParticle.Velocity { get => this.Velocity; set => this.Velocity = (IVector<T>)value; }

		IComparable IParticle.Radius => throw new NotImplementedException();

		public AParticle(IVector<T> position, IVector<T> velocity, T mass)
		: base(position) {
			this.Velocity = velocity;
			this.Mass = mass;
		}

		internal abstract void ApplyUpdate();
		protected abstract T[] BoundPosition(T[] value);

		public bool Equals(AParticle<T> other) { return !(other is null) && this.ID == other.ID; }
		public override bool Equals(object other) { return !(other is null) && (other is AParticle<T>) && this.ID == (other as AParticle<T>).ID; }
		public bool Equals(AParticle<T> x, AParticle<T> y) { return x.ID == y.ID; }
		public int GetHashCode(AParticle<T> obj) { return obj.ID.GetHashCode(); }
		public override int GetHashCode() { return this.ID.GetHashCode(); }
		public override string ToString() {
			return string.Format("{0}[ID {1}]<{2}>", nameof(AParticle<T>), this.ID,
				string.Join(",", this.Coordinates.Select(i => i.ToString())));
		}
	}

	public abstract class AParticleDouble : AParticle<double> {
		public AParticleDouble(IVector<double> position, IVector<double> velocity, double mass = 1)
		: base(position, velocity, mass) {
			this.Velocity = velocity;
			this.Mass = mass;
		}
		
		protected override double[] BoundPosition(double[] position) {
			return position
				.Select((x, i) =>
					x < 0d
						? x % Parameters.DOMAIN_DOUBLE[i] + Parameters.DOMAIN_DOUBLE[i]
						: x >= Parameters.DOMAIN_DOUBLE[i]
							? x % Parameters.DOMAIN_DOUBLE[i]
							: x)
				.ToArray();
		}
	}

	public abstract class AParticleFloat : AParticle<float> {
		public AParticleFloat(IVector<float> position, IVector<float> velocity, float mass = 1)
		: base(position, velocity, mass) {
			this.Velocity = velocity;
			this.Mass = mass;
		}
		
		protected override float[] BoundPosition(float[] position) {
			return position
				.Select((x, i) =>
					x < 0f
						? x % Parameters.DOMAIN_FLOAT[i] + Parameters.DOMAIN_FLOAT[i]
						: x >= Parameters.DOMAIN_FLOAT[i]
							? x % Parameters.DOMAIN_FLOAT[i]
							: x)
				.ToArray();
		}
	}
}