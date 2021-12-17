using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Numerics;
using Generic.Extensions;
using Generic.Vectors;

namespace ParticleSimulator.Simulation {
	public static class Renderer {
		public static Autoscaler Scaling { get; private set; }

		public static readonly int RenderWidthOffset = 0;
		public static readonly int RenderHeightOffset = 0;
		public static readonly int RenderWidth;
		public static readonly int RenderHeight;
		public static readonly int MaxX;
		public static readonly int MaxY;

		static Renderer() {
			MaxX = Parameters.WINDOW_WIDTH;
			MaxY = Parameters.WINDOW_HEIGHT * 2;

			if (Parameters.DIM > 1) {
				float aspectRatio = Parameters.DOMAIN_SIZE[0] / Parameters.DOMAIN_SIZE[1];
				float consoleAspectRatio = (float)MaxX / (float)MaxY;
				if (aspectRatio > consoleAspectRatio) {//wide
					RenderWidth = MaxX;
					RenderHeight = (int)(MaxX * Parameters.DOMAIN_SIZE[1] / Parameters.DOMAIN_SIZE[0]);
					if (RenderHeight < 1) RenderHeight = 1;
					RenderHeightOffset = (MaxY - RenderHeight) / 4;
				} else {//tall
					RenderWidth = (int)(MaxY * Parameters.DOMAIN_SIZE[0] / Parameters.DOMAIN_SIZE[1]);
					RenderHeight = MaxY;
					RenderWidthOffset = (MaxX - RenderWidth) / 2;
				}
			} else {
				RenderWidth = MaxX;
				RenderHeight = 1;
				RenderHeightOffset = MaxY / 4;
			}

			Scaling = new(Parameters.COLOR_FIXED_BANDS
				?? (Parameters.COLOR_USE_FIXED_BANDS
					? Enumerable.Range(1, Parameters.COLOR_ARRAY.Length).Select(i => (float)i).ToArray()
					: null));
		}

		private static DateTime? _lastUpdateUtc = null;
		public static void FlushScreenBuffer(object[] parameters) {
			ConsoleExtensions.CharInfo[] buffer = (ConsoleExtensions.CharInfo[])parameters[0]
				?? new ConsoleExtensions.CharInfo[Parameters.WINDOW_WIDTH * Parameters.WINDOW_HEIGHT];

			int xOffset = Parameters.PERF_ENABLE ? 6 : 0,
				yOffset = Parameters.PERF_ENABLE ? 1 : 0;

			if (Program.StepEval_Draw.IsPunctual ?? false) _lastUpdateUtc = DateTime.UtcNow;
			TimeSpan timeSinceLastUpdate = DateTime.UtcNow.Subtract(_lastUpdateUtc ?? Program.Manager.StartTimeUtc);
			bool isSlow = timeSinceLastUpdate.TotalMilliseconds >= Parameters.PERF_WARN_MS;
			if (isSlow) {
				string message = "No update for " + (timeSinceLastUpdate.TotalSeconds.ToStringBetter(2) + "s").PadRight(6);
				for (int i = 0; i < message.Length; i++)
					buffer[i + xOffset + Parameters.WINDOW_WIDTH*yOffset] = new ConsoleExtensions.CharInfo(message[i], ConsoleColor.Red);
			}

			if (Parameters.PERF_ENABLE)
				PerfMon.DrawStatsOverlay(buffer, isSlow);

			if (!(buffer is null)) ConsoleExtensions.WriteConsoleOutput(buffer);
		}

		public static ConsoleColor ChooseGroupColor(IEnumerable<ParticleData> particles) {
			int dominantGroupID;
			if (Parameters.DIM > 2)
				dominantGroupID = particles.MinBy(p => GetDepthScalar(p.Position)).GroupID;
			else dominantGroupID = particles.GroupBy(p => p.GroupID).MaxBy(g => g.Count()).Key;
			return Parameters.COLOR_ARRAY[dominantGroupID % Parameters.COLOR_ARRAY.Length];
		}

