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

		this.InitialRadius = GetRadius(this.NumParticles);
		this.EdgeSpeed = GetStableEdgeSpeed(this.NumParticles);
	}
	private static float GetRadius(int particleCount) =>
		MathF.Sqrt(particleCount / (MathF.PI * Parameters.GALAXY_STAR_DENSITY));
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
	private static Vector<float> DirectionUnitVector2d(Vector<float> offset) {
		var angle = MathF.Atan2(offset[1], offset[0]) + 0.5f * MathF.PI;
		return VectorFunctions.New([ MathF.Cos(angle), MathF.Sin(angle) ]);
	}

	protected override void InitializeParticle(MatterClump particle) {
		var offset = RandomExponentialDiskRadius(this.InitialRadius);
		var proportionalOffset = offset / this.InitialRadius;

		var radialOffsetDirection = RandomRadialDirection(offset);
		var radialOffset = radialOffsetDirection * offset;

		Vector<float> verticalOffset;
		if (Parameters.DIM > 2)
			verticalOffset = RandomVerticalOffset(proportionalOffset, this.InitialRadius);
		else verticalOffset = Vector<float>.Zero;
		particle._position += radialOffset + verticalOffset;

		var rotationalSpeed =
			this.EdgeSpeed
			* MathF.Tanh(Parameters.GALAXY_SPIN_CURVE_STEEP * proportionalOffset)
			* (this.InternalDirection ? 1f : -1f)
			* Parameters.GALAXY_SPIN_MULTIPLIER;
		var rotationalVelocity =
			DirectionUnitVector2d(radialOffset)
			* rotationalSpeed;
		particle.Velocity += rotationalVelocity;

		var randomVelocity = Parameters.GALAXY_STAR_VEL_RAND == 0f
			? Vector<float>.Zero
			: Parameters.GALAXY_STAR_VEL_RAND
			  * VectorFunctions.RandomDirectionVector(Parameters.DIM, Program.Random);
		particle.Velocity += randomVelocity;
	}
	private static Vector<float> RandomRadialDirection(float offset) {
		if (Parameters.DIM <= 2)
			return VectorFunctions.RandomDirectionVector(Parameters.DIM, Program.Random);
		else return VectorFunctions.RandomDirectionVector(2, Program.Random);
	}
	private static float GetStableEdgeSpeed(int particleCount) {
		float radius = MathF.Sqrt(
			particleCount
			/ (MathF.PI * Parameters.GALAXY_STAR_DENSITY));

		return MathF.Sqrt(
			particleCount
			* Parameters.MASS_SCALAR
			* Parameters.GRAVITATIONAL_CONSTANT
			/ radius);
	}
	
	//public const float GALAXY_SPIN_FUDGE_FACTOR	= 6f;
	//protected override void FinalizeInitialParticles() {
	//	var particlesByRadius = this.InitialParticles
	//		.Select(p => {
	//			var radialOffset = p.Position - this.Position;
	//
	//			var radialOnly = new float[Vector<float>.Count];
	//			radialOnly[0] = radialOffset[0];
	//			radialOnly[1] = radialOffset[1];
	//
	//			var planarOffset = VectorFunctions.New(radialOnly);
	//
	//			return new {
	//				Particle = p,
	//				PlanarOffset = planarOffset,
	//				Radius = MathF.Sqrt(Vector.Dot(planarOffset, planarOffset))
	//			};
	//		})
	//		.OrderBy(x => x.Radius)
	//		.ToArray();
	//
	//	float enclosedMass = 0f;
	//	foreach (var item in particlesByRadius) {
	//		enclosedMass += item.Particle.Mass;
	//
	//		var rotationalSpeed =
	//			ApproximateStableRadialSpeed(enclosedMass, item.Radius, this.InitialRadius)
	//			* (this.InternalDirection ? 1f : -1f)
	//			* Parameters.GALAXY_SPIN_MULTIPLIER;
	//		var rotationalVelocity = DirectionUnitVector2d(item.PlanarOffset) * rotationalSpeed;
	//
	//		item.Particle.Velocity += rotationalVelocity;
	//	}
	//}
	//private static float ApproximateStableRadialSpeed(float enclosedMass, float radius, float galaxyRadius) {
	//	var softening = Parameters.GALAXY_THINNESS * galaxyRadius;
	//	var effectiveRadius = radius * MathF.Sqrt(softening * softening);
	//
	//	return
	//		MathF.Sqrt(
	//			Parameters.GRAVITATIONAL_CONSTANT
	//			* enclosedMass
	//			/ MathF.Max(effectiveRadius, Parameters.PRECISION_EPSILON))
	//		* Parameters.GALAXY_SPIN_FUDGE_FACTOR;
	//}

	private static Vector<float> RandomVerticalOffset(float radialOffsetProportional, float galaxyRadius) {
		var coshTerm = 1f / MathF.Cosh(Parameters.GALAXY_BULGE_STEEPNESS * radialOffsetProportional);

		var verticalScale =
			coshTerm
			+ Parameters.GALAXY_MIDRANGE_FLOOR
			* (1f - coshTerm)
			* MathF.Pow(
				1f - MathF.Pow(radialOffsetProportional, Parameters.GALAXY_VERTICLE_POWER),
				1f / Parameters.GALAXY_VERTICLE_POWER);

		var verticalOffset =
			verticalScale
			* galaxyRadius
			* Parameters.GALAXY_THINNESS
			* MathF.Pow((float)Program.Random.NextDouble(), Parameters.GALAXY_THINNESS_BIAS);

		float[] verticalV = [.. VectorFunctions
			.RandomDirectionVector(Parameters.DIM - 2, Program.Random)
			.ToArray()
			.Select(x => verticalOffset * x)];

		float[] offsetV = new float[Vector<float>.Count];

		for (int i = 0; i < Parameters.DIM - 2; i++)
			offsetV[i + 2] = verticalV[i];

		return VectorFunctions.New(offsetV);
	}
	
	private static float RandomExponentialDiskRadius(float gammaRadius) {
		var scaleRadius = gammaRadius * Parameters.GALAXY_OUTER_BIAS;

		while (true) {
			var u1 = MathF.Max((float)Program.Random.NextDouble(), float.Epsilon);
			var u2 = MathF.Max((float)Program.Random.NextDouble(), float.Epsilon);
			var r = -scaleRadius * MathF.Log(u1 * u2);

			if (r > gammaRadius)
				continue;

			var x = r / gammaRadius;
			var keepChance = 1f - SmoothStep(Parameters.GALAXY_EDGE_TAPER_START, 1f, x);

			if ((float)Program.Random.NextDouble() <= keepChance)
				return r;
		}
	}

	private static float SmoothStep(float edge0, float edge1, float x) {
		x = Math.Clamp((x - edge0) / (edge1 - edge0), 0f, 1f);
		return x * x * (3f - 2f * x);
	}





	
	//protected override void FinalizeInitialParticles() {
	//	var accelerations = new Vector<float>[this.NumParticles];
	//
	//	for (int i = 0; i < this.NumParticles - 1; i++)
	//		for (int j = i + 1; j < this.NumParticles; j++)
	//			AddMutualInitialGravityAcceleration(
	//				this.InitialParticles[i],
	//				this.InitialParticles[j],
	//				ref accelerations[i],
	//				ref accelerations[j]);
	//
	//	for (int i = 0; i < this.NumParticles; i++) {
	//		var particle = this.InitialParticles[i];
	//		var offset = particle.Position - this.Position;
	//
	//		float[] radialOnly = new float[Vector<float>.Count];
	//		radialOnly[0] = offset[0];
	//		radialOnly[1] = offset[1];
	//
	//		var planarOffset = VectorFunctions.New(radialOnly);
	//
	//		var v2 = -Vector.Dot(accelerations[i], planarOffset);
	//		var rotationalSpeed = v2 > 0f ? MathF.Sqrt(v2) : 0f;
	//
	//		particle.Velocity +=
	//			rotationalSpeed
	//			* (this.InternalDirection ? 1f : -1f)
	//			* DirectionUnitVector2d(planarOffset);
	//	}
	//}
	//private static void AddMutualInitialGravityAcceleration(MatterClump a, MatterClump b, ref Vector<float> accelerationA, ref Vector<float> accelerationB) {
	//	var toB = b._position - a._position;
	//	var distance2 = Vector.Dot(toB, toB);
	//
	//	if (distance2 <= Parameters.PRECISION_EPSILON)
	//		return;
	//
	//	var distance = MathF.Sqrt(distance2);
	//
	//	var influenceAB = a.ComputeInfluence(b, toB, distance, distance2);
	//	var influenceBA = b.ComputeInfluence(a, -toB, distance, distance2);
	//
	//	accelerationA += b.Mass * influenceAB;
	//	accelerationB += a.Mass * influenceBA;
	//}
}