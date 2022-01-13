using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Generic.Vectors;
using ParticleSimulator.Simulation.Particles;

namespace ParticleSimulator.Simulation {
	public class SpinningDisk<TParticle> : AParticleGroup<TParticle>
	where TParticle : AParticle<TParticle> {
		public SpinningDisk(Func<Vector<float>, Vector<float>, TParticle> initializer, float radius)
		: base(initializer, radius) { }

		protected override void InitPositionVelocity() {
			base.InitPositionVelocity();
			if (Parameters.PARTICLES_GROUP_COUNT > 1)
				this.Velocity +=
					Parameters.GRAVITY_STARTING_SPEED_MAX_GROUP
					* this.DirectionUnitVector(this.Position);
		}

		protected override void ParticleAddPositionVelocity(TParticle particle) {
			float rand = (float)Program.Engine.Random.NextDouble();
			float radiusRange = this.Radius * MathF.Pow(rand, Parameters.GALAXY_CONCENTRATION);
			Vector<float> offset = radiusRange * VectorFunctions.New(VectorFunctions.RandomCoordinate_Spherical(radiusRange, Parameters.DIM, Program.Engine.Random).Select(x => (float)x));
			float offsetGalaxySizeFraction = offset.Magnitude() / this.Radius;
			particle.Position += offset;
			Vector<float> velocityDirection = this.DirectionUnitVector(offset);
			particle.Velocity +=
				(float)Program.Engine.Random.NextDouble()
				* Parameters.GRAVITY_STARTING_SPEED_MAX_INTRAGROUP
				* offsetGalaxySizeFraction
				* velocityDirection;
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