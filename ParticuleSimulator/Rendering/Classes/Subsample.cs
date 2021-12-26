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
}