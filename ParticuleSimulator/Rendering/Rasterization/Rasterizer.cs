using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ParticleSimulator.Engine.Threading;
using ParticleSimulator.Simulation.Particles;

namespace ParticleSimulator.Rendering.Rasterization {
	public class Rasterizer {
		public Rasterizer(Camera camera, int width, int height, int depth, Random rand, SynchronousBuffer<float?[]> rawRankings) {
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
			this.Camera = camera;

			if (Parameters.COLOR_METHOD == ParticleColoringMethod.Random)
				this._randOffset = (int)(100d * rand.NextDouble());
		
			float[] offset = new float[Vector<float>.Count];
			offset[0] = this.InternalWidth / 2f;
			offset[1] = this.InternalHeight / 2f;
			this.InternalOffset = new Vector<float>(offset);//make all values range from [0, size] instead of [-size/2, +size/2]
			this.InternalScaleFactor = this.InternalWidthF <= this.InternalHeight ? this.InternalWidthF : this.InternalHeight;
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
		
		private readonly Camera Camera;
		private readonly Vector<float> InternalOffset;
		private readonly float InternalScaleFactor;
		
		private readonly SynchronousBuffer<float?[]> _rawRankingsResource;
		private readonly int _randOffset = 0;
		
		public Pixel[] Rasterize(EvalResult prepResults, object[] parameters) {//top down view (smaller Z values = closer)
			ParticleData[] particles = (ParticleData[])parameters[0];
			if (particles is null) {
				return Array.Empty<Pixel>();
			} else {
				float[] scalings = (float[])parameters[1];
				Pixel[] results = new Pixel[this.OutNumPixels];
				float?[] ranks = new float?[this.OutNumPixels];

				this.Camera.Increment(Program.Engine.Simulator.Center.Position);

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

						if (counts[idx] == 0 || nearest[idx].Z > resampling.Z)
							nearest[idx] = resampling;

						counts[idx]++;
					}
				}

				bool any = false;
				if (this.Supersampling > 1) {
					bool any2;
					int idx2, count, totalCount, y2, x2, y3;
					float totalDensity;
					Queue<Subsample> bin = new();
					for (int x = 0; x < this.OutWidth; x++) {
						x2 = x * this.Supersampling;
						for (int y = 0; y < this.OutHeight; y++) {
							bin.Clear();
							y3 = y * this.Supersampling;
							any2 = false;
							idx = x + y *this.OutWidth;
							count = totalCount = 0;
							totalDensity = 0f;
							for (int sy = 0; sy < this.Supersampling; sy++) {
								y2 = (sy + y3) * this.InternalWidth;
								for (int sx = 0; sx < this.Supersampling; sx++) {
									idx2 = (sx + x2) + y2;
									if (counts[idx2] > 0) {
										count++;
										bin.Enqueue(nearest[idx2]);
										any = any2 = true;
										totalCount += counts[idx2];
										totalDensity += densities[idx2];
									}
								}
							}
							if (any2) {
								if (Parameters.COLOR_METHOD == ParticleColoringMethod.Density)
									ranks[idx] = bin.Sum(sample => this.GetRank(scalings, sample, (float)totalCount / count, totalDensity / count));
								else ranks[idx] = bin.Max(sample => this.GetRank(scalings, sample, (float)totalCount / count, totalDensity / count));
								results[idx] = new(x, y, ranks[idx].Value);
							}
						}
					}
				} else for (int i = 0; i < this.OutNumPixels; i++)
					if (counts[i] > 0) {
						any = true;
						ranks[i] = this.GetRank(scalings, nearest[i], counts[i], densities[i]);
						results[i] = new(nearest[i].X, nearest[i].Y, ranks[i].Value);
					}

				if (any) this._rawRankingsResource.Overwrite(ranks);

				return results;
			}
		}

		private float GetRank(float[] scaling, Subsample resampling, float count, float halfHeight) {
			switch (Parameters.COLOR_METHOD) {
				case ParticleColoringMethod.Random:
					return (resampling.Particle.Id + this._randOffset) % scaling.Length;
				case ParticleColoringMethod.Group:
					return (resampling.Particle.GroupId + this._randOffset) % scaling.Length;
				case ParticleColoringMethod.Luminosity:
					return resampling.Particle.Luminosity;
				case ParticleColoringMethod.Depth:
					return resampling.Z;
				case ParticleColoringMethod.Overlap:
					return count;
				case ParticleColoringMethod.Density:
					return halfHeight * 2f* resampling.Particle.Density;
				default:
					return 0f;
			}
		}

