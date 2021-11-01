using System;
using System.Linq;
using Generic;

namespace Boids {
	internal static class Rasterizer {
		public const char CHAR_TOP = '\u2580';//▀
		public const char CHAR_BOTTOM = '\u2584';//▄
		public const char CHAR_BOTH = '\u2588';//█

		public static readonly ConsoleColor[] DensityColors = new ConsoleColor[] {
			ConsoleColor.DarkGray,
			ConsoleColor.Gray,
			ConsoleColor.White,
			ConsoleColor.Blue,
			ConsoleColor.DarkBlue,
			ConsoleColor.Green,
			ConsoleColor.Yellow,
			ConsoleColor.Red,
			ConsoleColor.DarkRed,
			ConsoleColor.Magenta,
			ConsoleColor.DarkMagenta,
		};
		
		public static readonly int _renderWidthOffset = 0;
		public static readonly int _renderHeightOffset = 0;
		public static readonly int _renderWidth;
		public static readonly int _renderHeight;
		public static readonly int _maxX;
		public static readonly int _maxY;

		static Rasterizer() {
			_maxX = Parameters.WIDTH;
			_maxY = Parameters.HEIGHT * 2;

			if (Parameters.Domain.Length > 1) {
				double aspectRatio = Parameters.Domain[0] / Parameters.Domain[1];
				double consoleAspectRatio = (double)_maxX / (double)_maxY;
				if (aspectRatio > consoleAspectRatio) {//wide
					_renderWidth = _maxX;
					_renderHeight = (int)(_maxX * Parameters.Domain[1] / Parameters.Domain[0]);
					if (_renderHeight < 1) _renderHeight = 1;
					_renderHeightOffset = (int)(((_maxY) - _renderHeight) / 2d);
				} else {//tall
					_renderWidth = (int)(_maxY * Parameters.Domain[0] / Parameters.Domain[1]);
					_renderHeight = (int)(_maxY);
					_renderWidthOffset = (int)((_maxX - _renderWidth) / 2d);
				}
			} else {
				_renderWidth = _maxX;
				_renderHeight = 1;
				_renderHeightOffset = (int)( _maxY / 2d);
			}
		}

		public static Tuple<char, double>[] Rasterize(double[][] coordinates) {
			Tuple<char, double>[] results = new Tuple<char, double>[Parameters.WIDTH * Parameters.HEIGHT];

			int topCount, bottomCount;
			double colorCount;
			char pixelChar;
			foreach (var xGroup in coordinates.GroupBy(c => (int)(_renderWidth * c[0] / Parameters.Domain[0]))) {
				foreach (var yGroup in xGroup//subdivide each pixel into two vertical components
					.Select(c => Parameters.Domain.Length < 2 ? 0 : _renderHeight * c[1] / Parameters.Domain[1] / 2d)
					.GroupBy(y => (int)y))//preserve floating point value of normalized Y for subdivision
				{
					topCount = yGroup.Count(y => y % 1d < 0.5d);
					bottomCount = yGroup.Count() - topCount;

					if (topCount > 0 && bottomCount > 0) {
						pixelChar = CHAR_BOTH;
						colorCount = ((double)topCount + bottomCount) / 2d;
					} else if (topCount > 0) {
						pixelChar = CHAR_TOP;
						colorCount = topCount;
					} else {
						pixelChar = CHAR_BOTTOM;
						colorCount = bottomCount;
					}

					results[xGroup.Key + _renderWidthOffset + Parameters.WIDTH*(yGroup.Key + _renderHeightOffset)] =
						new Tuple<char, double>(
							pixelChar,
							colorCount);
				}
			}

			return results;
		}
		
		public static readonly SampleSMA[] PercentileBands = Enumerable.Range(1, DensityColors.Length - 1).Select(x => new SampleSMA(Parameters.AUTOSCALING_SMA_ALPHA, x)).ToArray();

		public static void DrawLegend(ConsoleExtensions.CharInfo[] buffer) {
			int pixelIdx;
			string strData;
			for (int cIdx = 0; cIdx < DensityColors.Length; cIdx++) {
				pixelIdx = Parameters.WIDTH * (Parameters.HEIGHT + cIdx - DensityColors.Length);
				buffer[pixelIdx] = new ConsoleExtensions.CharInfo(
					CHAR_BOTH,
					DensityColors[cIdx]);

				if (cIdx < PercentileBands.Length)
					strData = "=" + ((int)PercentileBands[cIdx].Current.Value).ToString("G4");
				else strData = ">";

				for (int sIdx = 0; sIdx < strData.Length; sIdx++) {
					buffer[pixelIdx + sIdx + 1] = new ConsoleExtensions.CharInfo(strData[sIdx], ConsoleColor.White);
				}
			}
		}

		//TODO use a Selection Algorithm to avoid the Order() call?
		public static void AutoscaleUpdate(Tuple<char, double>[] counts) {
			double[] orderedCounts = counts.Where(t => !(t is null)).Select(t => t.Item2).Order().ToArray();

			int totalBands = DensityColors.Length - 1;

			int lastBand = 0;
			int idx;
			int bandValue;
			for (int band = 1; band <= totalBands; band++) {
				idx = (int)(((double)orderedCounts.Length * band / (totalBands + 1d)) - 1d);

				bandValue = (int)orderedCounts[idx];

				if (bandValue > lastBand) {
					if (ExecutionManager.FramesRasterized <= 1) {
						PercentileBands[band - 1].Reset(bandValue);
					} else {
						PercentileBands[band - 1].Update(bandValue);
					}
					lastBand = (int)PercentileBands[band - 1].Current.Value;
				} else {
					PercentileBands[band - 1].Update(lastBand + 1);
					lastBand = lastBand + 1;
				}
			}
		}

		public static ConsoleColor ChooseDensityColor(double count) {
			int rank = PercentileBands.TakeWhile(b => count >= (int)(b.Current ?? 0)).Count();
			return DensityColors[rank];
		}
	}
}