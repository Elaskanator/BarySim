using System;
using System.Collections.Generic;
using System.Numerics;
using Generic.Trees;
using ParticleSimulator.Simulation.Baryon;

namespace ParticleSimulator.Simulation.Particles {
	public abstract class AParticle<TSelf> : IParticle
	where TSelf : AParticle<TSelf>{
		private static int _globalID = 0;
		private readonly int _id = ++_globalID;

		protected AParticle(Vector<float> position, Vector<float> velocity) {
			this.Position = position;
			this.Velocity = velocity;
			this.Acceleration = Vector<float>.Zero;
			this.Enabled = true;
		}

		public override string ToString() => string.Format("Particle[<{0}> ID {1}]", this.Id, string.Join("", this.Position));

		public int Id => this._id;
		public int GroupId { get; set; }
		public bool Enabled { get; set; }
		
		public float Luminosity { get; set; }
		public virtual float Density => 1f;

		private float _radius;
		public float Radius {
			get => this._radius;
			set { this._radius = value; this.RadiusSquared = value * value; } }
		public float RadiusSquared { get; private set; }
		
		public Vector<float> Position { get; set; }
		public Vector<float> Velocity { get; set; }
		public Vector<float> Acceleration { get; set; }

		public Queue<TSelf> Mergers = null;
		public Queue<TSelf> NewParticles = null;

		//Tuple<Gravity, Drag>
		public abstract Tuple<Vector<float>, Vector<float>> ComputeInfluence(TSelf other);
		public abstract void Incorporate(TSelf other);

		protected virtual void AfterMove() { }

		protected virtual bool IsInRange(BaryCenter center) => false;

		public void ApplyTimeStep(float timeStep, BaryCenter center) {
			Vector<float> velocity = this.Velocity + (timeStep * this.Acceleration),
				displacement = timeStep*velocity,
				position = this.Position + displacement;

			if (Parameters.WORLD_BOUNCING || Parameters.WORLD_WRAPPING) {
				Vector<int>
					lesses = Vector.LessThan(position, Parameters.WORLD_LEFT_INF),
					greaters = Vector.GreaterThanOrEqual(position, Parameters.WORLD_RIGHT_INF),
					union = lesses | greaters;
				if (Vector.LessThanAny(union, Vector<int>.Zero)) {
					if (Parameters.WORLD_BOUNCING) {
						position = -position
							+ 2f * Vector.ConditionalSelect(lesses,
								Parameters.WORLD_LEFT,
								Vector.ConditionalSelect(greaters,
									Parameters.WORLD_RIGHT,
									position));
						velocity = Vector.ConditionalSelect(union, -velocity, velocity);
					} else {// if (Parameters.WORLD_WRAPPING) {
						position = WrapPosition(position);
					}
				}
			} else if (Parameters.WORLD_DEATH_BOUND_RADIUS > 0f) {
				this.Enabled &= this.IsInRange(center);
			}

			this.Position = position;
			this.Velocity = velocity;

			this.AfterMove();
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