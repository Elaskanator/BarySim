namespace ParticleSimulator.Rendering.Rasterization;

public struct Pixel(int x, int y, float rank)
{
	public bool IsNotNull = true;
	public int X = x;
	public int Y = y;
	public float Rank = rank;

	public override readonly string ToString() =>
		string.Format("<{0}, {1}>[{2}]", this.X, this.Y, this.Rank);
	
	public override readonly bool Equals(object? obj) {
		return (obj is Pixel pixel)
		    && pixel.X == this.X
		    && pixel.Y == this.Y;
	}
	public static bool operator ==(Pixel left, Pixel right)
	{
		return left.Equals(right);
	}
	public static bool operator !=(Pixel left, Pixel right)
	{
		return !(left == right);
	}

	public override readonly int GetHashCode() {
		return base.GetHashCode();
	}
}