using System;
using System.Collections.Generic;
using System.Numerics;
using Generic.Extensions;

namespace ParticleSimulator.Rendering {
	public class ConsoleRenderer {
		public const float OVERLAP_THRESHOLD = 0.5f;

		public Autoscaler Scaling { get; private set; }

		public readonly int NumPixels;
		public readonly float PixelScalar;
		public readonly int RenderWidth;
		public readonly int RenderHeight;
		//public readonly int RenderWidthOffset = 0;
		//public readonly int RenderHeightOffset = 0;

		private ConsoleExtensions.CharInfo[] _lastFrame;

		public ConsoleRenderer() {
			//int numXSamples = Parameters.WINDOW_WIDTH;
			//int numYSamples = Parameters.WINDOW_HEIGHT * 2;

			//if (Parameters.DIM > 1) {
			//	float consoleAspectRatio = (float)numXSamples / (float)numYSamples;
			//	if (Parameters.WORLD_ASPECT_RATIO >= consoleAspectRatio) {//wide
			//		RenderWidth = numXSamples;
			//		RenderHeight = (int)(numXSamples / Parameters.WORLD_ASPECT_RATIO);
			//		if (RenderHeight < 1) RenderHeight = 1;
			//		RenderHeightOffset = (numYSamples - RenderHeight) / 4;
			//	} else {//tall
			//		RenderWidth = (int)(numYSamples * Parameters.WORLD_ASPECT_RATIO);
			//		RenderHeight = numYSamples;
			//		RenderWidthOffset = (numXSamples - RenderWidth) / 2;
			//	}
			//} else {
			//	RenderWidth = numXSamples;
			//	RenderHeight = 1;
			//	RenderHeightOffset = numYSamples / 4;
			//}
			
			RenderWidth = Parameters.WINDOW_WIDTH;
			RenderHeight = Parameters.WINDOW_HEIGHT * 2;
			NumPixels = RenderWidth * RenderHeight;
			PixelScalar = Parameters.WINDOW_WIDTH / Parameters.DOMAIN_SIZE[0];

			_lastFrame = new ConsoleExtensions.CharInfo[NumPixels];

			Scaling = new();
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

			TimeSpan timeSinceLastUpdate = DateTime.UtcNow.Subtract(Program.StepEval_Rasterize.LastComputeStartUtc ?? Program.Manager.StartTimeUtc.Value);
			bool isSlow = timeSinceLastUpdate.TotalMilliseconds >= Parameters.PERF_WARN_MS;
			if (isSlow) {
				string message = "No update for " + (timeSinceLastUpdate.TotalSeconds.ToStringBetter(2) + "s") + " ";
				for (int i = 0; i < message.Length; i++)
					buffer[i + xOffset + Parameters.WINDOW_WIDTH*yOffset] = new ConsoleExtensions.CharInfo(message[i], ConsoleColor.Red);
			}

			return isSlow;
		}

		public struct Resampling {
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

		public ConsoleExtensions.CharInfo[] Rasterize(object[] parameters) {//top down view (larger heights = closer)
			ConsoleExtensions.CharInfo[] results = new ConsoleExtensions.CharInfo[NumPixels];
			int[] topCounts = new int[NumPixels],
				bottomCounts = new int[NumPixels];
			float[] topDensities = new float[NumPixels],
				bottomDensities = new float[NumPixels],
				scalingValues = new float[NumPixels * 2];
			Resampling[] nearestTops = new Resampling[NumPixels],
				nearestBottoms = new Resampling[NumPixels];

			Queue<ParticleData> particles = (Queue<ParticleData>)parameters[0];
			ParticleData particle;
			Queue<Resampling> resamplings;
			Resampling resampling;
			int idx;
			while (particles.TryDequeue(out particle)) {
				if (particle.IsVisible) {
					resamplings = Resample(particle);
					while (resamplings.TryDequeue(out resampling)) {
						idx = resampling.X + Parameters.WINDOW_WIDTH * (resampling.Y >> 1);//divide by two for vertical splitting of console characters
						if (resampling.Y % 2 == 0) {
							if (topCounts[idx] == 0 || nearestTops[idx].Z > resampling.Z
							|| (nearestTops[idx].Z == resampling.Z && nearestTops[idx].Particle.ID > resampling.Particle.ID)) {
								topCounts[idx]++;
								topDensities[idx] += resampling.H;
								nearestTops[idx] = resampling;
							}
						} else {
							if (bottomCounts[idx] == 0 || nearestBottoms[idx].Z > resampling.Z
							|| (nearestBottoms[idx].Z == resampling.Z && nearestBottoms[idx].Particle.ID > resampling.Particle.ID)) {
								bottomCounts[idx]++;
								bottomDensities[idx] += resampling.H;
								nearestBottoms[idx] = resampling;
							}
						}
					}
				}
			}

			ConsoleColor bottomColor, topColor;
			for (int i = 0; i < NumPixels; i++) {
				if (bottomCounts[i] > 0 || topCounts[i] > 0) {
					bottomColor = bottomCounts[i] > 0
						? this.Scaling.RankColor(nearestBottoms[i].Particle, bottomCounts[i], bottomDensities[i])
						: ConsoleColor.Black;
					topColor = topCounts[i] > 0
						? this.Scaling.RankColor(nearestTops[i].Particle, topCounts[i], topDensities[i])
						: ConsoleColor.Black;
					results[i] = BuildChar(bottomColor,topColor);
				}
			}

			if (Parameters.LEGEND_ENABLE
			&& Parameters.COLOR_METHOD != ParticleColoringMethod.Group
			&& Parameters.COLOR_METHOD != ParticleColoringMethod.Random
			&& (Parameters.COLOR_METHOD != ParticleColoringMethod.Depth || Parameters.DIM > 2))
				DrawLegend(results);
			Program.Resource_ScalingData.Overwrite(scalingValues);

			return results;
		}

