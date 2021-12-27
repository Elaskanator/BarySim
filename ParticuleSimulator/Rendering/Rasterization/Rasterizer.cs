using System;
using System.Collections.Generic;
using System.Numerics;
using ParticleSimulator.Engine;

namespace ParticleSimulator.Rendering {
	public class Rasterizer {
		public Rasterizer(int width, int height, Random rand, SynchronousBuffer<float?[]> rawRankings) {
			this.Width = width;
			this.Height = height;
			this.NumPixels = width * height;
			this.WidthF = this.Width;
			this.HeightF = this.Height;

			this._rawRankingsResource = rawRankings;
			this.Camera = new Camera(Parameters.ZOOM_SCALE);

			if (Parameters.COLOR_METHOD == ParticleColoringMethod.Random)
				this._randOffset = (int)(100d * rand.NextDouble());
		
			float[] offset = new float[Vector<float>.Count];
			offset[0] = Parameters.WINDOW_WIDTH / 2f;
			offset[1] = Parameters.WINDOW_HEIGHT;
			this.Offset = new Vector<float>(offset);
			this.ScaleFactor = Parameters.WINDOW_WIDTH > 2f*Parameters.WINDOW_HEIGHT
				? Parameters.WINDOW_HEIGHT
				: Parameters.WINDOW_WIDTH/2f;
		}

		public readonly int NumPixels;
		public readonly float WidthF;
		public readonly float HeightF;
		public readonly int Width;
		public readonly int Height;
		
		public readonly Camera Camera;
		public readonly Vector<float> Offset;
		public readonly float ScaleFactor;
		
		private readonly SynchronousBuffer<float?[]> _rawRankingsResource;
		private readonly int _randOffset = 0;

		private int _framesRendered = 0;
		
		public Pixel[] Rasterize(bool wasPunctual, object[] parameters) {//top down view (smaller Z values = closer)
			Queue<ParticleData> particles = (Queue<ParticleData>)parameters[0];
			float[] scalings = (float[])parameters[1];

			if (Parameters.WORLD_ROTATION) {
				float numSeconds = Parameters.WORLD_ROTATION_SPEED_ABS
					? this._framesRendered / Parameters.TARGET_FPS_DEFAULT
					: (float)DateTime.UtcNow.Subtract(Program.Engine.StartTimeUtc.Value).TotalSeconds;

				this.Camera.Set3DRotation(
					Parameters.WORLD_ROTATION_PITCH ? Parameters.WORLD_ROTATION_RADPERSEC * numSeconds : 0f,
					Parameters.WORLD_ROTATION_YAW ? Parameters.WORLD_ROTATION_RADPERSEC * numSeconds : 0f,
					Parameters.WORLD_ROTATION_ROLL ? Parameters.WORLD_ROTATION_RADPERSEC * numSeconds : 0f);
			}

			Pixel[] results = new Pixel[this.NumPixels];
			int[] counts = new int[this.NumPixels];
			float[] densities = new float[this.NumPixels];
			Subsample[] nearest = new Subsample[this.NumPixels];
			float?[] ranks = new float?[this.NumPixels];

			ParticleData particle;
			Queue<Subsample> resamplings = new();
			Subsample resampling;
			int idx;
			while (particles.TryDequeue(out particle)) {
				this.Resample(particle, resamplings);
				while (resamplings.TryDequeue(out resampling)) {
					idx = resampling.X + this.Width * resampling.Y;
						densities[idx] += resampling.H;
						if (counts[idx] == 0
						|| nearest[idx].Z > resampling.Z
						|| (nearest[idx].Z == resampling.Z && nearest[idx].Particle.ID > resampling.Particle.ID)) {
							counts[idx]++;
							nearest[idx] = resampling;
						}
				}
			}

			for (int i = 0; i < this.NumPixels; i++) {
				if (counts[i] > 0) {
					ranks[i] = this.GetRank(scalings, nearest[i], counts[i], densities[i]);
					results[i] = new(nearest[i], ranks[i].Value);
				}
			}

			this._rawRankingsResource.Overwrite(ranks);

			this._framesRendered++;
			return results;
		}

