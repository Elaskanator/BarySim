using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Generic.Extensions;
using Generic.Vectors;

namespace ParticleSimulator.ConsoleRendering {
	public class ConsoleRenderer {
		public Autoscaler Scaling { get; private set; }

		public readonly int NumPixels;
		public readonly int NumXSamples;
		public readonly int NumYSamples;
		public readonly int RenderWidth;
		public readonly int RenderHeight;
		public readonly int RenderWidthOffset = 0;
		public readonly int RenderHeightOffset = 0;

		private ConsoleExtensions.CharInfo[] _lastFrame;

		public ConsoleRenderer() {
			NumPixels = Parameters.WINDOW_WIDTH * Parameters.WINDOW_HEIGHT;
			NumXSamples = Parameters.WINDOW_WIDTH;
			NumYSamples = Parameters.WINDOW_HEIGHT * 2;

			if (Parameters.DIM > 1) {
				float aspectRatio = Parameters.DOMAIN_SIZE[0] / Parameters.DOMAIN_SIZE[1];
				float consoleAspectRatio = (float)NumXSamples / (float)NumYSamples;
				if (aspectRatio > consoleAspectRatio) {//wide
					RenderWidth = NumXSamples;
					RenderHeight = (int)(NumXSamples * Parameters.DOMAIN_SIZE[1] / Parameters.DOMAIN_SIZE[0]);
					if (RenderHeight < 1) RenderHeight = 1;
					RenderHeightOffset = (NumYSamples - RenderHeight) / 4;
				} else {//tall
					RenderWidth = (int)(NumYSamples * Parameters.DOMAIN_SIZE[0] / Parameters.DOMAIN_SIZE[1]);
					RenderHeight = NumYSamples;
					RenderWidthOffset = (NumXSamples - RenderWidth) / 2;
				}
			} else {
				RenderWidth = NumXSamples;
				RenderHeight = 1;
				RenderHeightOffset = NumYSamples / 4;
			}

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

		public static Vector<float> RotateCoordinates(Vector<float> coordinates) {//TODODO
			return coordinates;
		}

		public ConsoleExtensions.CharInfo[] Rasterize(object[] parameters) {//top down view (larger heights = closer)
			ConsoleExtensions.CharInfo[] results = new ConsoleExtensions.CharInfo[NumPixels];
			float[] scalingValues = new float[NumPixels * 2];

			Queue<Tuple<ParticleData, int, int, float>>[] bins = DiscreteParticleBin((IEnumerable<ParticleData>)parameters[0]);

			int topCount, bottomCount;
			float topRank, topHeightMax, bottomRank, bottomHeightMax;
			Tuple<ParticleData, int, int, float> binnedParticle;

			for (int i = 0; i < bins.Length; i++) {
				if (!(bins[i] is null)) {
					topCount = bottomCount = 0;
					topHeightMax = bottomHeightMax = float.NegativeInfinity;
					topRank = bottomRank = float.NegativeInfinity;

					while (bins[i].TryDequeue(out binnedParticle)) {
						if (binnedParticle.Item3 % 2 == 0) {
							topCount++;
							if (binnedParticle.Item4 > topHeightMax) {
								topRank = ComputeRank(binnedParticle.Item1, topCount);
								topHeightMax = binnedParticle.Item4;
							}
						} else {
							bottomCount++;
							if (binnedParticle.Item4 > bottomHeightMax) {
								bottomRank = ComputeRank(binnedParticle.Item1, bottomCount);
								bottomHeightMax = binnedParticle.Item4;
							}
						}
					}

					scalingValues[2*i] = topRank;
					scalingValues[2*i + 1] = bottomRank;
					results[i] = BuildChar(topRank, bottomRank);
				}
			}

			if (Parameters.LEGEND_ENABLE && (Parameters.COLOR_METHOD != ParticleColoringMethod.Depth || Parameters.DIM > 2))
				DrawLegend(results);
			Program.Resource_ScalingData.Overwrite(scalingValues);

			return results;
		}
		private float ComputeRank(ParticleData particle, int count) {
			return
				Parameters.COLOR_METHOD == ParticleColoringMethod.Count
				? count
				: Parameters.COLOR_METHOD == ParticleColoringMethod.Density
				? particle.Density
				: Parameters.COLOR_METHOD == ParticleColoringMethod.Luminosity
				? particle.Luminosity
				: 1f;
		}

		private ConsoleExtensions.CharInfo BuildChar(float topRank, float bottomRank) {
			switch (((topRank == float.NegativeInfinity ? 0 : 1) << 1) + (bottomRank == float.NegativeInfinity ? 0 : 1)) {
				case 1://bottom only
					return new ConsoleExtensions.CharInfo(Parameters.CHAR_TOP, ConsoleColor.Black, RankColor(bottomRank));
				case 2://top only
					return new ConsoleExtensions.CharInfo(Parameters.CHAR_LOW, ConsoleColor.Black, RankColor(topRank));
				case 3://both
					ConsoleColor bottomColor  = RankColor(bottomRank);
					ConsoleColor topColor = RankColor(topRank);
					if (topColor == bottomColor)
						return new ConsoleExtensions.CharInfo(0, 0, topColor);
					else if (topRank < bottomRank)
						return new ConsoleExtensions.CharInfo(Parameters.CHAR_TOP, topColor, bottomColor);
					else return new ConsoleExtensions.CharInfo(Parameters.CHAR_LOW, bottomColor, topColor);
				default:
					return default;
			}
		}

		private ConsoleColor RankColor(float rank) =>
			Parameters.COLOR_ARRAY[Scaling.Values.Drop(1).TakeWhile(ds => ds < rank).Count()];

		private Queue<Tuple<ParticleData, int, int, float>>[] DiscreteParticleBin(IEnumerable<ParticleData> particles) { 
			Queue<Tuple<ParticleData, int, int, float>>[] results = new Queue<Tuple<ParticleData, int, int, float>>[NumPixels];
			int idx;
			foreach (Tuple<ParticleData, int, int, float> t in particles.SelectMany(p => SpreadSample(p))) {
				idx = t.Item2 + Parameters.WINDOW_WIDTH * (t.Item3 >> 1);
				results[idx] ??= new Queue<Tuple<ParticleData, int, int, float>>();
				results[idx].Enqueue(new Tuple<ParticleData, int, int, float>(t.Item1, t.Item2, t.Item3, t.Item4));
			}
			return results;
		}

		private IEnumerable<Tuple<ParticleData, int, int, float>> SpreadSample(ParticleData particleData) {
			float pixelScalar = RenderWidth / Parameters.DOMAIN_SIZE[0],
				scaledRadius = pixelScalar * particleData.Radius;
			Vector<float> scaledPosition = particleData.Position * pixelScalar;

			if (particleData.Radius <= Parameters.WORLD_EPSILON) {
				yield return Parameters.DIM < 3
					? new(particleData, RenderWidthOffset + (int)scaledPosition[0], RenderHeightOffset + (int)scaledPosition[1], 0)
					: new(particleData, RenderWidthOffset + (int)scaledPosition[0], RenderHeightOffset + (int)scaledPosition[1], GetHeight(0f, 0f, scaledRadius, scaledPosition));
			} else {
				float
					radiusX = scaledRadius,
					minX = scaledPosition[0] - radiusX,
					maxX = scaledPosition[0] + radiusX,
					radiusY = Parameters.DIM < 2 ? 0f : scaledRadius,
					minY = scaledPosition[1] - radiusY,
					maxY = scaledPosition[1] + radiusY;
				maxX = maxX < Parameters.WINDOW_WIDTH ? maxX : Parameters.WINDOW_WIDTH - 1;
				maxY = maxY < 2*Parameters.WINDOW_HEIGHT ? maxY : 2*Parameters.WINDOW_HEIGHT - 1;

				int rangeX = 1 + (int)(maxX) - (int)(minX),
					rangeY = 1 + (int)(maxY) - (int)(minY);

				float testX, testY, dist;
				int roundedX, roundedY;
				for (int x2 = 0; x2 < rangeX; x2++) {
					roundedX = x2 + (int)minX;
					if (roundedX >= 0 && roundedX < Parameters.WINDOW_WIDTH) {
						for (int y2 = 0; y2 < rangeY; y2++) {
							roundedY = y2 + (int)minY;
							if (roundedY >= 0 && roundedY < Parameters.WINDOW_HEIGHT*2) {
								testX = roundedX == (int)scaledPosition[0]//particle in current bin
									? scaledPosition[0]//use exact value
									: roundedX + (roundedX < scaledPosition[0] ? 1 : 0);//nearer edge

								if (Parameters.DIM == 1) {
									dist = MathF.Abs(testX - scaledPosition[0]);
									if (dist < scaledRadius)
										yield return new(particleData, RenderWidthOffset + roundedX, RenderHeightOffset + roundedY, 0f);
								} else {
									testY = roundedY == (int)scaledPosition[1]//particle in current bin
										? scaledPosition[1]//use exact value
										: roundedY + (roundedY < scaledPosition[1] ? 1 : 0);//nearer edge

									dist = GetDistance(testX, testY, scaledRadius, scaledPosition);
									if (dist < scaledRadius)
										yield return new(particleData, RenderWidthOffset + roundedX, RenderHeightOffset + roundedY,
											Parameters.DIM < 3 ? 0f : GetHeight(testX, testY, scaledRadius, scaledPosition));
		}}}}}}}

		private float GetDistance(float x, float y, float r, Vector<float> v) {//TODODODODODO
			return MathF.Sqrt((v[0] - x)*(v[0]-x) + (v[1]-y)*(v[1]-y));
		}

		private float GetHeight(float x, float y, float r, Vector<float> v) {
			float dx = v[0] - x,
				dy = v[1] - y;
			Vector<float> heightComponents = Vector.ConditionalSelect(
				VectorFunctions.DimensionSignalsInverted[2],
				v,
				Vector<float>.Zero);
			float height = heightComponents.Magnitude(),
				range = MathF.Sqrt(r*r - dx*dx - dy*dy);

			return height + range >= Parameters.WINDOW_WIDTH
				? Parameters.WINDOW_WIDTH
				: height + range;
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