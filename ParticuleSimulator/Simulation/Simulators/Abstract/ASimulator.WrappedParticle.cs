using Generic.Vectors;

namespace ParticleSimulator.Simulation {
	public abstract partial class ASimulator<TParticle>
	where TParticle : ABaryonParticle<TParticle> {
		public sealed class WrappedParticle {
			public readonly TParticle Particle;
			public WrappedParticle(TParticle instance) {
				this.Particle = instance;
				this.HandleBounds();
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
					if (this.Particle.Position[d] < 0f) {
						coords[d] = (this.Particle.Position[d] % Parameters.DOMAIN_SIZE[d]) + Parameters.DOMAIN_SIZE[d];//don't want symmetric modulus
						result = true;
					} else if (this.Particle.Position[d] >= Parameters.DOMAIN_SIZE[d]) {
						coords[d] = this.Particle.Position[d] % Parameters.DOMAIN_SIZE[d];
						result = true;
					} else coords[d] = this.Particle.Position[d];

				if (result)
					this.Particle.Position = VectorFunctions.New(coords);
				return result;
			}
			public bool BoundPosition() {
				bool result = false;
				float[] coords = new float[Parameters.DIM];
				for (int d = 0; d < Parameters.DIM; d++) 
					if (this.Particle.Position[d] < 0f) {
						coords[d] = 0f;
						result = true;
					} else if (this.Particle.Position[d] >= Parameters.DOMAIN_SIZE[d]) {
						coords[d] = Parameters.DOMAIN_SIZE[d] - Parameters.WORLD_EPSILON;
						result = true;
					}
				
				if (result)
					this.Particle.Position = VectorFunctions.New(coords);
				return result;
			}

			public void CheckOutOfBounds() {
				for (int d = 0; d < Parameters.DIM; d++)
					if (this.Particle.Position[d] < -Parameters.WORLD_DEATH_BOUND_CNT * Parameters.DOMAIN_SIZE[d]
					|| this.Particle.Position[d] > Parameters.DOMAIN_SIZE[d] * (1f + Parameters.WORLD_DEATH_BOUND_CNT))
						this.Particle.IsEnabled = false;
			}

			//public void BounceVelocity(float weight) {
			//	float dist;
			//	bool result = false;
			//	float[] coords = new float[Parameters.DIM];
			//	for (int d = 0; d < Parameters.DIM; d++) {
			//		dist = this.Particle.Position[d] - Parameters.DOMAIN_CENTER[d];
			//		if (dist < -Parameters.DOMAIN_MAX_RADIUS) {
			//			coords[d] = this.Particle.Velocity[d] + weight * MathF.Pow(Parameters.DOMAIN_MAX_RADIUS - dist, 0.5f);
			//			result = true;
			//		} else if (dist > Parameters.DOMAIN_MAX_RADIUS){
			//			coords[d] = this.Particle.Velocity[d] - weight * MathF.Pow(dist - Parameters.DOMAIN_MAX_RADIUS, 0.5f);
			//			result = true;
			//		}
			//	}
			//	if (result)
			//		this.Particle.Velocity = VectorFunctions.New(coords);
			//}
		}
	}
}