		private float GetRank(float[] scaling, Subsample resampling, int count, float density) {
			switch (Parameters.COLOR_METHOD) {
				case ParticleColoringMethod.Random:
					return (resampling.Particle.ID + this._randOffset) % scaling.Length;
				case ParticleColoringMethod.Group:
					return (resampling.Particle.GroupID + this._randOffset) % scaling.Length;
				case ParticleColoringMethod.Luminosity:
					return resampling.Particle.Luminosity;
				case ParticleColoringMethod.Depth:
					return resampling.Z;
				case ParticleColoringMethod.Count:
					return count;
				case ParticleColoringMethod.Density:
					return density;
				default:
					return 0f;
			}
		}

		//TODO rewrite to not use Sqrt
		private void Resample(ParticleData particle, Queue<Subsample> result) {
			if (particle.Radius > 0) {//let invisible particles remain so
				Vector<float> position = this.Offset
					+ this.ScaleFactor * this.Camera.OffsetAndRotate(particle.Position);
				float radius = this.ScaleFactor * particle.Radius;
				if (0f <= position[0] + radius && position[0] - radius < this.WidthF
				&& 0f <= position[1] + radius && position[1] - radius < this.HeightF) {
					int xRounded = (int)position[0],
						yRounded = (int)position[1];
					result.Clear();

					if (0 <= xRounded && xRounded < this.Width
					 && 0 <= yRounded && yRounded < this.Height)
						result.Enqueue(new(particle, xRounded, yRounded, position[2], radius));

					if (radius > Parameters.PIXEL_OVERLAP_THRESHOLD) {
						///If the particle's center is not visible,
						///  Determine the visible radius by truncation, as r_visible = |<dx, dy>|
						///    given the spherical radius r = |<dx, dy, dz_truncated, 0, ... , 0>|
						///	     r^2 = dx^2 + dy^2 + dZ_truncated^2
						///	     r_visible^2 = dx^2 + dy^2
						///      => r^2 - r_visible^2 = dz_truncated^2
						///      => r_visible = sqrt(r^2 - dz_truncated^2)
						float visibleRadius;
						if (Parameters.DIM > 2) {
							float dz;
							if (position[2] < -this.WidthF) {//only the bottom is visible
								dz = position[2] + this.WidthF;
								visibleRadius = MathF.Sqrt(radius*radius - dz*dz);
							} else if (position[2] > this.WidthF) {//only the top is visible
								dz = position[2] - this.WidthF;
								visibleRadius = MathF.Sqrt(radius*radius - dz*dz);
							} else visibleRadius = radius;
						} else visibleRadius = radius;

						int xMin = (int)MathF.Floor(position[0] - visibleRadius + Parameters.PIXEL_OVERLAP_THRESHOLD),
							xMax = (int)MathF.Floor(position[0] + visibleRadius - Parameters.PIXEL_OVERLAP_THRESHOLD);
						xMin = xMin < 0 ? 0 : xMin;
						xMax = xMax >= this.Width ? this.Width - 1 : xMax;

						int yMin, yMax;
						float dx, dy, yRangeRemainder;//Allow height to exceed the visible maximum, to preserve top-down render order
						float squareRemainingRadius;

						//draw a vertical line at dx = 0
						if (0 <= xRounded && xRounded < this.Width) {
							yMin = (int)MathF.Floor(position[1] - visibleRadius + Parameters.PIXEL_OVERLAP_THRESHOLD);
							yMin = yMin < 0 ? 0 : yMin;
							yMax = (int)MathF.Floor(position[1] + visibleRadius - Parameters.PIXEL_OVERLAP_THRESHOLD);
							yMax = yMax >= this.Height ? this.Height - 1 : yMax;
							//bottom half
							for (int y = yMin; y < yRounded && y < this.Height; y++) {
								dy = position[1] - (y + 1);//near side
								if (dy <= radius)
									result.Enqueue(new(particle, xRounded, y, position[2], MathF.Sqrt(radius*radius - dy*dy)));
							}
							//top half
							for (int y = yMax; y > yRounded && y >= 0; y--) {
								dy = y - position[1];
								if (dy <= radius)
									result.Enqueue(new(particle, xRounded, y, position[2], MathF.Sqrt(radius*radius - dy*dy)));
							}
						}
						
						///draw verticle lines inward toward center
						//left half
						for (int x = xMin; x < xRounded && x < this.Width; x++) {
							dx = position[0] - (x + 1);//near side
							yRangeRemainder = MathF.Sqrt(visibleRadius*visibleRadius - dx*dx);

							yMin = (int)MathF.Floor(position[1] - yRangeRemainder + Parameters.PIXEL_OVERLAP_THRESHOLD);
							yMin = yMin < 0 ? 0 : yMin;
							yMax = (int)MathF.Floor(position[1] + yRangeRemainder - Parameters.PIXEL_OVERLAP_THRESHOLD);
							yMax = yMax >= this.Height ? this.Height - 1 : yMax;
					
							//y middle
							if (0 <= yRounded && yRounded < this.Height) {
								if (dx <= radius)
									result.Enqueue(new(particle, x, yRounded, position[2], MathF.Sqrt(radius*radius - dx*dx)));
							}
							//bottom half
							for (int y = yMin; y < yRounded && y < this.Height; y++) {
								dy = position[1] - (y + 1);//near side
								squareRemainingRadius = radius*radius - dx*dx - dy*dy;
								if (squareRemainingRadius >= 0)
									result.Enqueue(new(particle, x, y, position[2], MathF.Sqrt(squareRemainingRadius)));
							}
							//top half
							for (int y = yMax; y > yRounded && y >= 0; y--) {
								dy = y - position[1];
								squareRemainingRadius = radius*radius - dx*dx - dy*dy;
								if (squareRemainingRadius >= 0)
									result.Enqueue(new(particle, x, y, position[2], MathF.Sqrt(squareRemainingRadius)));
							}
						}
						//right half
						for (int x = xMax; x > xRounded && x >= 0; x--) {
							dx = x - position[0];
							yRangeRemainder = MathF.Sqrt(visibleRadius*visibleRadius - dx*dx);

							yMin = (int)MathF.Floor(position[1] - yRangeRemainder + Parameters.PIXEL_OVERLAP_THRESHOLD);
							yMin = yMin < 0 ? 0 : yMin;
							yMax = (int)MathF.Floor(position[1] + yRangeRemainder - Parameters.PIXEL_OVERLAP_THRESHOLD);
							yMax = yMax >= this.Height ? this.Height - 1 : yMax;
					
							//y middle
							if (0 <= yRounded && yRounded < this.Height) {
								if (dx <= radius)
									result.Enqueue(new(particle, x, yRounded, position[2], MathF.Sqrt(radius*radius - dx*dx)));
							}
							//bottom half
							for (int y = yMin; y < yRounded && y < this.Height; y++) {
								dy = position[1] - (y + 1);//near side
								squareRemainingRadius = radius*radius - dx*dx - dy*dy;
								if (squareRemainingRadius >= 0)
									result.Enqueue(new(particle, x, y, position[2], MathF.Sqrt(squareRemainingRadius)));
							}
							//top half
							for (int y = yMax; y > yRounded && y >= 0; y--) {
								dy = y - position[1];
								squareRemainingRadius = radius*radius - dx*dx - dy*dy;
								if (squareRemainingRadius >= 0)
									result.Enqueue(new(particle, x, y, position[2], MathF.Sqrt(squareRemainingRadius)));
							}
						}
					}
				}
			}
		}
	}
}