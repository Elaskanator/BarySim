using System;
using Generic;

namespace Boids {
	public static class Parameters {
		//NOTE sentinel value is -1 (means default)
		public static readonly bool ENABLE_PARALLELISM = true;
		public static readonly bool LEGEND_ENABLE = true;
		public static readonly bool DENSITY_AUTOSCALE_ENABLE = true;
		public static readonly bool DEBUG_ENABLE = true;
		public static readonly bool PERF_STATS_ENABLE = true;
		public static readonly bool PERF_GRAPH_ENABLE = true;
		public static readonly bool SYNC_SIMULATION = true;
		public static readonly bool SYNC_FRAMERATE = false;

		public static readonly double TARGET_FPS = -1;
		public static readonly double MAX_FPS = 60d;

		#region Boids
		//TODO for the same number of total boids, more flocks makes it slower?
		public static readonly int NUM_BOIDS_PER_FLOCK = 10000;//significantly affects performance
		public static readonly int NUM_FLOCKS = 1;//total number of boids is flocks * boidsPerFlock
		public static readonly int RATED_BOIDS = 3000;//for a solid 30fps on my system - can do 100k boids at about 1fps
		public static readonly int SUBFRAME_MULTIPLE = 1;

		public static readonly int DESIRED_NEIGHBORS = 8;
		public static readonly bool ENABLE_COHESION = false;
		public static readonly bool ENABLE_ALIGNMENT = true;
		public static readonly bool ENABLE_SEPARATION = true;
		
		public static readonly int DEFAULT_SEPARATION = 4;
		public static readonly double DEFAULT_SPEED_DECAY = 0.1;
		#endregion Boids

		#region World
		public static readonly int WINDOW_WIDTH = 160;//160 x 80 max, practical min width of graph width if that is enabled
		public static readonly int WINDOW_HEIGHT = 80;
		public static readonly double WORLD_SCALE = 500d;
		public static readonly double WORLD_ASPECT_RATIO = 1d;

		public static readonly ConsoleColor[] DENSITY_COLORS = new ConsoleColor[] {
			ConsoleColor.DarkGray,
			ConsoleColor.Gray,
			ConsoleColor.White,
			ConsoleColor.Yellow,
			ConsoleColor.Red,
			ConsoleColor.DarkRed,
			///
			//ConsoleColor.DarkGray,
			//ConsoleColor.Gray,
			//ConsoleColor.White,
			//ConsoleColor.Blue,
			//ConsoleColor.DarkBlue,
			//ConsoleColor.Green,
			//ConsoleColor.Yellow,
			//ConsoleColor.Red,
			//ConsoleColor.DarkRed,
			//ConsoleColor.Magenta,
			//ConsoleColor.DarkMagenta,
		};
		
		public static readonly double[] DOMAIN = new double[] { WORLD_ASPECT_RATIO, 1d }.Multiply(WORLD_SCALE);
		#endregion World

		#region Performance Monitoring
		public const double TARGET_FPS_DEFAULT = 30d;
		public static readonly int PERF_WARN_MS = 2000;
		public static readonly int NUMBER_ACCURACY = 2;
		public static readonly int NUMBER_SPACING = 5;

		public static readonly int GRAPH_WIDTH = 92;
		public static readonly int GRAPH_HEIGHT = 12;
		public static readonly int PERF_GRAPH_FRAMES_PER_COLUMN = 10;
		public static readonly double PERF_SMA_ALPHA = 0.05d;
		#endregion Performance Monitoring

		#region Aux
		public static readonly int PRECALCULATION_LIMIT = 3;

		public static readonly int QUADTREE_REFRESH_FRAMES = 10;
		public static readonly bool QUADTREE_HYBRID_METHOD = true;
		public static readonly bool QUADTREE_INCREASED_ACCURACY = false;

		public static readonly double AUTOSCALING_REFRESH_FRAMES = 30;
		public static readonly double AUTOSCALING_SMA_ALPHA = 0.4d;

		public static readonly TimeSpan MinSleepDuration = TimeSpan.FromMilliseconds(15);
		#endregion Aux
	}
}