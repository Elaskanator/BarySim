using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Numerics;
using Generic.Extensions;
using ParticleSimulator.Simulation;

namespace ParticleSimulator.ConsoleRendering {
	public static class Renderer {
		public static Autoscaler Scaling { get; private set; }

		public static readonly int NumPixels;
		public static readonly int NumXSamples;
		public static readonly int NumYSamples;
		public static readonly int RenderWidth;
		public static readonly int RenderHeight;
		public static readonly int RenderWidthOffset = 0;
		public static readonly int RenderHeightOffset = 0;

		private static DateTime _lastUpdateUtc;
		private static ConsoleExtensions.CharInfo[] _lastFrame;

		static Renderer() {
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

			_lastUpdateUtc = DateTime.UtcNow;
			_lastFrame = new ConsoleExtensions.CharInfo[NumPixels];

			Scaling = new(Parameters.COLOR_FIXED_BANDS
				?? (Parameters.COLOR_USE_FIXED_BANDS
					? Enumerable.Range(1, Parameters.COLOR_ARRAY.Length).Select(i => (float)i).ToArray()
					: null));
		}

		public static void FlushScreenBuffer(object[] parameters) {
			ConsoleExtensions.CharInfo[] buffer = (ConsoleExtensions.CharInfo[])parameters[0] ?? _lastFrame;
			_lastFrame = buffer;

			bool isSlow = Watchdog(buffer);

			if (Parameters.PERF_ENABLE)
				Program.Monitor.DrawStatsOverlay(buffer, isSlow);

			ConsoleExtensions.WriteConsoleOutput(buffer);
		}

