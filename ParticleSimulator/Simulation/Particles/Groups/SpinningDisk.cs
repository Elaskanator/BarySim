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
			this.InternalDirection = Program.Random.NextDouble() < 0.5d;
		}

		public readonly bool GlobalDirection;
		public readonly bool InternalDirection;
		
		protected override void InitGroupPositionVelocity() {
			base.InitGroupPositionVelocity();
			if (Parameters.PARTICLES_GROUP_COUNT > 1) {
				this.Velocity +=
					  (this.GlobalDirection ? 1f : -1f)
					* Parameters.GALAXY_SPEED_ANGULAR
					* this.DirectionUnitVector2d(this.Position);
			} else this.Position = Vector<float>.Zero;
		}

		protected override void InitializeParticle(TParticle particle) {
			float rand = (float)Program.Random.NextDouble();
			//float offset = this.Radius * MathF.Pow(rand, Parameters.GALAXY_CONCENTRATION);
			//float offset = this.Radius * RandomMidBiased(Parameters.GALAXY_INNER_BIAS, Parameters.GALAXY_OUTER_BIAS);
			float offset = RandomExponentialDiskRadius(this.Radius);

			float[] offsetV;
			if (Parameters.DIM <= 2) {
				offsetV = [.. VectorFunctions
					.RandomDirectionVector(Parameters.DIM, Program.Random)
					.ToArray()
					.Select(x => offset*x)];
			} else{
				offsetV =
				[
					.. VectorFunctions
						.RandomDirectionVector(2, Program.Random)
						.ToArray()
						.Select(x => offset*x),
					.. Enumerable.Repeat(0f, Vector<float>.Count - 2),
				];
				float offset2 = (this.Radius*this.Radius - offset*offset) / (this.Radius * this.Radius);
				//float rand2 = MathF.Pow((float)Program.Random.NextDouble(), Parameters.GALAXY_CONCENTRATION);
				float rand2 = this.Radius * MathF.Pow((float)Program.Random.NextDouble(), Parameters.GALAXY_THINNESS_BIAS);;
				offset2 *= rand2 / Parameters.GALAXY_THINNESS_RATIO;
				float[] offsetV2 = [.. VectorFunctions
					.RandomDirectionVector(Parameters.DIM - 2, Program.Random)
					.ToArray()
					.Select(x => offset2*x)];
				for (int i = 0; i < Parameters.DIM - 2; i++)
					offsetV[i + 2] = offsetV2[i];
			}

			Vector<float> positionOffset = VectorFunctions.New(offsetV);
			particle._position += positionOffset;


			particle.Velocity +=
				Parameters.GALAXY_SPIN_ANGULAR
				*	(	(	(this.InternalDirection ? 1f : -1f)
			  				* MathF.Pow(offset / this.Radius, Parameters.GALAXY_SPIN_POW_TORTION)
			  				* this.DirectionUnitVector2d(positionOffset))
						+	(	Parameters.GALAXY_PARTICLE_VEL_RAND * MathF.Pow(1f - (offset / this.Radius), Parameters.GALAXY_SPIN_POW_TORTION)
								* VectorFunctions.RandomDirectionVector(Parameters.DIM, Program.Random)));
		}
		//private static float RandomMidBiased(float innerBias, float outerBias) {
		//	float u = MathF.Pow((float)Program.Random.NextDouble(), innerBias);
		//	float v = MathF.Pow((float)Program.Random.NextDouble(), outerBias);
		//	return u / (u + v);
		//}
		private static float RandomExponentialDiskRadius(float maxRadius)
		{
			float scaleRadius = maxRadius * Parameters.GALAXY_INNER_DIFFUSENESS;
			float r;
			do
			{
				float u1 = MathF.Max((float)Program.Random.NextDouble(), float.Epsilon);
				float u2 = MathF.Max((float)Program.Random.NextDouble(), float.Epsilon);
				r = -scaleRadius * MathF.Log(u1 * u2);
			} while (r > maxRadius);

			return r;
		}

		private Vector<float> DirectionUnitVector2d(Vector<float> offset) {
			if (Parameters.DIM > 1) {
				float angle = MathF.Atan2(offset[1], offset[0]) + 0.5f*MathF.PI;
				return VectorFunctions.New([ MathF.Cos(angle), MathF.Sin(angle) ]);
			} else return Vector<float>.Zero;
		}
	}
}