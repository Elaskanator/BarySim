using System;
using System.Collections.Generic;
using System.Numerics;
using ParticleSimulator.Engine;

namespace ParticleSimulator.Rendering.Rasterization {
	public class Rasterizer {
		public Rasterizer(int width, int height, Random rand, SynchronousBuffer<float?[]> rawRankings) {
			this.OutWidth = width;
			this.OutHeight = height;
			this.OutNumPixels = width * height;

			if (Parameters.SUPERSAMPLING > 1) {
				this.Supersampling = Parameters.SUPERSAMPLING;
				this.InternalWidth= width * Parameters.SUPERSAMPLING;
				this.InternalHeight = height * Parameters.SUPERSAMPLING;
				this.InternalNumPixels = width * height * Parameters.SUPERSAMPLING * Parameters.SUPERSAMPLING;
			} else {
				this.Supersampling = 1;
				this.InternalWidth= width;
				this.InternalHeight = height;
				this.InternalNumPixels = width * height;
			}

			this.InternalWidthF = this.InternalWidth;
			this.InternalHeightF = this.InternalHeight;

			this._rawRankingsResource = rawRankings;
			this.Camera = new Camera(Parameters.ZOOM_SCALE);

			if (Parameters.COLOR_METHOD == ParticleColoringMethod.Random)
				this._randOffset = (int)(100d * rand.NextDouble());
		
			float[] offset = new float[Vector<float>.Count];
			offset[0] = this.InternalWidth / 2f;
			offset[1] = this.InternalHeight / 2f;
			this.InternalOffset = new Vector<float>(offset);
			this.InternalScaleFactor = (this.InternalWidth > this.InternalHeight ? this.InternalHeight : this.InternalWidth) / 2f;
		}

		public readonly int Supersampling;
		public readonly int OutNumPixels;
		public readonly int OutWidth;
		public readonly int OutHeight;

		public readonly int InternalNumPixels;
		public readonly int InternalWidth;
		private readonly float InternalWidthF;
		public readonly int InternalHeight;
		private readonly float InternalHeightF;
		
		public readonly Camera Camera;
		public readonly Vector<float> InternalOffset;
		public readonly float InternalScaleFactor;
		
		private readonly SynchronousBuffer<float?[]> _rawRankingsResource;
		private readonly int _randOffset = 0;
		
		public Pixel[] Rasterize(bool wasPunctual, object[] parameters) {//top down view (smaller Z values = closer)
			Generic.Extensions.DebugExtensions.DebugWriteline_Interval(null);
			ParticleData[] particles = (ParticleData[])parameters[0];
			float[] scalings = (float[])parameters[1];

			this.Camera.IncrementRotation();

			int[] counts = new int[this.InternalNumPixels];
			float[] densities = new float[this.InternalNumPixels];
			Subsample[] nearest = new Subsample[this.InternalNumPixels];

			Queue<Subsample> resamplings = new();
			Subsample resampling;
			int idx;
			for (int i = 0; i < particles.Length; i++) {
				this.Resample(particles[i], resamplings);
				while (resamplings.TryDequeue(out resampling)) {
					idx = resampling.X + this.InternalWidth * resampling.Y;
						densities[idx] += resampling.H;
						if (counts[idx] == 0
						|| nearest[idx].Z > resampling.Z
						|| (nearest[idx].Z == resampling.Z && nearest[idx].Particle.ID > resampling.Particle.ID)) {
							nearest[idx] = resampling;
						}
						counts[idx]++;
				}
			}
			
			Pixel[] results = new Pixel[this.OutNumPixels];
			float?[] ranks = new float?[this.OutNumPixels];
			float densityScalar = 2f * Parameters.GRAVITY_RADIAL_DENSITY / this.InternalScaleFactor;
			bool any = false;
			if (this.Supersampling > 1) {
				bool any2;
				int idx2, count, totalCount;
				float totalDensity, rank, maxRank = float.NegativeInfinity;
				for (int x = 0; x < this.OutWidth; x++) {
					for (int y = 0; y < this.OutHeight; y++) {
						any2 = false;
						idx = x + y *this.OutWidth;
						count = totalCount = 0;
						totalDensity = 0f;
						maxRank = 0f;
						for (int sx = 0; sx < this.Supersampling; sx++) {
							for (int sy = 0; sy < this.Supersampling; sy++) {
								idx2 = (sx + x * this.Supersampling) + (sy + y * this.Supersampling) * this.InternalWidth;
								if (counts[idx2] > 0) {
									count++;
									any = any2 = true;
									totalCount += counts[idx2];
									totalDensity += densities[idx2];
									rank = this.GetRank(scalings, nearest[idx2], (float)totalCount / count, totalDensity * densityScalar / count);
									maxRank = rank > maxRank ? rank : maxRank;
								}
							}
						}
						if (any2) {
							ranks[idx] = maxRank;
							results[idx] = new(x * this.Supersampling, y * this.Supersampling, maxRank);
						}
					}
				}
			} else {
				for (int i = 0; i < this.OutNumPixels; i++) {
					if (counts[i] > 0) {
						any = true;
						ranks[i] = this.GetRank(scalings, nearest[i], counts[i], densities[i] * densityScalar);
						results[i] = new(nearest[i].X, nearest[i].Y, ranks[i].Value);
					}
				}
			}

			if (any) this._rawRankingsResource.Overwrite(ranks);

			return results;
		}

