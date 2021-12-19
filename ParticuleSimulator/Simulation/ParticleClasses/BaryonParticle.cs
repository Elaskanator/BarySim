using System.Linq;
using System.Numerics;
using Generic.Vectors;

namespace ParticleSimulator {
	public interface IBaryonParticle : IParticle {
		int ID { get; }
		int GroupID { get; }
		float Radius { get; }
		float Density { get; }
		float Luminosity { get; }
		
		bool Equals(object other) => (other is IBaryonParticle data) && this.ID == data.ID;
		int GetHashCode() => this.ID;
	}

	public class BaryonParticle : IBaryonParticle{
		public BaryonParticle() {
			this.IsEnabled = true;
		}
		public override string ToString() =>
			string.Format("Particle[<{0}> ID {1}]", this.ID,
				string.Join(",", Enumerable.Range(0, Parameters.DIM).Select(d => this.Position[d])));
		
		private static int _globalID = 0;
		private readonly int _id = ++_globalID;
		public int ID => this._id;
		public int GroupID { get; set; }
		public bool IsEnabled { get; set; }

		public virtual float Mass { get; set; }
		public virtual float Charge { get; set; }
		public virtual float Radius { get; set; }
		public virtual float Density { get; set; }
		public virtual float Luminosity { get; set; }
		
		public Vector<float> Position { get; set; }
		public Vector<float> Momentum { get; set; }
		public virtual Vector<float> Velocity {
			get => this.Momentum * (1f / this.Mass);
			set { this.Momentum = this.Mass * value; }}

		public void ApplyTimeStep(Vector<float> acceleration, float timeStep) {
			this.Velocity += acceleration
				.Clamp(Parameters.PARTICLE_MAX_ACCELERATION / timeStep)
				* timeStep;
			this.Position += this.Velocity * timeStep;
		}

		public void HandleBounds() {
			if (Parameters.WORLD_WRAPPING)
				this.WrapPosition();
			else if (Parameters.WORLD_BOUNDING)
				this.BoundPosition();
			else this.CheckOutOfBounds();
		}

		public bool WrapPosition() {
			bool result = false;
			float[] coords = new float[Parameters.DIM];
			for (int d = 0; d < Parameters.DIM; d++)
				if (this.Position[d] < 0f) {
					coords[d] = (this.Position[d] % Parameters.DOMAIN_SIZE[d]) + Parameters.DOMAIN_SIZE[d];//don't want symmetric modulus
					result = true;
				} else if (this.Position[d] >= Parameters.DOMAIN_SIZE[d]) {
					coords[d] = this.Position[d] % Parameters.DOMAIN_SIZE[d];
					result = true;
				} else coords[d] = this.Position[d];

			if (result)
				this.Position = VectorFunctions.New(coords);
			return result;
		}
		public bool BoundPosition() {
			bool result = false;
			float[] coords = new float[Parameters.DIM];
			for (int d = 0; d < Parameters.DIM; d++) 
				if (this.Position[d] < 0f) {
					coords[d] = 0f;
					result = true;
				} else if (this.Position[d] >= Parameters.DOMAIN_SIZE[d]) {
					coords[d] = Parameters.DOMAIN_SIZE[d] - Parameters.WORLD_EPSILON;
					result = true;
				}
				
			if (result)
				this.Position = VectorFunctions.New(coords);
			return result;
		}

		public void CheckOutOfBounds() {
			for (int d = 0; d < Parameters.DIM; d++)
				if (this.Position[d] < -Parameters.WORLD_DEATH_BOUND_CNT * Parameters.DOMAIN_SIZE[d]
				|| this.Position[d] > Parameters.DOMAIN_SIZE[d] * (1f + Parameters.WORLD_DEATH_BOUND_CNT))
					this.IsEnabled = false;
		}
	}
}