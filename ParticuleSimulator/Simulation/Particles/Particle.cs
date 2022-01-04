using System;
using System.Numerics;
using Generic.Vectors;

namespace ParticleSimulator {
	public interface IParticle : IPosition<Vector<float>>, IEquatable<IParticle> {
		int ID { get; }
		int GroupID { get; }
		float Radius { get; }
		float Luminosity { get; }
		
		bool Equals(object other) => (other is IParticle data) && this.ID == data.ID;
		bool IEquatable<IParticle>.Equals(IParticle other) => this.ID == other.ID;
		int GetHashCode() => this.ID;
	}

	public class Particle : IParticle {
		private static int _globalID = 0;
		private readonly int _id = ++_globalID;

		public Particle() {
			this.Enabled = true;
		}

		public override string ToString() => string.Format("Particle[<{0}> ID {1}]", this.ID, string.Join("", this.Position));

		public int ID => this._id;
		public int GroupID { get; internal set; }
		public bool Enabled { get; set; }

		public float Charge { get; set; }
		public float Radius { get; protected set; }
		public float Luminosity { get; protected set; }
		public virtual float Density => Parameters.GRAVITY_RADIAL_DENSITY;

		private float _mass = 0f;
		public virtual float Mass {
			get => this._mass;
			set {
				this._mass = value;
				this.Radius = (float)VectorFunctions.HypersphereRadius(value / this.Density, Parameters.DIM);
				this.Luminosity = MathF.Pow(value * Parameters.MASS_LUMINOSITY_SCALAR, 3.5f); }}
		
		public Vector<float> Position { get; set; }
		public Vector<float> Velocity { get; set; }
		public Vector<float> Acceleration { get; set; }

		internal bool Test1;
		internal bool Test2;

		public virtual Vector<float> Force {
			get => this.Acceleration * this.Mass;
			set { this.Acceleration = value * (1f / this.Mass); } }
		public virtual Vector<float> Momentum {
			get => this.Velocity * this.Mass;
			set { this.Velocity = value * (1f / this.Mass); } }

		public void ApplyTimeStep(float timeStep) {
			Vector<float> velocity = this.Velocity + timeStep*this.Acceleration,
				displacement = timeStep*velocity,
				newP = this.Position + displacement;

			if (Parameters.WORLD_BOUNCING || Parameters.WORLD_WRAPPING) {
				Vector<int>
					lesses = Vector.LessThan(newP, Parameters.WORLD_LEFT_INF),
					greaters = Vector.GreaterThanOrEqual(newP, Parameters.WORLD_RIGHT_INF),
					union = lesses | greaters;
				if (Vector.LessThanAny(union, Vector<int>.Zero)) {
					velocity = Vector.ConditionalSelect(union, -velocity, velocity);
					if (Parameters.WORLD_BOUNCING)
						newP = -newP + 2f
							* Vector.ConditionalSelect(lesses,
								Parameters.WORLD_LEFT,
								Vector.ConditionalSelect(greaters,
									Parameters.WORLD_RIGHT,
									newP));
					else newP = WrapPosition(newP);
				}
			}
			this.Position = newP;
			this.Velocity = velocity;
		}

		public virtual bool TryMerge(Particle other) {
			float largerRadius = this.Radius > other.Radius ? this.Radius : other.Radius,
				dist = this.Position.Distance(other.Position);
			
			if (dist <= Parameters.WORLD_EPSILON || dist <= largerRadius) {
				this.Incorporate(other);
				return true;
			} else return false;
		}

		public void Incorporate(Particle other) {
			float mass = this.Mass + other.Mass;
			Vector<float> position = ((this.Position*this.Mass) + (other.Position*other.Mass)) * (1f / mass);
			this.Mass = mass;
			this.Position = position;
			this.Momentum = this.Momentum + other.Momentum;
			this.Force = this.Force + other.Force;
			other.Enabled = false;
		}

		public void WrapPosition() {
			this.Position = WrapPosition(this.Position);
		}

