using Generic;

namespace Boids {
	//TODO world wrap interaction: boids look forward only into adjoining quadrant
	public static class Parameters {
		#region Boids
		//total number of boids is flocks * boidsPerFlock
		//significantly affects performance
		public const int NUM_BOIDS_PER_FLOCK = 3000;
		public const int NUM_FLOCKS = 1;
		public const int RATED_BOIDS = 3000;

		public const int DESIRED_NEIGHBORS = 10;
		public const bool ENABLE_COHESION = false;
		public const bool ENABLE_ALIGNMENT = true;
		public const bool ENABLE_SEPARATION = true;
		
		public const int DEFAULT_SEPARATION = 8;
		public const double DEFAULT_SPEED_DECAY = 0.1;
		#endregion Boids

		#region Timing
		public const double TARGET_FPS = -1;
		public const int SUBFRAME_MULTIPLE = 1;
		public const int RUNTIME_LIMIT_MS = -1;
		public const int MIN_DISPLAY_TIME_MS = 0;//16
		public static double UPDATE_INTERVAL_MS { get { return TARGET_FPS <= 0 ? 0 : 1000d / TARGET_FPS; } }
		public static double NOMINAL_UPDATE_INTERVAL_MS { get { return 1000d / (TARGET_FPS <= 0 ? 30 : TARGET_FPS); } }
		public const int QUADTREE_REFRESH_FRAMES = 10;
		#endregion Timing

		#region World
		//160 x 80 max
		public const int WIDTH = 160;
		public const int HEIGHT = 80;
		//only tangible performance effect from number of characters drawn
		public const double WORLD_SCALE = 800;
		public static readonly double[] Domain = 
			new double[] { WIDTH / (2d * HEIGHT), 1d }
			.Multiply(Parameters.WORLD_SCALE);
		#endregion World

		#region Legend
		public const bool LEGEND_ENABLE = true;
		public const bool DENSITY_AUTOSCALE_ENABLE = true;
		public const double AUTOSCALING_REFRESH_FRAMES = 30;
		public const double AUTOSCALING_SMA_ALPHA = 0.4d;
		#endregion Legend

		#region Performance Monitoring
		public const bool PERF_STATS_ENABLE = true;
		public const bool PERF_GRAPH_ENABLE = true;
		public const int PERF_GRAPH_FRAMES_PER_COLUMN = 10;
		public const double PERF_SMA_ALPHA = 0.1d;
		public const int PERF_MIN_INTERVAL_MS = 125;
		public const int PERF_MAX_INTERVAL_MS = 1000;
		#endregion Performance Monitoring
	}
}