		public static ConsoleExtensions.CharInfo BuildChar(ConsoleColor bottomColor, ConsoleColor topColor) {
			if (topColor == bottomColor)
				return new ConsoleExtensions.CharInfo(0, 0, bottomColor);
			else return new ConsoleExtensions.CharInfo(Parameters.CHAR_TOP, topColor, bottomColor);
		}

		//assumption: particle is visible
		private Queue<Resampling> Resample(ParticleData particle) {
			Queue<Resampling> result = new();
			if (particle.Radius > 0) {//let invisible particles remain so
				Vector<float> scaledPosition = particle.Position * PixelScalar;
				float scaledRadius = particle.Radius * PixelScalar;
				int xRounded = (int)scaledPosition[0],
					yRounded = (int)scaledPosition[1];

				if (0 <= xRounded && xRounded < RenderWidth
				 && 0 <= yRounded && yRounded < RenderHeight)
					result.Enqueue(new(particle, xRounded, yRounded, scaledPosition[2], scaledRadius));

				if (scaledRadius > OVERLAP_THRESHOLD) {
					///If the particle's center is not visible,
					///  Determine the visible radius by truncation, as r_visible = |<dx, dy>|
					///    given the spherical radius r = |<dx, dy, dz_truncated, 0, ... , 0>|
					///	     r^2 = dx^2 + dy^2 + dZ_truncated^2
					///	     r_visible^2 = dx^2 + dy^2
					///      => r^2 - r_visible^2 = dz_truncated^2
					///      => r_visible = sqrt(r^2 - dz_truncated^2)
					float visibleRadius;
					if (Parameters.DIM > 2) {
						if (scaledPosition[2] < 0) {//only the top is visible
							//dz = scaledPosition[2] - 0f;
							visibleRadius = MathF.Sqrt(scaledRadius*scaledRadius - scaledPosition[2]*scaledPosition[2]);
						} else if (scaledPosition[2] > Parameters.DOMAIN_SIZE[2] * PixelScalar) {//only the bottom is visible
							float dz = scaledPosition[2] - Parameters.DOMAIN_SIZE[2] * PixelScalar;
							visibleRadius = MathF.Sqrt(scaledRadius*scaledRadius - dz*dz);
						} else visibleRadius = scaledRadius;
					} else visibleRadius = scaledRadius;

					int xMin = (int)MathF.Floor(scaledPosition[0] - visibleRadius + OVERLAP_THRESHOLD),
						xMax = (int)MathF.Floor(scaledPosition[0] + visibleRadius - OVERLAP_THRESHOLD);
					xMin = xMin < 0 ? 0 : xMin;
					xMax = xMax >= RenderWidth ? RenderWidth - 1 : xMax;

					int yMin, yMax;
					float dx, dy, yRangeRemainder;//Allow height to exceed the visible maximum, to preserve top-down render order
					float squareRemainingRadius;

					//draw a vertical line at dx = 0
					if (0 <= xRounded && xRounded < RenderWidth) {
						yMin = (int)MathF.Floor(scaledPosition[1] - visibleRadius + OVERLAP_THRESHOLD);
						yMin = yMin < 0 ? 0 : yMin;
						yMax = (int)MathF.Floor(scaledPosition[1] + visibleRadius - OVERLAP_THRESHOLD);
						yMax = yMax >= RenderHeight ? RenderHeight - 1 : yMax;
						//bottom half
						for (int y = yMin; y < yRounded && y < RenderHeight; y++) {
							dy = scaledPosition[1] - (y + 1);
							if (dy <= scaledRadius)
								result.Enqueue(new(particle, xRounded, y, scaledPosition[2], MathF.Sqrt(scaledRadius*scaledRadius - dy*dy)));
						}
						//top half
						for (int y = yMax; y > yRounded && y >= 0; y--) {
							dy = y - scaledPosition[1];
							if (dy <= scaledRadius)
								result.Enqueue(new(particle, xRounded, y, scaledPosition[2], MathF.Sqrt(scaledRadius*scaledRadius - dy*dy)));
						}
					}

					///draw verticle lines inward toward center
					//left half
					for (int x = xMin; x < xRounded && x < RenderWidth; x++) {
						dx = scaledPosition[0] - (x + 1);
						yRangeRemainder = MathF.Sqrt(visibleRadius*visibleRadius - dx*dx);

						yMin = (int)MathF.Floor(scaledPosition[1] - yRangeRemainder + OVERLAP_THRESHOLD);
						yMin = yMin < 0 ? 0 : yMin;
						yMax = (int)MathF.Floor(scaledPosition[1] + yRangeRemainder - OVERLAP_THRESHOLD);
						yMax = yMax >= RenderHeight ? RenderHeight - 1 : yMax;
					
						//y middle
						if (0 <= yRounded && yRounded < RenderHeight) {
							if (dx <= scaledRadius)
								result.Enqueue(new(particle, x, yRounded, scaledPosition[2], MathF.Sqrt(scaledRadius*scaledRadius - dx*dx)));
						}
						//bottom half
						for (int y = yMin; y < yRounded && y < RenderHeight; y++) {
							dy = scaledPosition[1] - (y + 1);
							squareRemainingRadius = scaledRadius*scaledRadius - dx*dx - dy*dy;
							if (squareRemainingRadius >= 0)
								result.Enqueue(new(particle, x, y, scaledPosition[2], MathF.Sqrt(squareRemainingRadius)));
						}
						//top half
						for (int y = yMax; y > yRounded && y >= 0; y--) {
							dy = y - scaledPosition[1];
							squareRemainingRadius = scaledRadius*scaledRadius - dx*dx - dy*dy;
							if (squareRemainingRadius >= 0)
								result.Enqueue(new(particle, x, y, scaledPosition[2], MathF.Sqrt(squareRemainingRadius)));
						}
					}
					//right half
					for (int x = xMax; x > xRounded && x >= 0; x--) {
						dx = x - scaledPosition[0];
						yRangeRemainder = MathF.Sqrt(visibleRadius*visibleRadius - dx*dx);

						yMin = (int)MathF.Floor(scaledPosition[1] - yRangeRemainder + OVERLAP_THRESHOLD);
						yMin = yMin < 0 ? 0 : yMin;
						yMax = (int)MathF.Floor(scaledPosition[1] + yRangeRemainder - OVERLAP_THRESHOLD);
						yMax = yMax >= RenderHeight ? RenderHeight - 1 : yMax;
					
						//y middle
						if (0 <= yRounded && yRounded < RenderHeight) {
							if (dx <= scaledRadius)
								result.Enqueue(new(particle, x, yRounded, scaledPosition[2], MathF.Sqrt(scaledRadius*scaledRadius - dx*dx)));
						}
						//bottom half
						for (int y = yMin; y < yRounded && y < RenderHeight; y++) {
							dy = scaledPosition[1] - (y + 1);
							squareRemainingRadius = scaledRadius*scaledRadius - dx*dx - dy*dy;
							if (squareRemainingRadius >= 0)
								result.Enqueue(new(particle, x, y, scaledPosition[2], MathF.Sqrt(squareRemainingRadius)));
						}
						//top half
						for (int y = yMax; y > yRounded && y >= 0; y--) {
							dy = y - scaledPosition[1];
							squareRemainingRadius = scaledRadius*scaledRadius - dx*dx - dy*dy;
							if (squareRemainingRadius >= 0)
								result.Enqueue(new(particle, x, y, scaledPosition[2], MathF.Sqrt(squareRemainingRadius)));
						}
					}
				}
			}
			return result;
		}

		public void DrawLegend(ConsoleExtensions.CharInfo[] buffer) {
			int numColors = Scaling.Values.Length;
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
							? ((int)Scaling.Values[cIdx]).ToString()
							: Scaling.Values[cIdx].ToStringBetter(2, true, 5));

					for (int i = 0; i < rowStringData.Length; i++)
						buffer[pixelIdx + i + 1] = new ConsoleExtensions.CharInfo(rowStringData[i], ConsoleColor.White);
				}
			}
		}
	}
}