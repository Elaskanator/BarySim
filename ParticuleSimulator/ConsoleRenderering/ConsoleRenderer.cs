using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Generic.Extensions;

namespace ParticleSimulator.ConsoleRendering {
	public class ConsoleRenderer {
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

		public ConsoleExtensions.CharInfo[] Rasterize(object[] parameters) {//top down view (larger heights = closer)
			ConsoleExtensions.CharInfo[] results = new ConsoleExtensions.CharInfo[NumPixels];
			float[] scalingValues = new float[NumPixels * 2];

			Queue<Tuple<ParticleData, int, int, float>>[] bins = DiscreteParticleBin((IEnumerable<ParticleData>)parameters[0]);

			int topCount, bottomCount;
			float topRank, topHeightMax, bottomRank, bottomHeightMax;
			ParticleData topParticle, bottomParticle;
			Tuple<ParticleData, int, int, float> binnedParticle;

			for (int i = 0; i < bins.Length; i++) {
				if (!(bins[i] is null)) {
					topCount = bottomCount = 0;
					topHeightMax = bottomHeightMax = float.NegativeInfinity;
					topRank = bottomRank = float.NegativeInfinity;
					topParticle = bottomParticle = default;

					while (bins[i].TryDequeue(out binnedParticle)) {
						if (binnedParticle.Item3 % 2 == 0) {
							topCount++;
							if (binnedParticle.Item4 > topHeightMax) {
								topRank = ComputeRank(binnedParticle.Item1, topCount, topRank);
								topParticle = binnedParticle.Item1;
								topHeightMax = binnedParticle.Item4;
							}
						} else {
							bottomCount++;
							if (binnedParticle.Item4 > bottomHeightMax) {
								bottomRank = ComputeRank(binnedParticle.Item1, bottomCount, bottomRank);
								bottomParticle = binnedParticle.Item1;
								bottomHeightMax = binnedParticle.Item4;
							}
						}
					}

					scalingValues[2*i] = topRank;
					scalingValues[2*i + 1] = bottomRank;
					results[i] = BuildChar(topRank, topParticle, bottomRank, bottomParticle);
				}
			}

			if (Parameters.LEGEND_ENABLE && (Parameters.COLOR_METHOD != ParticleColoringMethod.Depth || Parameters.DIM > 2))
				DrawLegend(results);
			Program.Resource_ScalingData.Overwrite(scalingValues);

			return results;
		}
		private float ComputeRank(ParticleData particle, int count, float running) {
			return
				Parameters.COLOR_METHOD == ParticleColoringMethod.Count
					? count//sum
				: Parameters.COLOR_METHOD == ParticleColoringMethod.Density
					? particle.Density + running//sum
				: Parameters.COLOR_METHOD == ParticleColoringMethod.Luminosity
					? particle.Luminosity//max
				: 1f;//all equal
		}

		private ConsoleExtensions.CharInfo BuildChar(float topRank, ParticleData topParticle, float bottomRank, ParticleData bottomParticle) {
			switch (((topRank == float.NegativeInfinity ? 0 : 1) << 1) + (bottomRank == float.NegativeInfinity ? 0 : 1)) {
				case 1://bottom only
					return new ConsoleExtensions.CharInfo(Parameters.CHAR_TOP, ConsoleColor.Black, this.Scaling.RankColor(bottomRank, bottomParticle));
				case 2://top only
					return new ConsoleExtensions.CharInfo(Parameters.CHAR_LOW, ConsoleColor.Black, this.Scaling.RankColor(topRank, topParticle));
				case 3://both
					ConsoleColor bottomColor  = this.Scaling.RankColor(bottomRank, bottomParticle);
					ConsoleColor topColor = this.Scaling.RankColor(topRank, topParticle);
					if (topColor == bottomColor)
						return new ConsoleExtensions.CharInfo(0, 0, topColor);
					else if (topRank < bottomRank)
						return new ConsoleExtensions.CharInfo(Parameters.CHAR_TOP, topColor, bottomColor);
					else return new ConsoleExtensions.CharInfo(Parameters.CHAR_LOW, bottomColor, topColor);
				default:
					return default;
			}
		}

