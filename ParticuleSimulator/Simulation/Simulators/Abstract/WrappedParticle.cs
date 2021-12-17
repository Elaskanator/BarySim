using System;
using Generic.Models;
using Generic.Vectors;

namespace ParticleSimulator.Simulation {
	public abstract partial class ASimulator<TParticle, TTree> where TParticle : ABaryonParticle<TParticle>
	where TTree : AQuadTree<TParticle, TTree> {
		public sealed class WrappedParticle {
			public readonly TParticle Particle;
			public TTree Node { get; private set; }
			public bool IsEnabled = true;
			public WrappedParticle(TParticle instance, TTree node) {
				this.Particle = instance;
				this.Node = node;
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
			public void BounceVelocity(float weight) {
				float dist;
				bool result = false;
				float[] coords = new float[Parameters.DIM];
				for (int d = 0; d < Parameters.DIM; d++) {
					dist = this.Particle.Position[d] - Parameters.DOMAIN_CENTER[d];
					if (dist < -Parameters.DOMAIN_MAX_RADIUS) {
						coords[d] = this.Particle.Velocity[d] + weight * MathF.Pow(Parameters.DOMAIN_MAX_RADIUS - dist, 0.5f);
						result = true;
					} else if (dist > Parameters.DOMAIN_MAX_RADIUS){
						coords[d] = this.Particle.Velocity[d] - weight * MathF.Pow(dist - Parameters.DOMAIN_MAX_RADIUS, 0.5f);
						result = true;
					}
				}
				if (result)
					this.Particle.Velocity = VectorFunctions.New(coords);
			}

			public void UpdateParentNode() {
				TTree node = this.Node;
				while (!node.DoesContainCoordinates(this.Particle.Position))
					if (node.IsRoot) {
						this.Node = node.AddUp(this.Particle);
						return;
					}
					else node = node.Parent;
				this.Node = node.GetContainingLeaf(this.Particle.Position);
			}
		}
	}
}