		private float GetRank(float[] scaling, Subsample resampling, float count, float density) {
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
				Vector<float> position = this.InternalOffset
					+ this.InternalScaleFactor * this.Camera.OffsetAndRotate(particle.Position);
				float radius = this.InternalScaleFactor * particle.Radius;

				if (0f <= position[0] + radius && position[0] - radius < this.InternalWidthF
				&& 0f <= position[1] + radius && position[1] - radius < this.InternalHeightF) {
					int xRounded = (int)position[0],
						yRounded = (int)position[1];
					result.Clear();

					if (0 <= xRounded && xRounded < this.InternalWidth
					 && 0 <= yRounded && yRounded < this.InternalHeight)
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
							if (position[2] < -this.InternalWidthF) {//only the bottom is visible
								dz = position[2] + this.InternalWidthF;
								visibleRadius = MathF.Sqrt(radius*radius - dz*dz);
							} else if (position[2] > this.InternalWidthF) {//only the top is visible
								dz = position[2] - this.InternalWidthF;
								visibleRadius = MathF.Sqrt(radius*radius - dz*dz);
							} else visibleRadius = radius;
						} else visibleRadius = radius;

						int xMin = (int)MathF.Floor(position[0] - visibleRadius + Parameters.PIXEL_OVERLAP_THRESHOLD),
							xMax = (int)MathF.Floor(position[0] + visibleRadius - Parameters.PIXEL_OVERLAP_THRESHOLD);
						xMin = xMin < 0 ? 0 : xMin;
						xMax = xMax >= this.InternalWidth ? this.InternalWidth - 1 : xMax;

						int yMin, yMax;
						float dx, dy, yRangeRemainder;//Allow height to exceed the visible maximum, to preserve top-down render order
						float squareRemainingRadius;

						//draw a vertical line at dx = 0
						if (0 <= xRounded && xRounded < this.InternalWidth) {
							yMin = (int)MathF.Floor(position[1] - visibleRadius + Parameters.PIXEL_OVERLAP_THRESHOLD);
							yMin = yMin < 0 ? 0 : yMin;
							yMax = (int)MathF.Floor(position[1] + visibleRadius - Parameters.PIXEL_OVERLAP_THRESHOLD);
							yMax = yMax >= this.InternalHeight ? this.InternalHeight - 1 : yMax;
							//bottom half
							for (int y = yMin; y < yRounded && y < this.InternalHeight; y++) {
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
						for (int x = xMin; x < xRounded && x < this.InternalWidth; x++) {
							dx = position[0] - (x + 1);//near side
							yRangeRemainder = MathF.Sqrt(visibleRadius*visibleRadius - dx*dx);

							yMin = (int)MathF.Floor(position[1] - yRangeRemainder + Parameters.PIXEL_OVERLAP_THRESHOLD);
							yMin = yMin < 0 ? 0 : yMin;
							yMax = (int)MathF.Floor(position[1] + yRangeRemainder - Parameters.PIXEL_OVERLAP_THRESHOLD);
							yMax = yMax >= this.InternalHeight ? this.InternalHeight - 1 : yMax;
					
							//y middle
							if (0 <= yRounded && yRounded < this.InternalHeight) {
								if (dx <= radius)
									result.Enqueue(new(particle, x, yRounded, position[2], MathF.Sqrt(radius*radius - dx*dx)));
							}
							//bottom half
							for (int y = yMin; y < yRounded && y < this.InternalHeight; y++) {
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
							yMax = yMax >= this.InternalHeight ? this.InternalHeight - 1 : yMax;
					
							//y middle
							if (0 <= yRounded && yRounded < this.InternalHeight) {
								if (dx <= radius)
									result.Enqueue(new(particle, x, yRounded, position[2], MathF.Sqrt(radius*radius - dx*dx)));
							}
							//bottom half
							for (int y = yMin; y < yRounded && y < this.InternalHeight; y++) {
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