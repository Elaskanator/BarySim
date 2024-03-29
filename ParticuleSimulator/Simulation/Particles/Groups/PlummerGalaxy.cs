﻿using System;
using System.Numerics;
using Generic.Vectors;
using ParticleSimulator.Simulation.Baryon;

namespace ParticleSimulator.Simulation.Particles {
	public class PlummerGalaxy : AParticleGroup<MatterClump> {
		public PlummerGalaxy(Func<Vector<float>, Vector<float>, MatterClump> initializer, float r, float a) : base(initializer, r) {
			this.BulgeScalar = a;
			this.ParticleMass = Parameters.MASS_SCALAR / this.NumParticles;
		}

		public readonly float BulgeScalar;

		public readonly float ParticleMass;

		protected override void PrepareNewParticle(MatterClump p) {
			p.Mass = this.ParticleMass;
		}

		//Plummer distribution
		//see https://articles.adsabs.harvard.edu/pdf/1974A%26A....37..183A
		protected override void ParticleAddPositionVelocity(MatterClump particle) {
			float rand = (float)Program.Random.NextDouble();
			float radius = this.Radius * this.BulgeScalar / MathF.Sqrt(MathF.Pow(rand, -2f/3f) - 1f);
			Vector<float> uniformUnitVector = VectorFunctions.New(VectorFunctions.RandomUnitVector_Spherical(Parameters.DIM, Program.Random));
			Vector<float> particleOffset = radius * uniformUnitVector;
			particle._position += particleOffset;
			
			float bulgeRadius = this.Radius * this.BulgeScalar;
			float velocity = this.VelocitySampling(radius, bulgeRadius);
			uniformUnitVector = VectorFunctions.New(VectorFunctions.RandomUnitVector_Spherical(Parameters.DIM, Program.Random));
			particle.Velocity += velocity * uniformUnitVector;
		}

		private float VelocitySampling(float radius, float bulgeRadius) {
			float rand1 = (float)Program.Random.NextDouble(),
				rand2 = 0.1f*(float)Program.Random.NextDouble();
			while (rand2 > rand1*rand1*MathF.Pow(1f - rand1*rand1, 3.5f)) {
				rand1 = (float)Program.Random.NextDouble();
				rand2 = 0.1f*(float)Program.Random.NextDouble();
			}

			return rand1 * MathF.Sqrt(2)
				* MathF.Pow(bulgeRadius*bulgeRadius + radius*radius, -0.25f);
		}
	}
}