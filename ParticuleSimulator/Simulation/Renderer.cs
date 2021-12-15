using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
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
				double aspectRatio = Parameters.DOMAIN_SIZE[0] / Parameters.DOMAIN_SIZE[1];
				double consoleAspectRatio = (double)MaxX / (double)MaxY;
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
					? Enumerable.Range(1, Parameters.COLOR_ARRAY.Length).Select(i => (double)i).ToArray()
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

		public static void DrawLegend(ConsoleExtensions.CharInfo[] buffer) {
			int numColors = Scaling.Values.Length;
			if (numColors > 0) {
				bool isDiscrete = Parameters.DIM < 3 && Parameters.SimType == SimulationType.Boid;
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
								: Scaling.Values[cIdx].ToStringBetter(2));

					for (int i = 0; i < rowStringData.Length; i++)
						buffer[pixelIdx + i + 1] = new ConsoleExtensions.CharInfo(rowStringData[i], ConsoleColor.White);
				}
			}
		}

		public static Tuple<char, IParticle[], double>[] Resample(object[] parameters) {
			IParticle[] particleData = (IParticle[])parameters[0];
			Tuple<char, IParticle[], double>[] results = new Tuple<char, IParticle[], double>[Parameters.WINDOW_WIDTH * Parameters.WINDOW_HEIGHT];

			char pixelChar;
			IParticle[] topStuff, bottomStuff, distinct;
			foreach (IGrouping<int, Tuple<int, int, IParticle>> bin in DiscreteParticleBin(particleData)) {
				topStuff = bin.Where(t => t.Item2 % 2 == 0).Select(t => t.Item3).ToArray();
				bottomStuff = bin.Where(t => t.Item2 % 2 == 1).Select(t => t.Item3).ToArray();
				distinct = bin.Select(b => b.Item3).Distinct().ToArray();

				if (topStuff.Length > 0 && bottomStuff.Length > 0)
					pixelChar = Parameters.CHAR_BOTH;
				else if (topStuff.Length > 0)
					pixelChar = Parameters.CHAR_TOP;
				else pixelChar = Parameters.CHAR_LOW;

				results[bin.Key] =
					new Tuple<char, IParticle[], double>(
						pixelChar,
						distinct,
						Parameters.COLOR_METHOD == ParticleColoringMethod.Luminosity
							? distinct.Max(p => p.Luminosity)
							: Parameters.COLOR_METHOD == ParticleColoringMethod.Density
								? distinct.Sum(p => p.Density)
							: distinct.Length);
			}
			return results;
		}
		private static IEnumerable<IGrouping<int, Tuple<int, int, IParticle>>> DiscreteParticleBin(IParticle[] particles) { 
			return particles
				.Where(p => p.Enabled && p.Visible)
				.SelectMany(p => SpreadSample(p).Where(p => p.Item1 >= 0 && p.Item1 < Parameters.WINDOW_WIDTH && p.Item2 >= 0 && p.Item2 < 2*Parameters.WINDOW_HEIGHT))
				.GroupBy(pd => pd.Item1 + (Parameters.WINDOW_WIDTH * (pd.Item2 / 2)));
		}
		private static IEnumerable<Tuple<int, int, IParticle>> SpreadSample(IParticle p) {
			double
				pixelScalar = RenderWidth / Parameters.DOMAIN_SIZE[0],
				scaledX = RenderWidthOffset + p.LiveCoordinates[0] * pixelScalar,
				scaledY = RenderHeightOffset + (Parameters.DIM < 2 ? 0d : p.LiveCoordinates[1] * pixelScalar);

			if (p.Radius <= Parameters.WORLD_EPSILON)
				yield return new((int)scaledX, (int)scaledY, p);
			else {
				double
					radiusX = p.Radius * pixelScalar,
					minX = scaledX - radiusX,
					maxX = scaledX + radiusX,
					radiusY = Parameters.DIM < 2 ? 0d : radiusX,
					minY = scaledY - radiusY,
					maxY = scaledY + radiusY;
				maxX = maxX < Parameters.WINDOW_WIDTH ? maxX : Parameters.WINDOW_WIDTH - 1;
				maxY = maxY < 2*Parameters.WINDOW_HEIGHT ? maxY : 2*Parameters.WINDOW_HEIGHT - 1;

				int
					rangeX = 1 + (int)(maxX) - (int)(minX),
					rangeY = 1 + (int)(maxY) - (int)(minY);

				double testX, testY, dist;
				int roundedX, roundedY;
				for (int x2 = 0; x2 < rangeX; x2++) {
					roundedX = x2 + (int)minX;
					if (roundedX >= 0d && roundedX < Parameters.WINDOW_WIDTH) {
						for (int y2 = 0; y2 < rangeY; y2++) {
							roundedY = y2 + (int)minY;
							if (roundedY >= 0d && roundedY < Parameters.WINDOW_HEIGHT*2) {
								testX = roundedX == (int)scaledX//particle in current bin
									? p.LiveCoordinates[0] * pixelScalar//use exact value
									: roundedX + (roundedX < scaledX ? 1 : 0);//nearer edge
								if (Parameters.DIM == 1) {
									dist = Math.Abs(testX - p.LiveCoordinates[0]);
									if (dist <= p.Radius) {
										yield return new(roundedX, roundedY, p);
									}
								} else {
									testY = roundedY == (int)scaledY//particle in current bin
										? p.LiveCoordinates[1] * pixelScalar//use exact value
										: roundedY + (roundedY < scaledY ? 1 : 0);//nearer edge
									dist = new double[] { testX, testY }.Distance(p.LiveCoordinates.Take(2).Select(c => c * pixelScalar).ToArray());
									if (p.Radius * pixelScalar >= dist)
										yield return new(roundedX, roundedY, p);
		}}}}}}}

		public static ConsoleExtensions.CharInfo[] Rasterize(object[] parameters) {
			Tuple<char, IParticle[], double>[] sampling = (Tuple<char, IParticle[], double>[])parameters[0];

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

		public static ConsoleColor ChooseColor(Tuple<char, IParticle[], double> particleData) {
			if (Parameters.COLOR_METHOD == ParticleColoringMethod.Count
			|| Parameters.COLOR_METHOD == ParticleColoringMethod.Density
			|| Parameters.COLOR_METHOD == ParticleColoringMethod.Luminosity)
				return Parameters.COLOR_ARRAY[Scaling.Values.Drop(1).TakeWhile(ds => ds < particleData.Item3).Count()];
			else if (Parameters.COLOR_METHOD == ParticleColoringMethod.Group)
				return Program.Simulator.ChooseGroupColor(particleData.Item2);
			else if (Parameters.COLOR_METHOD == ParticleColoringMethod.Depth)
				if (Parameters.DIM > 2) {
					int numColors = Parameters.COLOR_ARRAY.Length;
					double depth = 1d - particleData.Item2.Min(p => GetDepthScalar(p.LiveCoordinates));
					int rank = Scaling.Values.Take(numColors - 1).TakeWhile(a => a < depth).Count();
					return Parameters.COLOR_ARRAY[rank];
				} else return Parameters.COLOR_ARRAY[^1];
			else throw new InvalidEnumArgumentException(nameof(Parameters.COLOR_METHOD));
		}

		public static double GetDepthScalar(double[] v) {
			if (Parameters.DIM > 2)
				return 1d - (v.Skip(2).ToArray().Magnitude() / Parameters.DOMAIN_HIDDEN_DIMENSIONAL_HEIGHT);
			else return 1d;
		}
	}
}