		public void BoundPosition() {
			this.Position = BoundPosition(this.Position);
		}

		public static Vector<float> WrapPosition(Vector<float> p) {
			Span<float> values = stackalloc float[Vector<float>.Count];
			values[0] = Parameters.DIM < 1 ? 0f :
				p[0] < Parameters.WORLD_LEFT[0]
				? Parameters.WORLD_LEFT[0] + Parameters.WORLD_SIZE[0] + ((p[0] - Parameters.WORLD_LEFT[0]) % Parameters.WORLD_SIZE[0])
				: p[0] >= Parameters.WORLD_RIGHT[0]
					? Parameters.WORLD_LEFT[0] + ((p[0] - Parameters.WORLD_LEFT[0]) % Parameters.WORLD_SIZE[0])
					: p[0];
			values[1] = Parameters.DIM < 2 ? 0f :
				p[1] < Parameters.WORLD_LEFT[1]
				? Parameters.WORLD_LEFT[1] + Parameters.WORLD_SIZE[1] + ((p[1] - Parameters.WORLD_LEFT[1]) % Parameters.WORLD_SIZE[1])
				: p[1] >= Parameters.WORLD_RIGHT[1]
					? Parameters.WORLD_LEFT[1] + ((p[1] - Parameters.WORLD_LEFT[1]) % Parameters.WORLD_SIZE[1])
					: p[1];
			values[2] = Parameters.DIM < 3 ? 0f :
				p[2] < Parameters.WORLD_LEFT[2]
				? Parameters.WORLD_LEFT[2] + Parameters.WORLD_SIZE[2] + ((p[2] - Parameters.WORLD_LEFT[2]) % Parameters.WORLD_SIZE[2])
				: p[2] >= Parameters.WORLD_RIGHT[2]
					? Parameters.WORLD_LEFT[2] + ((p[2] - Parameters.WORLD_LEFT[2]) % Parameters.WORLD_SIZE[2])
					: p[2];
			values[3] = Parameters.DIM < 4 ? 0f :
				p[3] < Parameters.WORLD_LEFT[3]
				? Parameters.WORLD_LEFT[3] + Parameters.WORLD_SIZE[3] + ((p[3] - Parameters.WORLD_LEFT[3]) % Parameters.WORLD_SIZE[3])
				: p[3] >= Parameters.WORLD_RIGHT[3]
					? Parameters.WORLD_LEFT[3] + ((p[3] - Parameters.WORLD_LEFT[3]) % Parameters.WORLD_SIZE[3])
					: p[3];
			values[4] = Parameters.DIM < 5 ? 0f :
				p[4] < Parameters.WORLD_LEFT[4]
				? Parameters.WORLD_LEFT[4] + Parameters.WORLD_SIZE[4] + ((p[4] - Parameters.WORLD_LEFT[4]) % Parameters.WORLD_SIZE[4])
				: p[4] >= Parameters.WORLD_RIGHT[4]
					? Parameters.WORLD_LEFT[4] + ((p[4] - Parameters.WORLD_LEFT[4]) % Parameters.WORLD_SIZE[4])
					: p[4];
			values[5] = Parameters.DIM < 6 ? 0f :
				p[5] < Parameters.WORLD_LEFT[5]
				? Parameters.WORLD_LEFT[5] + Parameters.WORLD_SIZE[5] + ((p[5] - Parameters.WORLD_LEFT[5]) % Parameters.WORLD_SIZE[5])
				: p[5] >= Parameters.WORLD_RIGHT[5]
					? Parameters.WORLD_LEFT[5] + ((p[5] - Parameters.WORLD_LEFT[5]) % Parameters.WORLD_SIZE[5])
					: p[5];
			values[6] = Parameters.DIM < 7 ? 0f :
				p[6] < Parameters.WORLD_LEFT[6]
				? Parameters.WORLD_LEFT[6] + Parameters.WORLD_SIZE[6] + ((p[6] - Parameters.WORLD_LEFT[6]) % Parameters.WORLD_SIZE[6])
				: p[6] >= Parameters.WORLD_RIGHT[6]
					? Parameters.WORLD_LEFT[6] + ((p[6] - Parameters.WORLD_LEFT[6]) % Parameters.WORLD_SIZE[6])
					: p[6];
			values[7] = Parameters.DIM < 8 ? 0f :
				p[7] < Parameters.WORLD_LEFT[7]
				? Parameters.WORLD_LEFT[7] + Parameters.WORLD_SIZE[7] + ((p[7] - Parameters.WORLD_LEFT[7]) % Parameters.WORLD_SIZE[7])
				: p[7] >= Parameters.WORLD_RIGHT[7]
					? Parameters.WORLD_LEFT[7] + ((p[7] - Parameters.WORLD_LEFT[7]) % Parameters.WORLD_SIZE[7])
					: p[7];
			return new Vector<float>(values);
		}

