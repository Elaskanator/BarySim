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
			if (!Parameters.WORLD_BOUNCING || !this.BounceWalls(timeStep))
				/*this.Velocity += acceleration
					.Clamp(Parameters.PARTICLE_MAX_ACCELERATION / timeStep)
					* timeStep;*/
				this.Position += this.Velocity;// * timeStep;
		}

		//private void CheckOutOfBounds() {
		//	for (int d = 0; d < Parameters.DIM; d++)
		//		if (this.Position[d] < -Parameters.WORLD_DEATH_BOUND_CNT * Parameters.WORLD_SCALE
		//		|| this.Position[d] > Parameters.WORLD_SCALE * (1f + Parameters.WORLD_DEATH_BOUND_CNT))
		//			this.IsEnabled = false;
		//}

		public void WrapPosition() {
			Span<float> values = stackalloc float[Vector<float>.Count];
			values[0] = Parameters.DIM < 1 ? 0f :
				this.Position[0] < Parameters.WORLD_LEFT[0]
				? Parameters.WORLD_LEFT[0] + Parameters.WORLD_SIZE[0] + ((this.Position[0] - Parameters.WORLD_LEFT[0]) % Parameters.WORLD_SIZE[0])
				: this.Position[0] >= Parameters.WORLD_RIGHT[0]
					? Parameters.WORLD_LEFT[0] + ((this.Position[0] - Parameters.WORLD_LEFT[0]) % Parameters.WORLD_SIZE[0])
					: this.Position[0];
			values[1] = Parameters.DIM < 2 ? 0f :
				this.Position[1] < Parameters.WORLD_LEFT[1]
				? Parameters.WORLD_LEFT[1] + Parameters.WORLD_SIZE[1] + ((this.Position[1] - Parameters.WORLD_LEFT[1]) % Parameters.WORLD_SIZE[1])
				: this.Position[1] >= Parameters.WORLD_RIGHT[1]
					? Parameters.WORLD_LEFT[1] + ((this.Position[1] - Parameters.WORLD_LEFT[1]) % Parameters.WORLD_SIZE[1])
					: this.Position[1];
			values[2] = Parameters.DIM < 3 ? 0f :
				this.Position[2] < Parameters.WORLD_LEFT[2]
				? Parameters.WORLD_LEFT[2] + Parameters.WORLD_SIZE[2] + ((this.Position[2] - Parameters.WORLD_LEFT[2]) % Parameters.WORLD_SIZE[2])
				: this.Position[2] >= Parameters.WORLD_RIGHT[2]
					? Parameters.WORLD_LEFT[2] + ((this.Position[2] - Parameters.WORLD_LEFT[2]) % Parameters.WORLD_SIZE[2])
					: this.Position[2];
			values[3] = Parameters.DIM < 4 ? 0f :
				this.Position[3] < Parameters.WORLD_LEFT[3]
				? Parameters.WORLD_LEFT[3] + Parameters.WORLD_SIZE[3] + ((this.Position[3] - Parameters.WORLD_LEFT[3]) % Parameters.WORLD_SIZE[3])
				: this.Position[3] >= Parameters.WORLD_RIGHT[3]
					? Parameters.WORLD_LEFT[3] + ((this.Position[3] - Parameters.WORLD_LEFT[3]) % Parameters.WORLD_SIZE[3])
					: this.Position[3];
			values[4] = Parameters.DIM < 5 ? 0f :
				this.Position[4] < Parameters.WORLD_LEFT[4]
				? Parameters.WORLD_LEFT[4] + Parameters.WORLD_SIZE[4] + ((this.Position[4] - Parameters.WORLD_LEFT[4]) % Parameters.WORLD_SIZE[4])
				: this.Position[4] >= Parameters.WORLD_RIGHT[4]
					? Parameters.WORLD_LEFT[4] + ((this.Position[4] - Parameters.WORLD_LEFT[4]) % Parameters.WORLD_SIZE[4])
					: this.Position[4];
			values[5] = Parameters.DIM < 6 ? 0f :
				this.Position[5] < Parameters.WORLD_LEFT[5]
				? Parameters.WORLD_LEFT[5] + Parameters.WORLD_SIZE[5] + ((this.Position[5] - Parameters.WORLD_LEFT[5]) % Parameters.WORLD_SIZE[5])
				: this.Position[5] >= Parameters.WORLD_RIGHT[5]
					? Parameters.WORLD_LEFT[5] + ((this.Position[5] - Parameters.WORLD_LEFT[5]) % Parameters.WORLD_SIZE[5])
					: this.Position[5];
			values[6] = Parameters.DIM < 7 ? 0f :
				this.Position[6] < Parameters.WORLD_LEFT[6]
				? Parameters.WORLD_LEFT[6] + Parameters.WORLD_SIZE[6] + ((this.Position[6] - Parameters.WORLD_LEFT[6]) % Parameters.WORLD_SIZE[6])
				: this.Position[6] >= Parameters.WORLD_RIGHT[6]
					? Parameters.WORLD_LEFT[6] + ((this.Position[6] - Parameters.WORLD_LEFT[6]) % Parameters.WORLD_SIZE[6])
					: this.Position[6];
			values[7] = Parameters.DIM < 8 ? 0f :
				this.Position[7] < Parameters.WORLD_LEFT[7]
				? Parameters.WORLD_LEFT[7] + Parameters.WORLD_SIZE[7] + ((this.Position[7] - Parameters.WORLD_LEFT[7]) % Parameters.WORLD_SIZE[7])
				: this.Position[7] >= Parameters.WORLD_RIGHT[7]
					? Parameters.WORLD_LEFT[7] + ((this.Position[7] - Parameters.WORLD_LEFT[7]) % Parameters.WORLD_SIZE[7])
					: this.Position[7];
			this.Position = new Vector<float>(values);
		}

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
			Vector<float> velocity = this.Velocity;
			Vector<float> displacement = timeStep*velocity;
			Vector<float> newP = this.Position + displacement;
			bool result = false;
			Vector<int>
				lessThans = Vector.LessThan(newP, Parameters.WORLD_LEFT),
				greaterThans = Vector.ConditionalSelect(
					VectorFunctions.DimensionSignals[Parameters.DIM],
					Vector.GreaterThanOrEqual(newP, Parameters.WORLD_RIGHT),
					Vector<int>.Zero);
			if (Vector.LessThanAny(lessThans, Vector<int>.Zero) || Vector.LessThanAny(greaterThans, Vector<int>.Zero)) {
				result = true;
				velocity = Vector.ConditionalSelect(
					lessThans,
					-velocity,
					Vector.ConditionalSelect(
						greaterThans,
						-velocity,
						velocity));
				this.Position += Vector.ConditionalSelect(//original velocity
					lessThans,
					Parameters.WORLD_LEFT - this.Position - displacement,
					Vector.ConditionalSelect(
						greaterThans,
						this.Position - Parameters.WORLD_RIGHT + displacement,
						Vector<float>.Zero));
				this.Velocity = velocity;
			}
			return result;
		}
	}
}