		private static bool Watchdog(ConsoleExtensions.CharInfo[] buffer) {
			int xOffset = Parameters.PERF_ENABLE ? 6 : 0,
				yOffset = Parameters.PERF_ENABLE ? 1 : 0;

			if (Program.StepEval_Render.IsPunctual ?? false) _lastUpdateUtc = DateTime.UtcNow;
			TimeSpan timeSinceLastUpdate = DateTime.UtcNow.Subtract(_lastUpdateUtc);
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

		public static ConsoleExtensions.CharInfo[] Rasterize(object[] parameters) {
			ConsoleExtensions.CharInfo[] results = new ConsoleExtensions.CharInfo[NumPixels];
			float[] scalingValues = new float[NumPixels * 2];
			int topCount, bottomCount;
			float depth, rank,
				topRank, topMaxRank, topMinDepth,
				bottomRank, bottomMaxRank, bottomMinDepth;
			Queue<Tuple<ParticleData, int, int>>[] bins = DiscreteParticleBin((IEnumerable<ParticleData>)parameters[0]);
			Tuple<ParticleData, int, int> binnedParticle;
			for (int i = 0; i < bins.Length; i++) {
				if (!(bins[i] is null)) {
					depth = rank = topCount = bottomCount = 0;
					topMinDepth = bottomMinDepth = float.PositiveInfinity;
					topRank = topMaxRank = bottomRank = bottomMaxRank = float.NegativeInfinity;

					while (bins[i].TryDequeue(out binnedParticle)) {
						depth = binnedParticle.Item1.Depth;
						if (binnedParticle.Item3 % 2 == 0) {
							topCount++;
							rank = ComputeRank(binnedParticle.Item1, topCount, depth, topMaxRank);
							if (rank > topMaxRank)
								topMaxRank = rank;
							if (depth < topMinDepth) {
								topRank = rank;
								topMinDepth = binnedParticle.Item1.Depth;
							}
						} else {
							bottomCount++;
							rank = ComputeRank(binnedParticle.Item1, bottomCount, depth, bottomMaxRank);
							if (rank > bottomMaxRank)
								bottomMaxRank = rank;
							if (depth < bottomMinDepth) {
								bottomRank = rank;
								bottomMinDepth = binnedParticle.Item1.Depth;
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
		private static float ComputeRank(ParticleData particle, int count, float depth, float maxRank) {
			maxRank = maxRank == float.NegativeInfinity ? 0f : maxRank;
			return
				Parameters.COLOR_METHOD == ParticleColoringMethod.Count
				? count
				: Parameters.COLOR_METHOD == ParticleColoringMethod.Density
				? maxRank + particle.Density
				: Parameters.COLOR_METHOD == ParticleColoringMethod.Depth
				? -depth
				: Parameters.COLOR_METHOD == ParticleColoringMethod.Luminosity
				? particle.Luminosity
				: 1f;
		}

		private static ConsoleColor RankColor(float rank) =>
			Parameters.COLOR_ARRAY[Scaling.Values.Drop(1).TakeWhile(ds => ds < rank).Count()];
		private static ConsoleExtensions.CharInfo BuildChar(float topRank, float bottomRank) {
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

		//private static ConsoleColor ChooseColor(Tuple<char, ParticleData[], float> particleData) {
		//	if (Parameters.COLOR_METHOD == ParticleColoringMethod.Count
		//	|| Parameters.COLOR_METHOD == ParticleColoringMethod.Density
		//	|| Parameters.COLOR_METHOD == ParticleColoringMethod.Luminosity)
		//		return Parameters.COLOR_ARRAY[Scaling.Values.Drop(1).TakeWhile(ds => ds < particleData.Item3).Count()];
		//	else if (Parameters.COLOR_METHOD == ParticleColoringMethod.Group)
		//		return ChooseGroupColor(particleData.Item2);
		//	else if (Parameters.COLOR_METHOD == ParticleColoringMethod.Depth)
		//		if (Parameters.DIM > 2) {
		//			int numColors = Parameters.COLOR_ARRAY.Length;
		//			float depth = 1f - particleData.Item2.Min(p => GetDepthScalar(p.Position));
		//			int rank = Scaling.Values.Take(numColors - 1).TakeWhile(a => a < depth).Count();
		//			return Parameters.COLOR_ARRAY[rank];
		//		} else return Parameters.COLOR_ARRAY[^1];
		//	else throw new InvalidEnumArgumentException(nameof(Parameters.COLOR_METHOD));
		//}

		//public static ConsoleExtensions.CharInfo[] Rasterize(object[] parameters) {
		//	Tuple<char, ParticleData[], float>[] sampling = (Tuple<char, ParticleData[], float>[])parameters[0];

		//	ConsoleExtensions.CharInfo[] results = new ConsoleExtensions.CharInfo[NumPixels];
		//	if (!(sampling is null)) {
		//		for (int i = 0; i < sampling.Length; i++)
		//			results[i] = sampling[i] is null ? default :
		//				new ConsoleExtensions.CharInfo(
		//					sampling[i].Item1,
		//					ChooseColor(sampling[i]));

		//	}

		//	return results;
		//}
		private static Queue<Tuple<ParticleData, int, int>>[] DiscreteParticleBin(IEnumerable<ParticleData> particles) { 
			Queue<Tuple<ParticleData, int, int>>[] results = new Queue<Tuple<ParticleData, int, int>>[NumPixels];
			int idx;
			foreach (Tuple<ParticleData, int, int> t in particles.Where(p => p.IsVisible).SelectMany(p => SpreadSample(p))) {
				idx = t.Item2 + Parameters.WINDOW_WIDTH * (t.Item3 >> 1);
				results[idx] ??= new Queue<Tuple<ParticleData, int, int>>();
				results[idx].Enqueue(new Tuple<ParticleData, int, int>(t.Item1, t.Item2, t.Item3));
			}
			return results;
		}
		private static IEnumerable<Tuple<ParticleData, int, int>> SpreadSample(ParticleData p) {
			float
				pixelScalar = RenderWidth / Parameters.DOMAIN_SIZE[0],
				scaledX = RenderWidthOffset + p.Position[0] * pixelScalar,
				scaledY = RenderHeightOffset + (Parameters.DIM < 2 ? 0f : p.Position[1] * pixelScalar);

			if (p.Radius <= Parameters.WORLD_EPSILON) {
				yield return new(p, (int)scaledX, (int)scaledY);
			} else {
				float
					radiusX = p.Radius * pixelScalar,
					minX = scaledX - radiusX,
					maxX = scaledX + radiusX,
					radiusY = Parameters.DIM < 2 ? 0f : radiusX,
					minY = scaledY - radiusY,
					maxY = scaledY + radiusY;
				maxX = maxX < Parameters.WINDOW_WIDTH ? maxX : Parameters.WINDOW_WIDTH - 1;
				maxY = maxY < 2*Parameters.WINDOW_HEIGHT ? maxY : 2*Parameters.WINDOW_HEIGHT - 1;

				int
					rangeX = 1 + (int)(maxX) - (int)(minX),
					rangeY = 1 + (int)(maxY) - (int)(minY);

				float testX, testY, dist;
				int roundedX, roundedY;
				for (int x2 = 0; x2 < rangeX; x2++) {
					roundedX = x2 + (int)minX;
					if (roundedX >= 0f && roundedX < Parameters.WINDOW_WIDTH) {
						for (int y2 = 0; y2 < rangeY; y2++) {
							roundedY = y2 + (int)minY;
							if (roundedY >= 0f && roundedY < Parameters.WINDOW_HEIGHT*2) {
								testX = roundedX == (int)scaledX//particle in current bin
									? p.Position[0] * pixelScalar//use exact value
									: roundedX + (roundedX < scaledX ? 1 : 0);//nearer edge
								if (Parameters.DIM == 1) {
									dist = MathF.Abs(testX - p.Position[0]);
									if (dist <= p.Radius)
										yield return new(p, roundedX, roundedY);
								} else {
									testY = roundedY == (int)scaledY//particle in current bin
										? p.Position[1] * pixelScalar//use exact value
										: roundedY + (roundedY < scaledY ? 1 : 0);//nearer edge
									dist = MathF.Sqrt(
										new float[] { testX, testY }
										.Select((tx, d) => pixelScalar * p.Position[d] - tx)
										.Sum(dx => dx * dx));
									if (p.Radius * pixelScalar >= dist)
										yield return new(p, roundedX, roundedY);
		}}}}}}}

		public static void DrawLegend(ConsoleExtensions.CharInfo[] buffer) {
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