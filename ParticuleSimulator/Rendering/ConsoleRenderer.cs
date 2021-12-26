using System;
using System.Collections.Generic;
using System.Numerics;
using Generic.Extensions;

namespace ParticleSimulator.Rendering {
	public class ConsoleRenderer {
		public Autoscaler Scaling { get; private set; }
		public Camera Camera { get; private set; }

		public readonly int NumPixels;
		public readonly int RenderWidth;
		public readonly float RenderWidthF;
		public readonly int RenderHeight;
		public readonly float RenderHeightF;

		public readonly float ScaleFactor;
		public readonly Vector<float> RenderOffset;

		private int _framesRendered = 0;
		private ConsoleExtensions.CharInfo[] _lastFrame;

		public ConsoleRenderer() {
			this.RenderWidth = Parameters.WINDOW_WIDTH;
			this.RenderWidthF = (float)this.RenderWidth;
			this.RenderHeight = Parameters.WINDOW_HEIGHT * 2;
			this.RenderHeightF = (float)this.RenderHeight;
			this.NumPixels = RenderWidth * RenderHeight;

			this.ScaleFactor = Parameters.WINDOW_WIDTH > 2f*Parameters.WINDOW_HEIGHT
				? Parameters.WINDOW_HEIGHT
				: Parameters.WINDOW_WIDTH/2f;
		
			float[] offset = new float[Vector<float>.Count];
			offset[0] = Parameters.WINDOW_WIDTH / 2f;
			offset[1] = Parameters.WINDOW_HEIGHT;
			this.RenderOffset = new	Vector<float>(offset);

			this.Scaling = new();
			this.Camera = new Camera(Parameters.ZOOM_SCALE);

			this._lastFrame = new ConsoleExtensions.CharInfo[NumPixels];
		}

		public static ConsoleExtensions.CharInfo BuildChar(ConsoleColor bottomColor, ConsoleColor topColor) {
			if (topColor == bottomColor)
				return new ConsoleExtensions.CharInfo(0, 0, bottomColor);
			else return new ConsoleExtensions.CharInfo(Parameters.CHAR_LOW, bottomColor, topColor);
		}

		public void FlushScreenBuffer(object[] parameters) {
			ConsoleExtensions.CharInfo[] buffer = (ConsoleExtensions.CharInfo[])parameters[0] ?? _lastFrame;
			_lastFrame = buffer;

			bool isSlow = Watchdog(buffer);

			if (Parameters.PERF_ENABLE)
				Program.Monitor.DrawStatsOverlay(buffer, isSlow);

			ConsoleExtensions.WriteConsoleOutput(buffer);
		}

		private bool Watchdog(ConsoleExtensions.CharInfo[] buffer) {
			int xOffset = Parameters.PERF_ENABLE ? 6 : 0,
				yOffset = Parameters.PERF_ENABLE ? 1 : 0;

			TimeSpan timeSinceLastUpdate = DateTime.UtcNow.Subtract(Program.StepEval_Rasterize.LastComputeStartUtc ?? Program.Engine.StartTimeUtc.Value);
			bool isSlow = timeSinceLastUpdate.TotalMilliseconds >= Parameters.PERF_WARN_MS;
			if (isSlow) {
				string message = "No update for " + (timeSinceLastUpdate.TotalSeconds.ToStringBetter(2) + "s") + " ";
				for (int i = 0; i < message.Length; i++)
					buffer[i + xOffset + Parameters.WINDOW_WIDTH*yOffset] = new ConsoleExtensions.CharInfo(message[i], ConsoleColor.Red);
			}

			return isSlow;
		}

		public void DrawLegend(ConsoleExtensions.CharInfo[] buffer) {
			int numColors = this.Scaling.Values.Length;
			if (numColors > 0) {
				bool isDiscrete = false;//Parameters.DIM < 3 && Parameters.SIM_TYPE == SimulationType.Boid;
				string header = Parameters.COLOR_METHOD.ToString();

				int pixelIdx = Parameters.WINDOW_WIDTH * (Parameters.WINDOW_HEIGHT - numColors - 1);
				for (int i = 0; i < header.Length; i++)
					buffer[pixelIdx + i] = new ConsoleExtensions.CharInfo(header[i], ConsoleColor.White);
			
				string rowStringData;
				for (int cIdx = 0; cIdx < numColors; cIdx++) {
					pixelIdx += Parameters.WINDOW_WIDTH;

					buffer[pixelIdx] = new ConsoleExtensions.CharInfo(
						Parameters.CHAR_BOTH,
						Parameters.COLOR_ARRAY[cIdx]);

					rowStringData = 
						(isDiscrete && cIdx == 0 ? "=" : "≤")
						+ (isDiscrete
							? ((int)this.Scaling.Values[cIdx]).ToString()
							: this.Scaling.Values[cIdx].ToStringBetter(2, true, 5));

					for (int i = 0; i < rowStringData.Length; i++)
						buffer[pixelIdx + i + 1] = new ConsoleExtensions.CharInfo(rowStringData[i], ConsoleColor.White);
				}
			}
		}

