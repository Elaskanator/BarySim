using System;
using System.Collections.Generic;
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
				* Parameters.GRAVITY_STARTING_SPEED_MAX_GROUP
				* this.DirectionUnitVector(this.Position);
		}

		protected override void ParticleAddPositionVelocity(TParticle particle) {
			float rand = (float)Program.Engine.Random.NextDouble();
			Vector<float> offset = VectorFunctions.New(
				VectorFunctions.RandomCoordinate_Spherical(
					this.Radius * MathF.Pow(rand, Parameters.GALAXY_CONCENTRATION),
					Parameters.DIM,
					Program.Engine.Random)
				.Select(x => (float)x));
			particle.Position += offset;
			particle.Velocity +=
				  (this.InternalDirection ? 1f : -1f)
				* (float)Program.Engine.Random.NextDouble()
				* Parameters.GRAVITY_STARTING_SPEED_MAX_INTRAGROUP
				* MathF.Pow(offset.Magnitude() / this.Radius, Parameters.GALAXY_RADIAL_SPEED_POW)
				* this.DirectionUnitVector(offset);
		}

		private Vector<float> DirectionUnitVector(Vector<float> offset) {
			Vector<float> result;
			if (Parameters.DIM == 1) {
				result = VectorFunctions.New(offset[0] < 0f ? -1f : 1f);
			} else {
				float angle = MathF.Atan2(offset[1], offset[0]);
				angle += 2f * MathF.PI
					* (0.25f//90 degree rotation
						+ (MathF.Pow((float)Program.Engine.Random.NextDouble(), Parameters.GRAVITY_ALIGNMENT_SKEW_POW)
							* Parameters.GRAVITY_ALIGNMENT_SKEW_RANGE_PCT / 100f));

				IEnumerable<float> rotation = new float[] {
					MathF.Cos(angle),
					MathF.Sin(angle) };

				result = VectorFunctions.New(rotation);
			}
			return result;
		}
	}
}