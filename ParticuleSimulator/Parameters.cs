using System;
using Generic.Models;

namespace ParticleSimulator {
	public static class Parameters {
		public const bool ENABLE_ASYNCHRONOUS = true;
		public const bool LEGEND_ENABLE = true;
		public const bool DENSITY_AUTOSCALE_ENABLE = true;
		public const bool PERF_ENABLE = true;
		public const bool PERF_STATS_ENABLE = true;
		public const bool PERF_GRAPH_ENABLE = true;

		#region Particles
		public const int NUM_PARTICLES_PER_GROUP = 1000;
		public const int NUM_PARTICLE_GROUPS = 5;

		public const double SEPARATION = 3;
		#endregion Particles

		#region Sizes
		public const int WINDOW_WIDTH = 160;//160 x 80 max, practical min width of graph width if that is enabled
		public const int WINDOW_HEIGHT = 80;//using top and bottom halves of each character to get double the verticle resolution
		public const double WORLD_SCALE = 100d;
		public const double WORLD_ASPECT_RATIO = 1d;

		public const int GRAPH_WIDTH = -1;
		public const int GRAPH_HEIGHT = 7;//at least 2
		#endregion Sizes

		#region Timings
		//NOTE sentinel value is -1 for unlimited
		public const double TARGET_FPS = -1;
		public const double MAX_FPS = -1;

		public const int SIMULATION_SKIPS = 0;
		public const int TREE_REFRESH_SKIPS = 7;
		public const int QUADTREE_NODE_CAPACITY = 5;
		public const int AUTOSCALING_REFRESH_FRAMES = 30;

		public const bool SYNC_SIMULATION = false;
		public const bool SYNC_TREE_REFRESH = false;

		//how far ahead steps can work (when applicable)
		//set to zero to force everything to be synchronous
		public const int PRECALCULATION_LIMIT = 2;//no benefit to larger values than one
		#endregion Timings

		#region Boids
		public const int DESIRED_NEIGHBORS = 10;
		public const bool ENABLE_COHESION = true;
		public const bool ENABLE_ALIGNMENT = true;
		public const bool ENABLE_SEPARATION = true;
		
		public const double SPEED_DECAY = 0;//used as E^-val

		public const double SEPARATION_WEIGHT = 1d;
		public const double ALIGNMENT_WEIGHT = 1d;
		public const double COHESION_WEIGHT = 1d;

		public const double MAX_ACCELERATION = 0.1d;
		public const double MAX_SPEED = 0.1d;
		public const double MAX_STARTING_SPEED = 1d;

		public const double MAX_IMPULSE_COHESION = 1d;
		public const double MAX_IMPULSE_ALIGNMENT = 0.5d;
		public const double MAX_IMPULSE_SEPARATION = 0.5d;
		#endregion Boids

		#region Gravity
		public const double MIN_MASS = 1;
		public const double MAX_MASS = 1;

		public const int QUADTREE_NEIGHBORHOOD_FILTER = 5;
		#endregion Gravity

		#region Aux
		public const char CHAR_TOP = '\u2580';//▀
		public const char CHAR_BOTTOM = '\u2584';//▄
		public const char CHAR_BOTH = '\u2588';//█

		public const double TARGET_FPS_DEFAULT = 30d;
		public const int PERF_WARN_MS = 2000;
		public const int NUMBER_ACCURACY = 2;
		public const int NUMBER_SPACING = 5;

		public const int PERF_GRAPH_DEFAULT_WIDTH = 30;
		public const int PERF_GRAPH_FRAMES_PER_COLUMN = 30;
		public const double PERF_GRAPH_PERCENTILE_LOW_CUTOFF = 0d;
		public const double PERF_GRAPH_PERCENTILE_HIGH_CUTOFF = 2d;
		public const double PERF_SMA_ALPHA = 0.1d;
		public const double PERF_GRA_SMA_ALPHA = 0.2d;

		public const double AUTOSCALING_SMA_ALPHA = 0.4d;
		public const int AUTOSCALE_INTERVAL_MS = 250;

		public const bool ENABLE_DEBUG_LOGGING = false;
		public const bool ENABLE_DEBUG_PROCESS_LOGGING = false;
		public const bool ENABLE_VERBOSE_DEBUG_PROCESS_LOGGING = false;
		public const bool ENABLE_DEBUG_QUEUE_LOGGING = false;
		#endregion Aux

		public static readonly ConsoleColor[] DENSITY_COLORS = new ConsoleColor[] {
			ConsoleColor.DarkGray,
			ConsoleColor.Gray,
			ConsoleColor.White,
			ConsoleColor.Yellow,
			ConsoleColor.Red,
			ConsoleColor.DarkRed,
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
		
		public static readonly double[] DOMAIN = VectorFunctions.Multiply(new double[] { WORLD_ASPECT_RATIO, 1d }, WORLD_SCALE);
	}
}