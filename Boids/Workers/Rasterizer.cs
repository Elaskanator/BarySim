using System;
using System.Linq;

using Generic.Extensions;

namespace Simulation {
	internal static class Rasterizer {
		public const char CHAR_TOP = '\u2580';//▀
		public const char CHAR_BOTTOM = '\u2584';//▄
		public const char CHAR_BOTH = '\u2588';//█
		
		private static readonly int _renderWidthOffset = 0;
		private static readonly int _renderHeightOffset = 0;
		private static readonly int _renderWidth;
		private static readonly int _renderHeight;
		private static readonly int _maxX;
		private static readonly int _maxY;

		static Rasterizer() {
			_maxX = Parameters.WINDOW_WIDTH;
			_maxY = Parameters.WINDOW_HEIGHT * 2;

			if (Parameters.DOMAIN.Length > 1) {
				double aspectRatio = Parameters.DOMAIN[0] / Parameters.DOMAIN[1];
				double consoleAspectRatio = (double)_maxX / (double)_maxY;
				if (aspectRatio > consoleAspectRatio) {//wide
					_renderWidth = _maxX;
					_renderHeight = (int)(_maxX * Parameters.DOMAIN[1] / Parameters.DOMAIN[0]);
					if (_renderHeight < 1) _renderHeight = 1;
					_renderHeightOffset = (_maxY - _renderHeight) / 4;
				} else {//tall
					_renderWidth = (int)(_maxY * Parameters.DOMAIN[0] / Parameters.DOMAIN[1]);
					_renderHeight = _maxY;
					_renderWidthOffset = (_maxX - _renderWidth) / 2;
				}
			} else {
				_renderWidth = _maxX;
				_renderHeight = 1;
				_renderHeightOffset = _maxY / 4;
			}
		}

		public static Tuple<char, double>[] Rasterize(object[] p) {
			if (Program.ENABLE_DEBUG_LOGGING) DebugExtensions.DebugWriteline("Rasterize - Start");
			DateTime startUtc = DateTime.UtcNow;
			double[][] coordinates = (double[][])p[0];

			Tuple<char, double>[] results = new Tuple<char, double>[Parameters.WINDOW_WIDTH * Parameters.WINDOW_HEIGHT];

			int topCount, bottomCount;
			double colorCount;
			char pixelChar;
			foreach (IGrouping<int, double[]> xGroup
			in coordinates.GroupBy(c => (int)(_renderWidth * c[0] / Parameters.DOMAIN[0]))) {
				foreach (IGrouping<int, double> yGroup
				in xGroup//subdivide each pixel into two vertical components
					.Select(c => Parameters.DOMAIN.Length < 2 ? 0 : _renderHeight * c[1] / Parameters.DOMAIN[1] / 2d)
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

					results[xGroup.Key + _renderWidthOffset + Parameters.WINDOW_WIDTH*(yGroup.Key + _renderHeightOffset)] =
						new Tuple<char, double>(
							pixelChar,
							colorCount);
				}
			}

			if (Parameters.DEBUG_ENABLE) PerfMon.AfterRasterize(startUtc);
			if (Program.ENABLE_DEBUG_LOGGING) DebugExtensions.DebugWriteline("Rasterize - End");
			return results;
		}

		public static ConsoleColor ChooseDensityColor(double count) {
			int rank = Program.Simulator.DensityScale.TakeWhile(b => (int)(2*count) > (int)(2 * b.Current)).Count();
			return Parameters.DENSITY_COLORS[rank];
		}
	}
}