		//TODO rewrite to not use Sqrt
		private void Resample(ParticleData particle, Queue<Subsample> result) {
			Vector<float> position = this.InternalOffset + (this.InternalScaleFactor * this.Camera.Rotate(particle.Position));
			float radius = this.InternalScaleFactor * this.Camera.Zoom * particle.Radius;

			if (0f <= position[0] + radius && position[0] - radius < this.InternalWidthF
			&& 0f <= position[1] + radius && position[1] - radius < this.InternalHeightF) {//visible
				result.Clear();
				int xRounded = (int)position[0],
					yRounded = (int)position[1];
				float radiusSquared = radius * radius;

				if (0 <= xRounded && xRounded < this.InternalWidth
				&& 0 <= yRounded && yRounded < this.InternalHeight)
					result.Enqueue(new(particle, xRounded, yRounded, position[2], radiusSquared));
				
				//draw verticle lines inward toward center
				if (radius > Parameters.PIXEL_ROUNDOFF) {
					int xMin = (int)MathF.Floor(position[0] - radius + Parameters.PIXEL_ROUNDOFF),
						xMax = (int)MathF.Floor(position[0] + radius - Parameters.PIXEL_ROUNDOFF);
					xMin = xMin < 0 ? 0 : xMin;
					xMax = xMax >= this.InternalWidth ? this.InternalWidth - 1 : xMax;

					int yMin, yMax;
					float dx, dy, yRangeRemainder;
					float squareRemainingRadiusX, squareRemainingRadiusY;

					//centerline
					if (0 <= xRounded && xRounded < this.InternalWidth) {
						yMin = (int)MathF.Floor(position[1] - radius + Parameters.PIXEL_ROUNDOFF);
						yMax = (int)MathF.Floor(position[1] + radius - Parameters.PIXEL_ROUNDOFF);

						//bottom half
						for (int y = yMin < 0 ? 0 : yMin; y < yRounded && y < this.InternalHeight; y++) {
							dy = position[1] - (y + 1);//near side
							squareRemainingRadiusY = radiusSquared - dy*dy;

							result.Enqueue(new(particle, xRounded, y, position[2], squareRemainingRadiusY));
						}
						//top half
						for (int y = yMax >= this.InternalHeight ? this.InternalHeight - 1 : yMax; y > yRounded && y >= 0; y--) {
							dy = y - position[1];
							squareRemainingRadiusY = radiusSquared - dy*dy;

							result.Enqueue(new(particle, xRounded, y, position[2], squareRemainingRadiusY));
						}
					}
					//left half
					for (int x = xMin; x < xRounded && x < this.InternalWidth; x++) {
						dx = position[0] - (x + 1);//near side
						squareRemainingRadiusX = radiusSquared - dx*dx;
						yRangeRemainder = MathF.Sqrt(squareRemainingRadiusX);
						yMin = (int)MathF.Floor(position[1] - yRangeRemainder + Parameters.PIXEL_ROUNDOFF);
						yMax = (int)MathF.Floor(position[1] + yRangeRemainder - Parameters.PIXEL_ROUNDOFF);
								
						//y middle
						if (0 <= yRounded && yRounded < this.InternalHeight)
							result.Enqueue(new(particle, x, yRounded, position[2], squareRemainingRadiusX));
						//bottom half
						for (int y = yMin < 0 ? 0 : yMin; y < yRounded && y < this.InternalHeight; y++) {
							dy = position[1] - (y + 1);//near side
							squareRemainingRadiusY = squareRemainingRadiusX - dy*dy;

							result.Enqueue(new(particle, x, y, position[2], squareRemainingRadiusY));
						}
						//top half
						for (int y = yMax >= this.InternalHeight ? this.InternalHeight - 1 : yMax; y > yRounded && y >= 0; y--) {
							dy = y - position[1];
							squareRemainingRadiusY = squareRemainingRadiusX - dy*dy;

							result.Enqueue(new(particle, x, y, position[2], squareRemainingRadiusY));
						}
					}
					//right half
					for (int x = xMax; x > xRounded && x >= 0; x--) {
						dx = x - position[0];
						squareRemainingRadiusX = radiusSquared - dx*dx;
						yRangeRemainder = MathF.Sqrt(squareRemainingRadiusX);
						yMin = (int)MathF.Floor(position[1] - yRangeRemainder + Parameters.PIXEL_ROUNDOFF);
						yMax = (int)MathF.Floor(position[1] + yRangeRemainder - Parameters.PIXEL_ROUNDOFF);
								
						//y middle
						if (0 <= yRounded && yRounded < this.InternalHeight)
							result.Enqueue(new(particle, x, yRounded, position[2], squareRemainingRadiusX));
						//bottom half
						for (int y = yMin < 0 ? 0 : yMin; y < yRounded && y < this.InternalHeight; y++) {
							dy = position[1] - (y + 1);//near side
							squareRemainingRadiusY = squareRemainingRadiusX - dy*dy;

							result.Enqueue(new(particle, x, y, position[2], squareRemainingRadiusY));
						}
						//top half
						for (int y = yMax >= this.InternalHeight ? this.InternalHeight - 1 : yMax; y > yRounded && y >= 0; y--) {
							dy = y - position[1];
							squareRemainingRadiusY = squareRemainingRadiusX - dy*dy;

							result.Enqueue(new(particle, x, y, position[2], squareRemainingRadiusY));
						}
					}
				}
			}
		}
	}
}