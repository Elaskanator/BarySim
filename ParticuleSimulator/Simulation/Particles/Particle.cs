using System;
using System.Numerics;
using Generic.Vectors;

namespace ParticleSimulator {
	public interface IParticle : IMultidimensionalFloat, IEquatable<IParticle> {
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

		public Particle() { this.IsEnabled = true; }

		public override string ToString() => string.Format("Particle[<{0}> ID {1}]", this.ID, string.Join("", this.Position));

		public int ID => this._id;
		public int GroupID { get; set; }
		public bool IsEnabled { get; set; }

		public virtual float Mass { get; set; }
		public virtual float Charge { get; set; }
		public virtual float Radius { get; set; }
		//public virtual float Density { get; set; }
		public virtual float Luminosity { get; set; }
		
		public Vector<float> Position { get; set; }
		public Vector<float> Momentum { get; set; }
		public virtual Vector<float> Velocity {
			get => this.Momentum * (1f / this.Mass);
			set { this.Momentum = this.Mass * value; }}

		public void ApplyTimeStep(Vector<float> acceleration, float timeStep) {
			/*this.Velocity += acceleration
				.Clamp(Parameters.PARTICLE_MAX_ACCELERATION / timeStep)
				* timeStep;*/
			this.Position += this.Velocity;// * timeStep;
		}

		public void HandleBounds(float timeStep) {
		//	if (Parameters.WORLD_WRAPPING)
		//		this.WrapPosition();
			if (Parameters.WORLD_BOUNCING)
				this.BounceWalls(timeStep);
		//	else if (Parameters.WORLD_BOUNDING)
		//		this.BoundPosition();
		//	else this.CheckOutOfBounds();
		}

		//private void CheckOutOfBounds() {
		//	for (int d = 0; d < Parameters.DIM; d++)
		//		if (this.Position[d] < -Parameters.WORLD_DEATH_BOUND_CNT * Parameters.WORLD_SCALE
		//		|| this.Position[d] > Parameters.WORLD_SCALE * (1f + Parameters.WORLD_DEATH_BOUND_CNT))
		//			this.IsEnabled = false;
		//}

		//private bool WrapPosition() {
		//	bool result = false;
		//	float[] coords = new float[Parameters.DIM];
		//	for (int d = 0; d < Parameters.DIM; d++)
		//		if (this.Position[d] < 0f) {
		//			coords[d] = (this.Position[d] % Parameters.DOMAIN_SIZE[d]) + Parameters.DOMAIN_SIZE[d];//don't want symmetric modulus
		//			result = true;
		//		} else if (this.Position[d] >= Parameters.DOMAIN_SIZE[d]) {
		//			coords[d] = this.Position[d] % Parameters.DOMAIN_SIZE[d];
		//			result = true;
		//		} else coords[d] = this.Position[d];

		//	if (result)
		//		this.Position = VectorFunctions.New(coords);
		//	return result;
		//}

		//private bool BoundPosition() {
		//	bool result = false;
		//	float[] coords = new float[Parameters.DIM];
		//	for (int d = 0; d < Parameters.DIM; d++) 
		//		if (this.Position[d] - this.Radius < 0f) {
		//			coords[d] = this.Radius;
		//			result = true;
		//		} else if (this.Position[d] + this.Radius > Parameters.DOMAIN_SIZE[d]) {
		//			coords[d] = Parameters.DOMAIN_SIZE[d] - this.Radius;
		//			result = true;
		//		} else coords[d] = this.Position[d];
				
		//	if (result)
		//		this.Position = VectorFunctions.New(coords);
		//	return result;
		//}

		public bool BounceWalls(float timeStep) {//TODODODO
			bool result = false;
			Vector<int>
				lessThans = Vector.LessThan(this.Position + timeStep*this.Velocity, Parameters.LEFT_BOUND),
				greaterThans = Vector.GreaterThanOrEqual(this.Position + timeStep*this.Velocity, Parameters.RIGHT_BOUND);
			if (Vector.LessThanAny(lessThans, Vector<int>.Zero) || Vector.LessThanAny(greaterThans, Vector<int>.Zero)) {
				this.Velocity = Vector.ConditionalSelect(
					lessThans,
					-this.Velocity,
					Vector.ConditionalSelect(
						greaterThans,
						-this.Velocity,
						this.Velocity));
				this.Position += Vector.ConditionalSelect(//v is reversed already
					lessThans,
					timeStep*this.Velocity - (this.Position - Parameters.LEFT_BOUND),
					Vector.ConditionalSelect(
						greaterThans,
						timeStep*this.Velocity - (Parameters.RIGHT_BOUND - this.Position),
						Vector<float>.Zero));
			}
			
			/*
			float radius = Parameters.WORLD_BOUNCING_EXTENSION ? 0f : this.Radius;
			float[] coords = new float[Parameters.DIM],
				velocity = new float[Parameters.DIM];
			for (int d = 0; d < Parameters.DIM; d++) {
				coords[d] = this.Position[d];
				velocity[d] = this.Velocity[d];
				if (this.Position[d] - radius < -Parameters.WORLD_SCALE) {
					coords[d] = radius - Parameters.WORLD_SCALE;//TODODODO
					result = true;
				}
				if (this.Position[d] + radius > Parameters.WORLD_SCALE) {
					coords[d] = Parameters.WORLD_SCALE - radius;//TODODODO
					result = true;
				}

				if (this.Position[d] - radius + this.Velocity[d]*timeStep < -Parameters.WORLD_SCALE) {
					velocity[d] = -this.Velocity[d];
					coords[d] = radius - Parameters.WORLD_SCALE;
					result = true;
				} else if (this.Position[d] + radius + this.Velocity[d]*timeStep > Parameters.WORLD_SCALE) {
					velocity[d] = -this.Velocity[d];
					coords[d] = Parameters.WORLD_SCALE - radius;
					result = true;
				}
			}

			if (result) {
				this.Position = VectorFunctions.New(coords);
				this.Velocity = VectorFunctions.New(velocity);
			}
			*/
			return result;
		}
	}
}