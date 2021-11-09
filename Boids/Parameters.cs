using System;
using Generic.Models;

namespace ParticleSimulator {
	public static class Parameters {
		//NOTE sentinel value is -1 (means default)
		public const bool LEGEND_ENABLE = true;
		public const bool DENSITY_AUTOSCALE_ENABLE = true;
		public const bool DEBUG_ENABLE = true;
		public const bool PERF_STATS_ENABLE = true;
		public const bool PERF_GRAPH_ENABLE = false;

		public const bool SYNC_SIMULATION = true;
		public const double TARGET_FPS = -1;
		public const double MAX_FPS = 60;

		public const int PRECALCULATION_LIMIT = 1;//no benefit to larger values than one

		#region Particles
		public const int NUM_PARTICLES_PER_GROUP = 3000;
		public const int NUM_PARTICLE_GROUPS = 1;

		public const int DEFAULT_SEPARATION = 4;
		public const double DEFAULT_MAX_MASS = 1;
		#endregion Particles

		#region World
		public const int WINDOW_WIDTH = 160;//160 x 80 max, practical min width of graph width if that is enabled
		public const int WINDOW_HEIGHT = 80;
		public const double WORLD_SCALE = 400d;
		public const double WORLD_ASPECT_RATIO = 1d;
		#endregion World

		#region Boids
		public const int SUBFRAME_MULTIPLE = 1;

		public const int DESIRED_NEIGHBORS = 8;
		public const bool ENABLE_COHESION = false;
		public const bool ENABLE_ALIGNMENT = true;
		public const bool ENABLE_SEPARATION = true;
		
		public const double DEFAULT_SPEED_DECAY = 0.1;

		public const double DEFAULT_SEPARATION_WEIGHT = 2;
		public const double DEFAULT_ALIGNMENT_WEIGHT = 0.5;
		public const double DEFAULT_COHESION_WEIGHT = 0.5;

		public const double MAX_ACCELERATION = 0.1;
		public const double DEFAULT_MAX_ACCELERATION = 0.05;
		public const double DEFAULT_MAX_SPEED = 0.4;
		public const double DEFAULT_MAX_STARTING_SPEED = 1;

		public const double DEFAULT_MAX_IMPULSE_COHESION = 1;
		public const double DEFAULT_MAX_IMPULSE_ALIGNMENT = 0.5;
		public const double DEFAULT_MAX_IMPULSE_SEPARATION = 2;
		#endregion Boids

		#region Aux
		public const char CHAR_TOP = '\u2580';//▀
		public const char CHAR_BOTTOM = '\u2584';//▄
		public const char CHAR_BOTH = '\u2588';//█

		public const double TARGET_FPS_DEFAULT = 30d;
		public const int PERF_WARN_MS = 2000;
		public const int NUMBER_ACCURACY = 2;
		public const int NUMBER_SPACING = 5;

		public const int GRAPH_WIDTH = 92;
		public const int GRAPH_HEIGHT = 8;
		public const int PERF_GRAPH_FRAMES_PER_COLUMN = 10;
		public const double PERF_SMA_ALPHA = 0.05d;

		public const int TREE_REFRESH_FRAMES = 10;
		public const double AUTOSCALING_REFRESH_FRAMES = 30;
		public const double AUTOSCALING_SMA_ALPHA = 0.4d;
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