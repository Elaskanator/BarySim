namespace ParticleSimulator.Rendering {
	public struct Pixel {
		public bool IsNotNull;
		public int X;
		public int Y;
		public float Rank;

		public Pixel(Subsample sample, float rank) {
			this.IsNotNull = true;
			this.X = sample.X;
			this.Y = sample.Y;
			this.Rank = rank;
		}
	}
}