		//TODO rewrite to not use Sqrt
		public ConsoleExtensions.CharInfo[] Rasterize(object[] parameters) {//top down view (smaller Z values = closer)
			if (Parameters.WORLD_ROTATION) {
				float numSeconds = Parameters.WORLD_ROTATION_SPEED_ABS
					? this._framesRendered / Parameters.TARGET_FPS_DEFAULT
					: (float)DateTime.UtcNow.Subtract(Program.Engine.StartTimeUtc.Value).TotalSeconds;

				this.Camera.Set3DRotation(
					Parameters.WORLD_ROTATION_PITCH ? Parameters.WORLD_ROTATION_SPEED * numSeconds : 0f,
					Parameters.WORLD_ROTATION_YAW ? Parameters.WORLD_ROTATION_SPEED * numSeconds : 0f,
					Parameters.WORLD_ROTATION_ROLL ? Parameters.WORLD_ROTATION_SPEED * numSeconds : 0f);
			}

			ConsoleExtensions.CharInfo[] results = new ConsoleExtensions.CharInfo[this.NumPixels];
			int[] topCounts = new int[this.NumPixels],
				bottomCounts = new int[this.NumPixels];
			float[] topDensities = new float[this.NumPixels],
				bottomDensities = new float[this.NumPixels];
			Resampling[] nearestTops = new Resampling[this.NumPixels],
				nearestBottoms = new Resampling[this.NumPixels];
			float?[] ranks = new float?[this.NumPixels*2];

			Queue<ParticleData> particles = (Queue<ParticleData>)parameters[0];
			ParticleData particle;
			Queue<Resampling> resamplings = new();
			Resampling resampling;
			int idx;
			while (particles.TryDequeue(out particle)) {
				this.Resample(particle, resamplings);
				while (resamplings.TryDequeue(out resampling)) {
					idx = resampling.X + Parameters.WINDOW_WIDTH * (resampling.Y >> 1);//divide by two for vertical splitting of console characters
					if ((resampling.Y & 1) == 0) {//odd
						topDensities[idx] += resampling.H;
						if (topCounts[idx] == 0
						|| nearestTops[idx].Z > resampling.Z
						|| (nearestTops[idx].Z == resampling.Z && nearestTops[idx].Particle.ID > resampling.Particle.ID)) {
							topCounts[idx]++;
							nearestTops[idx] = resampling;
						}
					} else {
						bottomDensities[idx] += resampling.H;
						if (bottomCounts[idx] == 0
						|| nearestBottoms[idx].Z > resampling.Z
						|| (nearestBottoms[idx].Z == resampling.Z && nearestBottoms[idx].Particle.ID > resampling.Particle.ID)) {
							bottomCounts[idx]++;
							nearestBottoms[idx] = resampling;
						}
					}
				}
			}

			ConsoleColor bottomColor, topColor;
			float? rank;
			for (int i = 0; i < this.NumPixels; i++) {
				if (bottomCounts[i] > 0 || topCounts[i] > 0) {
					rank = null;
					bottomColor = bottomCounts[i] > 0
						? this.Scaling.GetRankedColor(nearestBottoms[i].Particle, nearestBottoms[i].Z, bottomCounts[i], bottomDensities[i], ref rank)
						: ConsoleColor.Black;
					ranks[2*i] = rank;

					rank = null;
					topColor = topCounts[i] > 0
						? this.Scaling.GetRankedColor(nearestTops[i].Particle, nearestTops[i].Z, topCounts[i], topDensities[i], ref rank)
						: ConsoleColor.Black;
					results[i] = BuildChar(bottomColor,topColor);
					ranks[2*i + 1] = rank;
				}
			}

			if (Parameters.LEGEND_ENABLE
			&& Parameters.COLOR_METHOD != ParticleColoringMethod.Group
			&& Parameters.COLOR_METHOD != ParticleColoringMethod.Random
			&& (Parameters.COLOR_METHOD != ParticleColoringMethod.Depth || Parameters.DIM > 2))
				DrawLegend(results);

			Program.Resource_ScalingData.Overwrite(ranks);

			this._framesRendered++;
			return results;
		}

