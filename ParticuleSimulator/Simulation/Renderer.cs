using System;
using System.Collections.Generic;
using System.Linq;
using Generic.Extensions;
using Generic.Vectors;

namespace ParticleSimulator.Simulation {
	public static class Renderer {
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
		}
		
		public static void TitleUpdate(object[] parameters = null) {
			int visibleParticles = Program.Simulator.AllParticles.Count(p => p.IsVisible);

			Console.Title = string.Format("{0} Simulator - {1}{2} - {3}D",
				Parameters.SimType,
				Program.Simulator.AllParticles.Length.Pluralize("particle"),
				visibleParticles < Program.Simulator.AllParticles.Length
					? string.Format(" ({0} visible)", visibleParticles)
					: "",
				Parameters.DIM);
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
			int numColors = Program.Simulator.Scaling.Values.Length;
			if (numColors > 0) {
				bool isDiscrete = Parameters.DIM < 3 && Parameters.SimType == SimulationType.Boid;
				string header = Parameters.COLOR_SCHEME.ToString();
				if (Parameters.COLOR_SCHEME == ParticleColoringMethod.Group) {
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

					if (Parameters.COLOR_SCHEME == ParticleColoringMethod.Group)
						rowStringData = "=" + cIdx.ToString();
					else rowStringData =
							(isDiscrete && cIdx == 0 ? "=" : "≤")
							+ (isDiscrete
								? ((int)Program.Simulator.Scaling.Values[cIdx]).ToString()
								: Program.Simulator.Scaling.Values[cIdx].ToStringBetter(2));

					for (int i = 0; i < rowStringData.Length; i++)
						buffer[pixelIdx + i + 1] = new ConsoleExtensions.CharInfo(rowStringData[i], ConsoleColor.White);
				}
			}
		}

		public static ConsoleExtensions.CharInfo[] Rasterize(object[] parameters) {
			Tuple<char, AClassicalParticle[], double>[] sampling = (Tuple<char, AClassicalParticle[], double>[])parameters[0];

			ConsoleExtensions.CharInfo[] frameBuffer = new ConsoleExtensions.CharInfo[Parameters.WINDOW_WIDTH * Parameters.WINDOW_HEIGHT];
			if (!(sampling is null)) {
				for (int i = 0; i < sampling.Length; i++)
					frameBuffer[i] = sampling[i] is null ? default :
						new ConsoleExtensions.CharInfo(
							sampling[i].Item1,
							Program.Simulator.ChooseColor(sampling[i]));

				if (Parameters.LEGEND_ENABLE && (Parameters.COLOR_SCHEME != ParticleColoringMethod.Depth || Parameters.DIM > 2))
					DrawLegend(frameBuffer);
			}

			return frameBuffer;
		}

		public static Tuple<char, AClassicalParticle[], double>[] Resample(object[] parameters) {
			AClassicalParticle[] particleData = (AClassicalParticle[])parameters[0];
			Tuple<char, AClassicalParticle[], double>[] results = new Tuple<char, AClassicalParticle[], double>[Parameters.WINDOW_WIDTH * Parameters.WINDOW_HEIGHT];

			char pixelChar;
			AClassicalParticle[] topStuff, bottomStuff, distinct;
			foreach (IGrouping<int, Tuple<int, int, AClassicalParticle>> bin in DiscreteParticleBin(particleData)) {
				topStuff = bin.Where(t => t.Item2 % 2 == 0).Select(t => t.Item3).ToArray();
				bottomStuff = bin.Where(t => t.Item2 % 2 == 1).Select(t => t.Item3).ToArray();
				distinct = bin.Select(b => b.Item3).Distinct().ToArray();

				if (topStuff.Length > 0 && bottomStuff.Length > 0)
					pixelChar = Parameters.CHAR_BOTH;
				else if (topStuff.Length > 0)
					pixelChar = Parameters.CHAR_TOP;
				else pixelChar = Parameters.CHAR_LOW;

				results[bin.Key] =
					new Tuple<char, AClassicalParticle[], double>(
						pixelChar,
						distinct,
						distinct.Length);
			}
			return results;
		}
		private static IEnumerable<IGrouping<int, Tuple<int, int, AClassicalParticle>>> DiscreteParticleBin(AClassicalParticle[] particles) { 
			return particles
				.Where(p =>
					p.IsAlive
					&& p.LiveCoordinates[0] + p.Radius >= 0d && p.LiveCoordinates[0] - p.Radius < Parameters.DOMAIN_SIZE[0]
					&& (Parameters.DIM < 2 || p.LiveCoordinates[1] > -p.Radius && p.LiveCoordinates[1] < p.Radius + Parameters.DOMAIN_SIZE[1]))
				.SelectMany(p => SpreadSample(p).Where(p => p.Item1 >= 0 && p.Item1 < Parameters.WINDOW_WIDTH && p.Item2 >= 0 && p.Item2 < 2*Parameters.WINDOW_HEIGHT))
				.GroupBy(pd => pd.Item1 + (Parameters.WINDOW_WIDTH * (pd.Item2 / 2)));
		}
		private static IEnumerable<Tuple<int, int, AClassicalParticle>> SpreadSample(AClassicalParticle p) {
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
	}
}