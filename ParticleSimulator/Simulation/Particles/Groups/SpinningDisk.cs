using System;
using System.Linq;
using System.Numerics;
using Generic.Vectors;
using ParticleSimulator.Simulation.Baryon;

namespace ParticleSimulator.Simulation.Particles.Groups;

public class SpinningDisk : AParticleGroup<MatterClump, BarnesHutTree>{
	public SpinningDisk(Func<Vector<float>, Vector<float>, MatterClump> initializer)
	: base(initializer) {
		//this.GlobalDirection = Program.Engine.Random.NextDouble() < 0.5d;
		this.InternalDirection = Program.Random.NextDouble() < 0.5d;

		this.InitialRadius = GetGalaxyRadius(this.NumParticles);
		this.EdgeSpeed = GetStableEdgeSpeed(this.NumParticles, this.InitialRadius) * Parameters.GALAXY_SPIN_FACTOR;
	}
	public static float GetGalaxyRadius(int particleCount) =>
		MathF.Sqrt(particleCount / (MathF.PI * Parameters.GALAXY_STAR_DENSITY));
	public static float GetStableEdgeSpeed(int particleCount, float radius) =>
		MathF.Sqrt(particleCount * Parameters.MASS_SCALAR * Parameters.GRAVITATIONAL_CONSTANT / radius);

	public readonly bool GlobalDirection;
	public readonly bool InternalDirection;

	public readonly float InitialRadius;
	public readonly float EdgeSpeed;

	protected override void InitGroupPositionVelocity() {
		base.InitGroupPositionVelocity();
		if (Parameters.PARTICLES_GROUP_COUNT > 1) {
			this.Velocity +=
				(this.GlobalDirection ? 1f : -1f)
				* Parameters.GALAXY_SPEED_ANGULAR
				* DirectionUnitVector2d(this.Position);
		} else this.Position = Vector<float>.Zero;
	}

	protected override void InitializeParticle(MatterClump particle) {
		float rand = (float)Program.Random.NextDouble();
		//float offset = this.Radius * MathF.Pow(rand, Parameters.GALAXY_CONCENTRATION);
		//float offset = this.Radius * RandomMidBiased(Parameters.GALAXY_INNER_BIAS, Parameters.GALAXY_OUTER_BIAS);
		float offset = RandomExponentialDiskRadius(this.InitialRadius);
		float proportionalOffset = offset / this.InitialRadius;

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
			// Inverse hyperbolic secant term (1/cosh(kx)): Models the central bulge, producing a high central thickness that decays smoothly with radius
			// Superellipse term ((1 - x^p)^(1/p)): Shapes the outer disk profile, controlling how rounded the disk remains as it approaches the galactic edge
			float offset2 =
				1f / MathF.Cosh(Parameters.GALAXY_BULGE_STEEPNESS * proportionalOffset)
				+ Parameters.GALAXY_MIDRANGE_FLOOR
				* (1f - 1f / MathF.Cosh(Parameters.GALAXY_BULGE_STEEPNESS * proportionalOffset))
				* MathF.Pow(
					1f - MathF.Pow(proportionalOffset, Parameters.GALAXY_EDGE_POWER),
					1f / Parameters.GALAXY_EDGE_POWER);
			offset2 *= this.InitialRadius * Parameters.GALAXY_THINNESS
				* MathF.Pow((float)Program.Random.NextDouble(), Parameters.GALAXY_THINNESS_BIAS);

			float[] offsetV2 = [.. VectorFunctions
				.RandomDirectionVector(Parameters.DIM - 2, Program.Random)
				.ToArray()
				.Select(x => offset2*x)];
			for (int i = 0; i < Parameters.DIM - 2; i++)
				offsetV[i + 2] = offsetV2[i];
		}

		Vector<float> positionOffset = VectorFunctions.New(offsetV);
		particle._position += positionOffset;
			
		Vector<float> randomVelocity = Parameters.GALAXY_STAR_VEL_RAND == 0f
			? Vector<float>.Zero
			: this.EdgeSpeed
			  * Parameters.GALAXY_STAR_VEL_RAND
			  * MathF.Pow(1f - proportionalOffset, Parameters.GALAXY_STAR_RAND_VEL_BIAS)
			  * VectorFunctions.RandomDirectionVector(Parameters.DIM, Program.Random);

		float diffusionScaledOffset = proportionalOffset / Parameters.GALAXY_OUTER_BIAS;
		float enclosedMassFraction = 1f - MathF.Exp(-diffusionScaledOffset) * (1f + diffusionScaledOffset);
		float tangentialSpeed = MathF.Sqrt(
			this.NumParticles
			* Parameters.MASS_SCALAR
			* Parameters.GRAVITATIONAL_CONSTANT
			* enclosedMassFraction
			/ MathF.Max(offset, Parameters.PRECISION_EPSILON));
		float tangentialSupport = diffusionScaledOffset / (1f + diffusionScaledOffset);
		tangentialSpeed *= tangentialSupport;

		particle.Velocity +=
			tangentialSpeed
			* Parameters.GALAXY_SPIN_FACTOR
			* (this.InternalDirection ? 1f : -1f)
			* DirectionUnitVector2d(positionOffset)
			+ randomVelocity;
	}
	//private static float RandomMidBiased(float innerBias, float outerBias) {
	//	float u = MathF.Pow((float)Program.Random.NextDouble(), innerBias);
	//	float v = MathF.Pow((float)Program.Random.NextDouble(), outerBias);
	//	return u / (u + v);
	//}
	private static float RandomExponentialDiskRadius(float gammaRadius) {
		float scaleRadius = gammaRadius * Parameters.GALAXY_OUTER_BIAS;
		float r;
		do
		{
			float u1 = MathF.Max((float)Program.Random.NextDouble(), float.Epsilon);
			float u2 = MathF.Max((float)Program.Random.NextDouble(), float.Epsilon);
			r = -scaleRadius * MathF.Log(u1 * u2);
		} while (r > gammaRadius);

		return r;
	}

	private static Vector<float> DirectionUnitVector2d(Vector<float> offset) {
		if (Parameters.DIM > 1) {
			float angle = MathF.Atan2(offset[1], offset[0]) + 0.5f*MathF.PI;
			return VectorFunctions.New([ MathF.Cos(angle), MathF.Sin(angle) ]);
		} else return Vector<float>.Zero;
	}
}