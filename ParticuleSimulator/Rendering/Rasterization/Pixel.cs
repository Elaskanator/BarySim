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

		public override string ToString() =>
			string.Format("<{0}, {1}>[{2}]", this.X, this.Y, this.Rank);

		public override bool Equals(object obj) {
			return (obj is Pixel)
				&& ((Pixel)obj).X == this.X
				&& ((Pixel)obj).Y == this.Y;
		}
		public override int GetHashCode() {
			return base.GetHashCode();
		}
	}
}