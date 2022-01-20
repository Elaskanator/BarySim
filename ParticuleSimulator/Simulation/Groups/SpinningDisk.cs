using System;
using System.Linq;
using System.Numerics;
using Generic.Vectors;

namespace ParticleSimulator.Simulation.Particles {
	public class SpinningDisk<TParticle> : AParticleGroup<TParticle>
	where TParticle : AParticle<TParticle> {
		public SpinningDisk(Func<Vector<float>, Vector<float>, TParticle> initializer, float radius)
		: base(initializer, radius) {
			//this.GlobalDirection = Program.Engine.Random.NextDouble() < 0.5d;
			this.InternalDirection = Program.Engine.Random.NextDouble() < 0.5d;
		}

		public readonly bool GlobalDirection;
		public readonly bool InternalDirection;

		protected override void InitPositionVelocity() {
			base.InitPositionVelocity();
			this.Velocity +=
				  (this.GlobalDirection ? 1f : -1f)
				* Parameters.GALAXY_SPEED_ANGULAR
				* this.DirectionUnitVector(this.Position);
		}

		protected override void ParticleAddPositionVelocity(TParticle particle) {
			float rand = (float)Program.Engine.Random.NextDouble();
			float offset = this.Radius * MathF.Pow(rand, Parameters.GALAXY_CONCENTRATION);

			float[] offsetV;
			if (Parameters.DIM <= 2) {
				offsetV = VectorFunctions
					.RandomUnitVector_Spherical(Parameters.DIM, Program.Engine.Random)
					.Select(x => offset*x)
					.ToArray();
			} else{
				offsetV = VectorFunctions
					.RandomUnitVector_Spherical(2, Program.Engine.Random)
					.Select(x => offset*x)
					.Concat(Enumerable.Repeat(0f, Vector<float>.Count - 2))
					.ToArray();
				float offset2 = (this.Radius*this.Radius - offset*offset) / (this.Radius * this.Radius);
				float rand2 = MathF.Pow((float)Program.Engine.Random.NextDouble(), Parameters.GALAXY_CONCENTRATION);
				offset2 *= rand * rand2 * this.Radius / Parameters.GALAXY_THINNESS;
				float[] offsetV2 = VectorFunctions
					.RandomUnitVector_Spherical(Parameters.DIM - 2, Program.Engine.Random)
					.Select(x => offset2*x)
					.ToArray();
				for (int i = 0; i < Parameters.DIM - 2; i++)
					offsetV[i + 2] = offsetV2[i];
			}

			Vector<float> positionOffset = VectorFunctions.New(offsetV);
			particle.Position += positionOffset;

			particle.Velocity +=
			  (   (this.InternalDirection ? 1f : -1f)
				* Parameters.GALAXY_SPIN_ANGULAR
				* MathF.Pow(offset / this.Radius, Parameters.GALAXY_SPIN_POW)
				* this.DirectionUnitVector(positionOffset))
			+ (   new Vector<float>(Parameters.GALAXY_SPIN_RAND) * MathF.Pow(1f - (offset / this.Radius), Parameters.GALAXY_SPIN_POW)
				* VectorFunctions.New(VectorFunctions.RandomUnitVector_Spherical(Parameters.DIM, Program.Engine.Random)));
		}

		private Vector<float> DirectionUnitVector(Vector<float> offset) {
			if (Parameters.DIM > 1) {
				float angle = MathF.Atan2(offset[1], offset[0]) + 0.5f*MathF.PI;
				return VectorFunctions.New(new float[] {
					MathF.Cos(angle),
					MathF.Sin(angle) });
			} else return Vector<float>.Zero;
		}
	}
}