		public static void DrawLegend(ConsoleExtensions.CharInfo[] buffer) {
			int numColors = Scaling.Values.Length;
			if (numColors > 0) {
				bool isDiscrete = Parameters.DIM < 3 && Parameters.SIM_TYPE == SimulationType.Boid;
				string header = Parameters.COLOR_METHOD.ToString();
				if (Parameters.COLOR_METHOD == ParticleColoringMethod.Group) {
					if (Parameters.PARTICLES_GROUP_COUNT > numColors)
						header += " (mod " + numColors + ")";
					if (Parameters.PARTICLES_GROUP_COUNT < Parameters.COLOR_ARRAY.Length)
						numColors = Parameters.PARTICLES_GROUP_COUNT;
				}

				int pixelIdx = Parameters.WINDOW_WIDTH * (Parameters.WINDOW_HEIGHT - numColors - 1);
				for (int i = 0; i < header.Length; i++)
					buffer[pixelIdx + i] = new ConsoleExtensions.CharInfo(header[i], ConsoleColor.White);
			
				string rowStringData;
				for (int cIdx = 0; cIdx < numColors; cIdx++) {
					pixelIdx += Parameters.WINDOW_WIDTH;

					buffer[pixelIdx] = new ConsoleExtensions.CharInfo(
						Parameters.CHAR_BOTH,
						Parameters.COLOR_ARRAY[cIdx]);

					if (Parameters.COLOR_METHOD == ParticleColoringMethod.Group)
						rowStringData = "=" + cIdx.ToString();
					else rowStringData =
							(isDiscrete && cIdx == 0 ? "=" : "≤")
							+ (isDiscrete
								? ((int)Scaling.Values[cIdx]).ToString()
								: Scaling.Values[cIdx].ToStringBetter(2, true, 5));

					for (int i = 0; i < rowStringData.Length; i++)
						buffer[pixelIdx + i + 1] = new ConsoleExtensions.CharInfo(rowStringData[i], ConsoleColor.White);
				}
			}
		}

		public static Vector<float> RotateCoordinates(Vector<float> coordinates) {//TODODO
			return coordinates;
		}

