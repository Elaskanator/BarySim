namespace ParticleSimulator.Rendering.Rasterization;

public struct PixelRank {
	public bool IsNotNull;
	public int X;
	public int Y;
	public float Alpha;
	public float Rank;

	public PixelRank(int x, int y, float rank, float alpha = 0f) {
		this.X = x;
		this.Y = y;
		this.Alpha = alpha;
		this.IsNotNull = true;
		this.Rank = rank;
	}

	public override string ToString() =>
		string.Format("<{0}, {1}>[{2}]", this.X, this.Y, this.Rank);

	public override bool Equals(object obj) {
		return (obj is PixelRank)
		       && ((PixelRank)obj).IsNotNull == this.IsNotNull
		       && ((PixelRank)obj).X == this.X
		       && ((PixelRank)obj).Y == this.Y;
	}
	public override int GetHashCode() {
		return base.GetHashCode();
	}
}