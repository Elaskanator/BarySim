using System;
using System.Collections.Generic;
using System.Numerics;
using Generic.Trees;
using ParticleSimulator.Simulation.Baryon;

namespace ParticleSimulator.Simulation.Particles {
	public abstract class AParticle<TSelf> : IParticle
	where TSelf : AParticle<TSelf> {//
		private static int _globalID = 0;
		private readonly int _id = ++_globalID;

		protected AParticle(Vector<float> position, Vector<float> velocity) {
			this._position = position;
			this.Velocity = velocity;
			this.Acceleration = this._acceleration1 = this._acceleration2 = Vector<float>.Zero;
			this.DragAcceleration = Vector<float>.Zero;
			this.Enabled = true;
		}

		public override string ToString() => string.Format("Particle[<{0}> ID {1}]", this.Id, string.Join("", this._position));

		public int Id => this._id;
		public int GroupId { get; set; }
		public bool Enabled { get; set; }
		public int Age { get; set; }
		
		protected float _radius;
		public float Radius => this._radius;
		public float Luminosity { get; set; }
		public virtual float Density => 1f;
		
		public Vector<float> _position;//fields are faster than properties with high-volume access
		public Vector<float> Position => this._position;
		public Vector<float> Velocity;
		public Vector<float> DragAcceleration;//exclude from quadrature method on acceleration
		public Vector<float> Acceleration;
		//using derivative of Lagrange interpolating polynomial on acceleration (also counteracts the overstep phenomenon)
		protected Vector<float> _acceleration1;
		protected Vector<float> _acceleration2;
		public Vector<float> Jerk1 => this.Acceleration - this._acceleration1;//first-order
		//see https://www3.nd.edu/~zxu2/acms40390F15/Lec-4.1.pdf (equally-spaced three-point endpoint formula)
		public Vector<float> Jerk2 =>//second-order
			0.5f * (this._acceleration2 - 4f*this._acceleration1 + 3f*this.Acceleration);

		public virtual Vector<float> Momentum {
			get => this.Velocity;
			set { this.Velocity = value; } }
		public virtual Vector<float> Impulse {
			get => this.Acceleration;
			set { this.Acceleration = value; } }
		public virtual Vector<float> DragImpulse {
			get => this.DragAcceleration;
			set { this.DragAcceleration = value; } }

		public Queue<TSelf> Collisions = null;
		public Queue<TSelf> NewParticles = null;
		public ABinaryTree<TSelf> Node = null;

		protected abstract Vector<float> ComputeInfluence(TSelf other, Vector<float> toOther, float distance, float distance2);
		public abstract Vector<float> ComputeCollisionImpulse(TSelf other, float engulfRelativeDistance);
		public abstract void Consume(TSelf other);
		protected virtual void AfterMove() { }
		public bool IsInRange(BaryCenter center) {
			Vector<float> toCenter = (center.Position - this._position) * (1f / Parameters.WORLD_SCALE);
			float distanceSquared = Vector.Dot(toCenter, toCenter);
			return distanceSquared <= Parameters.WORLD_PRUNE_RADII2//near enough the center
				|| this.SurviveOutOfBounds(center, distanceSquared);
		}
		protected virtual bool SurviveOutOfBounds(BaryCenter center, float distance2) => false;

		public float EngulfRelativeDistance(TSelf other, float distance) {//values <= 0 are fully engulfed, 1 is touching, and larger are separate
			TSelf smaller, larger;
			(smaller, larger) = this._radius <= other._radius
				? ((TSelf)this, other)
				: (other, (TSelf)this);
			return (distance + smaller._radius - larger._radius) / (2f * smaller._radius);
		}

		public Vector<float> ComputeInteractionInfluence(TSelf other) {
			Vector<float> toOther = other._position - this._position;
			float distance2 = Vector.Dot(toOther, toOther);
			float distance = MathF.Sqrt(distance2);

			if (Parameters.COLLISION_ENABLE && distance < (this._radius + other._radius) * Parameters.COLLISION_OVERLAP)
				(this.Collisions ??= new()).Enqueue(other);

			return distance > Parameters.PRECISION_EPSILON
				? this.ComputeInfluence(other, toOther, distance, distance2)
				: Vector<float>.Zero;
		}

		public void IntegrateMotion() {
			//Modified Taylor Series integration
			//see http://www.schlitt.net/xstar/n-body.pdf section 2.2.1
			Vector<float> displacement = this.Velocity + this.DragAcceleration;
			displacement += this.Age++ switch {
				0 => this.Acceleration,//Riemann sum
				1 => Parameters.TIME_SCALE_HALF * this.Acceleration
					+ Parameters.TIME_SCALE_SQUARED_SIXTH * this.Jerk1,
				_ => Parameters.TIME_SCALE_HALF * this.Acceleration
					+ Parameters.TIME_SCALE_SQUARED_SIXTH * this.Jerk2,
			};
			this._position += displacement * Parameters.TIME_SCALE;

			//check bounding conditions, including escape velocity
			if (Parameters.WORLD_BOUNCING || Parameters.WORLD_WRAPPING) {
				Vector<int>
					lesses = Vector.LessThan(this._position, Parameters.WORLD_LEFT_INF),
					greaters = Vector.GreaterThanOrEqual(this._position, Parameters.WORLD_RIGHT_INF),
					union = lesses | greaters;
				if (Vector.LessThanAny(union, Vector<int>.Zero)) {
					if (Parameters.WORLD_BOUNCING) {
						this._position =
							-this._position
							+ 2f*Vector.ConditionalSelect(lesses,
								Parameters.WORLD_LEFT,
								Vector.ConditionalSelect(greaters,
									Parameters.WORLD_RIGHT,
									this._position));
						displacement = Vector.ConditionalSelect(union, -displacement, displacement);
					} else this._position = WrapPosition(this._position);
				}
			}

			this.Velocity = displacement;
			//supernova and stuff
			this.AfterMove();
			//update history
			this._acceleration2 = this._acceleration1;
			this._acceleration1 = this.Acceleration;
		}

		public void WrapPosition() {
			this._position = WrapPosition(this._position);
		}

		public void BoundPosition() {
			this._position = BoundPosition(this._position);
		}

		public static Vector<float> WrapPosition(Vector<float> p) {
			//unrolled puke
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
			//unrolled puke
			Span<float> values = stackalloc float[Vector<float>.Count];
			values[0] = Parameters.DIM < 1 ? 0f :
				p[0] < Parameters.WORLD_LEFT[0]
				? Parameters.WORLD_LEFT[0]
				: p[0] >= Parameters.WORLD_RIGHT[0]
					? Parameters.WORLD_RIGHT[0] - Parameters.PRECISION_EPSILON
					: p[0];
			values[1] = Parameters.DIM < 1 ? 0f :
				p[1] < Parameters.WORLD_LEFT[1]
				? Parameters.WORLD_LEFT[1]
				: p[1] >= Parameters.WORLD_RIGHT[1]
					? Parameters.WORLD_RIGHT[1] - Parameters.PRECISION_EPSILON
					: p[1];
			values[2] = Parameters.DIM < 1 ? 0f :
				p[2] < Parameters.WORLD_LEFT[2]
				? Parameters.WORLD_LEFT[2]
				: p[2] >= Parameters.WORLD_RIGHT[2]
					? Parameters.WORLD_RIGHT[2] - Parameters.PRECISION_EPSILON
					: p[2];
			values[3] = Parameters.DIM < 1 ? 0f :
				p[3] < Parameters.WORLD_LEFT[3]
				? Parameters.WORLD_LEFT[3]
				: p[3] >= Parameters.WORLD_RIGHT[3]
					? Parameters.WORLD_RIGHT[3] - Parameters.PRECISION_EPSILON
					: p[3];
			values[4] = Parameters.DIM < 1 ? 0f :
				p[4] < Parameters.WORLD_LEFT[4]
				? Parameters.WORLD_LEFT[4]
				: p[4] >= Parameters.WORLD_RIGHT[4]
					? Parameters.WORLD_RIGHT[4] - Parameters.PRECISION_EPSILON
					: p[4];
			values[5] = Parameters.DIM < 1 ? 0f :
				p[5] < Parameters.WORLD_LEFT[5]
				? Parameters.WORLD_LEFT[5]
				: p[5] >= Parameters.WORLD_RIGHT[5]
					? Parameters.WORLD_RIGHT[5] - Parameters.PRECISION_EPSILON
					: p[5];
			values[6] = Parameters.DIM < 1 ? 0f :
				p[6] < Parameters.WORLD_LEFT[6]
				? Parameters.WORLD_LEFT[6]
				: p[6] >= Parameters.WORLD_RIGHT[6]
					? Parameters.WORLD_RIGHT[6] - Parameters.PRECISION_EPSILON
					: p[6];
			values[7] = Parameters.DIM < 1 ? 0f :
				p[7] < Parameters.WORLD_LEFT[7]
				? Parameters.WORLD_LEFT[7]
				: p[7] >= Parameters.WORLD_RIGHT[7]
					? Parameters.WORLD_RIGHT[7] - Parameters.PRECISION_EPSILON
					: p[7];
			return new Vector<float>(values);
		}
	}
}