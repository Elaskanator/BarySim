using System;
using ParticleSimulator.Simulation.Particles;

namespace ParticleSimulator.Rendering.Rasterization {
	public struct Subsample {
		public ParticleData Particle;
		public int X;
		public int Y;
		public float Z;
		public float H;

		public Subsample(ParticleData particle, int x, int y, float z, float h2) {
			float h = h2 > 0f ? MathF.Sqrt(h2) : 0f;

			this.Particle = particle;

			this.X = x;
			this.Y = y;
			this.H = h; 
			this.Z = z - h;
		}
	}
}