		public static Tuple<char, ParticleData[], float>[] Resample(object[] parameters) {
			ParticleData[] particleData = (ParticleData[])parameters[0];
			Tuple<char, ParticleData[], float>[] results = new Tuple<char, ParticleData[], float>[Parameters.WINDOW_WIDTH * Parameters.WINDOW_HEIGHT];

			char pixelChar;
			Queue<ParticleData> topStuff = new(), bottomStuff = new();
			HashSet<ParticleData> distinct = new();
			Queue<Tuple<ParticleData, int>>[] bins = DiscreteParticleBin(particleData);
			for (int i = 0; i < bins.Length; i++) {
				if (!(bins[i] is null)) {
					foreach (Tuple<ParticleData, int> t in bins[i]) {
						distinct.Add(t.Item1);
						if (t.Item2 % 2 == 0)
							topStuff.Enqueue(t.Item1);
						else bottomStuff.Enqueue(t.Item1);
					}

					if (topStuff.Count > 0 && bottomStuff.Count > 0)
						pixelChar = Parameters.CHAR_BOTH;
					else if (topStuff.Count > 0)
						pixelChar = Parameters.CHAR_TOP;
					else pixelChar = Parameters.CHAR_LOW;

					results[i] =
						new Tuple<char, ParticleData[], float>(
							pixelChar,
							distinct.ToArray(),
							Parameters.COLOR_METHOD == ParticleColoringMethod.Luminosity
								? Parameters.DIM > 2
									? new float[] {
										topStuff.Count > 0
											? topStuff.OrderBy(p => GetDepthScalar(p.Position)).ThenByDescending(p => p.Luminosity).Select(p => p.Luminosity).FirstOrDefault()
											: 0f,
										bottomStuff.Count > 0
											? bottomStuff.OrderBy(p => GetDepthScalar(p.Position)).ThenByDescending(p => p.Luminosity).Select(p => p.Luminosity).FirstOrDefault()
											: 0f
									}.Max()
									: distinct.Max(p => p.Luminosity)
								: Parameters.COLOR_METHOD == ParticleColoringMethod.Density
									? distinct.Sum(p => p.Density)
									: distinct.Count);

					topStuff.Clear();
					bottomStuff.Clear();
					distinct.Clear();
				}
			}

			return results;
		}
		private static Queue<Tuple<ParticleData, int>>[] DiscreteParticleBin(ParticleData[] particles) { 
			Queue<Tuple<ParticleData, int>>[] results = new Queue<Tuple<ParticleData, int>>[Parameters.WINDOW_WIDTH * Parameters.WINDOW_HEIGHT];
			int idx;
			foreach (Tuple<ParticleData, int, int> t in particles.SelectMany(p => SpreadSample(p))) {
				idx = t.Item2 + Parameters.WINDOW_WIDTH * (t.Item3 >> 1);
				results[idx] ??= new Queue<Tuple<ParticleData, int>>();
				results[idx].Enqueue(new Tuple<ParticleData, int>(t.Item1, t.Item3));
			}
			return results;
		}
		private static IEnumerable<Tuple<ParticleData, int, int>> SpreadSample(ParticleData p) {
			float
				pixelScalar = RenderWidth / Parameters.DOMAIN_SIZE[0],
				scaledX = RenderWidthOffset + p.Position[0] * pixelScalar,
				scaledY = RenderHeightOffset + (Parameters.DIM < 2 ? 0f : p.Position[1] * pixelScalar);

			if (p.Radius <= Parameters.WORLD_EPSILON)
				yield return new(p, (int)scaledX, (int)scaledY);
			else {
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

		public static ConsoleExtensions.CharInfo[] Rasterize(object[] parameters) {
			Tuple<char, ParticleData[], float>[] sampling = (Tuple<char, ParticleData[], float>[])parameters[0];

			ConsoleExtensions.CharInfo[] frameBuffer = new ConsoleExtensions.CharInfo[Parameters.WINDOW_WIDTH * Parameters.WINDOW_HEIGHT];
			if (!(sampling is null)) {
				for (int i = 0; i < sampling.Length; i++)
					frameBuffer[i] = sampling[i] is null ? default :
						new ConsoleExtensions.CharInfo(
							sampling[i].Item1,
							ChooseColor(sampling[i]));

				if (Parameters.LEGEND_ENABLE && (Parameters.COLOR_METHOD != ParticleColoringMethod.Depth || Parameters.DIM > 2))
					DrawLegend(frameBuffer);
			}

			return frameBuffer;
		}

		public static ConsoleColor ChooseColor(Tuple<char, ParticleData[], float> particleData) {
			if (Parameters.COLOR_METHOD == ParticleColoringMethod.Count
			|| Parameters.COLOR_METHOD == ParticleColoringMethod.Density
			|| Parameters.COLOR_METHOD == ParticleColoringMethod.Luminosity)
				return Parameters.COLOR_ARRAY[Scaling.Values.Drop(1).TakeWhile(ds => ds < particleData.Item3).Count()];
			else if (Parameters.COLOR_METHOD == ParticleColoringMethod.Group)
				return ChooseGroupColor(particleData.Item2);
			else if (Parameters.COLOR_METHOD == ParticleColoringMethod.Depth)
				if (Parameters.DIM > 2) {
					int numColors = Parameters.COLOR_ARRAY.Length;
					float depth = 1f - particleData.Item2.Min(p => GetDepthScalar(p.Position));
					int rank = Scaling.Values.Take(numColors - 1).TakeWhile(a => a < depth).Count();
					return Parameters.COLOR_ARRAY[rank];
				} else return Parameters.COLOR_ARRAY[^1];
			else throw new InvalidEnumArgumentException(nameof(Parameters.COLOR_METHOD));
		}

		public static float GetDepthScalar(Vector<float> v) {
			if (Parameters.DIM > 2)
				return 1f - (MathF.Sqrt(Enumerable.Range(2, Parameters.DIM - 2).Select(d => v[d] * v[d]).Sum()) / Parameters.DOMAIN_HIDDEN_DIMENSIONAL_HEIGHT);
			else return 1f;
		}
	}
}