		//assumption: particle is visible
		private void Resample(ParticleData particle, Queue<Resampling> result) {
			if (particle.Radius > 0) {//let invisible particles remain so
				Vector<float> position = this.RenderOffset
					+ this.ScaleFactor * this.Camera.OffsetAndRotate(particle.Position);
				float radius = this.ScaleFactor * particle.Radius;
				if (0f <= position[0] + radius && position[0] - radius < this.RenderWidthF
				&& 0f <= position[1] + radius && position[1] - radius < this.RenderHeightF) {
					int xRounded = (int)position[0],
						yRounded = (int)position[1];
					result.Clear();

					if (0 <= xRounded && xRounded < this.RenderWidth
					 && 0 <= yRounded && yRounded < this.RenderHeight)
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
							if (position[2] < -this.RenderWidthF) {//only the bottom is visible
								dz = position[2] + this.RenderWidthF;
								visibleRadius = MathF.Sqrt(radius*radius - dz*dz);
							} else if (position[2] > this.RenderWidthF) {//only the top is visible
								dz = position[2] - this.RenderWidthF;
								visibleRadius = MathF.Sqrt(radius*radius - dz*dz);
							} else visibleRadius = radius;
						} else visibleRadius = radius;

						int xMin = (int)MathF.Floor(position[0] - visibleRadius + Parameters.PIXEL_OVERLAP_THRESHOLD),
							xMax = (int)MathF.Floor(position[0] + visibleRadius - Parameters.PIXEL_OVERLAP_THRESHOLD);
						xMin = xMin < 0 ? 0 : xMin;
						xMax = xMax >= this.RenderWidth ? this.RenderWidth - 1 : xMax;

						int yMin, yMax;
						float dx, dy, yRangeRemainder;//Allow height to exceed the visible maximum, to preserve top-down render order
						float squareRemainingRadius;

						//draw a vertical line at dx = 0
						if (0 <= xRounded && xRounded < this.RenderWidth) {
							yMin = (int)MathF.Floor(position[1] - visibleRadius + Parameters.PIXEL_OVERLAP_THRESHOLD);
							yMin = yMin < 0 ? 0 : yMin;
							yMax = (int)MathF.Floor(position[1] + visibleRadius - Parameters.PIXEL_OVERLAP_THRESHOLD);
							yMax = yMax >= this.RenderHeight ? this.RenderHeight - 1 : yMax;
							//bottom half
							for (int y = yMin; y < yRounded && y < this.RenderHeight; y++) {
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
						for (int x = xMin; x < xRounded && x < this.RenderWidth; x++) {
							dx = position[0] - (x + 1);//near side
							yRangeRemainder = MathF.Sqrt(visibleRadius*visibleRadius - dx*dx);

							yMin = (int)MathF.Floor(position[1] - yRangeRemainder + Parameters.PIXEL_OVERLAP_THRESHOLD);
							yMin = yMin < 0 ? 0 : yMin;
							yMax = (int)MathF.Floor(position[1] + yRangeRemainder - Parameters.PIXEL_OVERLAP_THRESHOLD);
							yMax = yMax >= this.RenderHeight ? this.RenderHeight - 1 : yMax;
					
							//y middle
							if (0 <= yRounded && yRounded < this.RenderHeight) {
								if (dx <= radius)
									result.Enqueue(new(particle, x, yRounded, position[2], MathF.Sqrt(radius*radius - dx*dx)));
							}
							//bottom half
							for (int y = yMin; y < yRounded && y < this.RenderHeight; y++) {
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
							yMax = yMax >= this.RenderHeight ? this.RenderHeight - 1 : yMax;
					
							//y middle
							if (0 <= yRounded && yRounded < this.RenderHeight) {
								if (dx <= radius)
									result.Enqueue(new(particle, x, yRounded, position[2], MathF.Sqrt(radius*radius - dx*dx)));
							}
							//bottom half
							for (int y = yMin; y < yRounded && y < this.RenderHeight; y++) {
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

		private struct Resampling {
			public ParticleData Particle;
			public int X;
			public int Y;
			public float Z;
			public float H;

			public Resampling(ParticleData particle, int x, int y, float z, float h) {
				this.Particle = particle;
				this.X = x;
				this.Y = y;
				this.Z = z - h;
				this.H = h;
			}
		}
	}
}