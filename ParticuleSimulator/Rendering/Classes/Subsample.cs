namespace ParticleSimulator.Rendering {
	public struct Subsample {
		public ParticleData Particle;
		public int X;
		public int Y;
		public float Z;
		public float H;

		public Subsample(ParticleData particle, int x, int y, float z, float h) {
			this.Particle = particle;
			this.X = x;
			this.Y = y;
			this.Z = z - h;
			this.H = h;
		}
	}

	public struct Resampling {
		public bool IsNotNull;
		public int X;
		public int Y;
		public float Z;
		public float Rank;

		public Resampling(Subsample sample, float rank) {
			this.IsNotNull = true;
			this.X = sample.X;
			this.Y = sample.Y;
			this.Z = sample.Z;
			this.Rank = rank;
		}
	}
}