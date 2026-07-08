namespace ParticleSimulator.Rendering.Rasterization;

public struct PixelRank(int x, int y, float rank, float alpha = 0f)
{
	public bool IsNotNull = true;
	public int X = x;
	public int Y = y;
	public float Alpha = alpha;
	public float Rank = rank;

	public override readonly string ToString() =>
		string.Format("<{0}, {1}>[{2}]", this.X, this.Y, this.Rank);

	public override readonly bool Equals(object? obj) {
		return (obj is PixelRank rank)
		       && rank.IsNotNull == this.IsNotNull
		       && rank.X == this.X
		       && rank.Y == this.Y;
	}
	public override readonly int GetHashCode() {
		return base.GetHashCode();
	}

	public static bool operator ==(PixelRank left, PixelRank right)
	{
		return left.Equals(right);
	}

	public static bool operator !=(PixelRank left, PixelRank right)
	{
		return !(left == right);
	}
}