		public static Vector<float> BoundPosition(Vector<float> p) {
			Span<float> values = stackalloc float[Vector<float>.Count];
			values[0] = Parameters.DIM < 1 ? 0f :
				p[0] < Parameters.WORLD_LEFT[0]
				? Parameters.WORLD_LEFT[0]
				: p[0] >= Parameters.WORLD_RIGHT[0]
					? Parameters.WORLD_RIGHT[0] - Parameters.WORLD_EPSILON
					: p[0];
			values[1] = Parameters.DIM < 1 ? 0f :
				p[1] < Parameters.WORLD_LEFT[1]
				? Parameters.WORLD_LEFT[1]
				: p[1] >= Parameters.WORLD_RIGHT[1]
					? Parameters.WORLD_RIGHT[1] - Parameters.WORLD_EPSILON
					: p[1];
			values[2] = Parameters.DIM < 1 ? 0f :
				p[2] < Parameters.WORLD_LEFT[2]
				? Parameters.WORLD_LEFT[2]
				: p[2] >= Parameters.WORLD_RIGHT[2]
					? Parameters.WORLD_RIGHT[2] - Parameters.WORLD_EPSILON
					: p[2];
			values[3] = Parameters.DIM < 1 ? 0f :
				p[3] < Parameters.WORLD_LEFT[3]
				? Parameters.WORLD_LEFT[3]
				: p[3] >= Parameters.WORLD_RIGHT[3]
					? Parameters.WORLD_RIGHT[3] - Parameters.WORLD_EPSILON
					: p[3];
			values[4] = Parameters.DIM < 1 ? 0f :
				p[4] < Parameters.WORLD_LEFT[4]
				? Parameters.WORLD_LEFT[4]
				: p[4] >= Parameters.WORLD_RIGHT[4]
					? Parameters.WORLD_RIGHT[4] - Parameters.WORLD_EPSILON
					: p[4];
			values[5] = Parameters.DIM < 1 ? 0f :
				p[5] < Parameters.WORLD_LEFT[5]
				? Parameters.WORLD_LEFT[5]
				: p[5] >= Parameters.WORLD_RIGHT[5]
					? Parameters.WORLD_RIGHT[5] - Parameters.WORLD_EPSILON
					: p[5];
			values[6] = Parameters.DIM < 1 ? 0f :
				p[6] < Parameters.WORLD_LEFT[6]
				? Parameters.WORLD_LEFT[6]
				: p[6] >= Parameters.WORLD_RIGHT[6]
					? Parameters.WORLD_RIGHT[6] - Parameters.WORLD_EPSILON
					: p[6];
			values[7] = Parameters.DIM < 1 ? 0f :
				p[7] < Parameters.WORLD_LEFT[7]
				? Parameters.WORLD_LEFT[7]
				: p[7] >= Parameters.WORLD_RIGHT[7]
					? Parameters.WORLD_RIGHT[7] - Parameters.WORLD_EPSILON
					: p[7];
			return new Vector<float>(values);
		}
	}
}