		private Queue<Tuple<ParticleData, int, int, float>>[] DiscreteParticleBin(IEnumerable<ParticleData> particles) { 
			Queue<Tuple<ParticleData, int, int, float>>[] results = new Queue<Tuple<ParticleData, int, int, float>>[NumPixels];
			int idx;
			foreach (ParticleData particle in particles) {
				foreach (Tuple<int, int, float> t in SpreadSample(particle)) {
					idx = t.Item1 + Parameters.WINDOW_WIDTH * (t.Item2 >> 1);//divide by two for vertical splitting of console characters
					results[idx] ??= new Queue<Tuple<ParticleData, int, int, float>>();
					results[idx].Enqueue(new Tuple<ParticleData, int, int, float>(particle, t.Item1, t.Item2, t.Item3));
				}
			}
			return results;
		}

		//assumption: particle is visible
		private IEnumerable<Tuple<int, int, float>> SpreadSample(ParticleData particle) {
			float scaledRadius = particle.Radius * PixelScalar;
			if (scaledRadius > 0) {
				Vector<float> scaledPosition = particle.Position * PixelScalar;

				int midX = (int)MathF.Round(scaledPosition[0]),
					midY = (int)MathF.Round(scaledPosition[1]);

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
					} else if (scaledPosition[2] > RenderHeight) {//only the bottom is visible
						float z = scaledPosition[2] - RenderHeight;
						visibleRadius = MathF.Sqrt(scaledRadius*scaledRadius - z*z);
					} else visibleRadius = scaledRadius;
				} else visibleRadius = scaledRadius;

				float pHeight;
				int xRange, yRange;
				int xBot, xTop, yBot, yTop;

				xRange = (int)MathF.Floor(visibleRadius);
				for (int x = 0; x <= xRange; x++) {
					if (Parameters.DIM > 1) {
						///Given the offset dx (nearest to the center),
						///  determine the range of dy
						///  that satisfies the circular radius r = |<dx, dy>|.
						yRange = (int)MathF.Floor(MathF.Sqrt(visibleRadius*visibleRadius - x*x));
						for (int y = 0; y <= yRange; y++) {
							///Determine the height dz of a point on the surface at <dx, dx>.
							///  For a point <dx, dy, ...> on the surface, its height dz (3D-only)
							///    satisfies the spherical radius r = |<dx, dy, dz, 0, ... , 0>|,
							///    which yields dz = sqrt(r^2 - dx^2 - dy^2).
							//Don't truncate if it extends beyond the visible region, for top-down rendering order
							pHeight = Parameters.DIM > 2
								? scaledPosition[2] + MathF.Sqrt(visibleRadius*visibleRadius - x*x - y*y)
								: particle.ID;//preserve render order

							if (x == 0 && y == 0) {
								if (0 <= midX && midX < RenderWidth && 0 <= midY && midY < RenderWidth)
									yield return new Tuple<int, int, float>(midX, midY, pHeight);
							} else if (x == 0) {
								if (0 <= midX && midX < RenderWidth) {
									yBot = midY - y;
									yTop = midY + y;
									if (0 <= yBot && yBot < RenderHeight) yield return new Tuple<int, int, float>(midX, yBot, pHeight);
									if (0 <= yTop && yTop < RenderHeight) yield return new Tuple<int, int, float>(midX, yTop, pHeight);
								}
							} else if (y == 0) {
								if (0 <= midY && midY < RenderWidth) {
									xBot = midX - x;
									xTop = midX + x;
									if (0 <= xBot && xBot < RenderWidth) yield return new Tuple<int, int, float>(xBot, midY, pHeight);
									if (0 <= xTop && xTop < RenderWidth) yield return new Tuple<int, int, float>(xTop, midY, pHeight);
								}
							} else {
								xBot = midX - x;
								xTop = midX + x;
								if (0 <= xBot && xBot < RenderWidth) {
									yBot = midY - y;
									yTop = midY + y;
									if (0 <= yBot && yBot < RenderHeight) yield return new Tuple<int, int, float>(xBot, yBot, pHeight);
									if (0 <= yTop && yTop < RenderHeight) yield return new Tuple<int, int, float>(xBot, yTop, pHeight);
								}
								if (0 <= xTop && xTop < RenderWidth) {
									yBot = midY - y;
									yTop = midY + y;
									if (0 <= yBot && yBot < RenderHeight) yield return new Tuple<int, int, float>(xTop, yBot, pHeight);
									if (0 <= yTop && yTop < RenderHeight) yield return new Tuple<int, int, float>(xTop, yTop, pHeight);
								}
							}
						}
					}
				}
			}
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