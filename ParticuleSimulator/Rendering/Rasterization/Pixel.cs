namespace ParticleSimulator.Rendering.Rasterization {
	public struct Pixel {
		public bool IsNotNull;
		public int X;
		public int Y;
		public float Rank;

		public Pixel(int x, int y, float rank) {
			this.X = x;
			this.Y = y;
			this.IsNotNull = true;
			this.Rank = rank;
		}
	}
}