using System;
using System.Linq;
using Generic.Models;

namespace ParticleSimulator {
	public static class Parameters {
		//NOTE sentinel value is usually -1 for unlimited

		public const bool ENABLE_ASYNCHRONOUS = true;
		public const bool LEGEND_ENABLE = false;
		public const bool DENSITY_AUTOSCALE_ENABLE = false;
		public const bool PERF_ENABLE = true;
		public const bool PERF_STATS_ENABLE = true;
		public const bool PERF_GRAPH_ENABLE = true;

		#region Particles
		public const bool COLOR_GROUPS = true;

		public const int NUM_PARTICLES_PER_GROUP = 200;
		public const int NUM_PARTICLE_GROUPS = 30;//only 14 colors are available
		public const double INITIAL_SEPARATION = 20;
		
		public const double MAX_STARTING_SPEED = -1;
		public const int DESIRED_INTERACTION_NEIGHBORS = 8;
		#endregion Particles

		#region Sizes
		public const int WINDOW_WIDTH = 160;//160 x 80 max, practical min width of graph width if that is enabled
		public const int WINDOW_HEIGHT = 80;//using top and bottom halves of each character to get double the verticle resolution

		public const double RENDER_3D_PHI = Math.PI / 4d;

		public const int DIMENSIONALITY = 3;
		public const double WORLD_SCALE = 1200d;
		public const double WORLD_EPSILON = 0.0001d;
		public const bool WORLD_WRAPPING = false;//TODO
		public const double WORLD_BOUNCE_WEIGHT = 0.0004d;

		public const int GRAPH_WIDTH = -1;
		public const int GRAPH_HEIGHT = 7;//at least 2
		#endregion Sizes

		#region Timings
		public const double TARGET_FPS = -1;
		public const double MAX_FPS = -1;

		public const int SIMULATION_SKIPS = 0;
		public const int TREE_REFRESH_SKIPS = 4;
		public const int QUADTREE_NODE_CAPACITY = 8;
		public const int AUTOSCALING_REFRESH_FRAMES = 30;

		public const bool SYNC_SIMULATION = true;
		public const bool SYNC_TREE_REFRESH = false;
		//how far ahead steps can work (when applicable)
		//set to zero to force everything to be synchronous
		public const int PRECALCULATION_LIMIT = 2;//no benefit to larger values than one
		#endregion Timings

		#region Boids
		public const bool BOIDS_ENABLE_COHESION				= true;
		public const bool BOIDS_ENABLE_ALIGNMENT			= true;
		public const bool BOIDS_ENABLE_SEPARATION			= true;
		public const double BOIDS_PREDATOR_CHANCE_BIAS		= 0.00d;

		public const double BOIDS_BOID_MIN_SPEED			= 2d;
		public const double BOIDS_BOID_MAX_SPEED			= 3d;
		public const double BOIDS_BOID_SPEED_DECAY			= 0.25d;//used as E^-val
		public const double BOIDS_PREDATOR_MIN_SPEED		= 1d;
		public const double BOIDS_PREDATOR_MAX_SPEED		= 5d;
		public const double BOIDS_PREDATOR_SPEED_DECAY		= 0.02d;//used as E^-val

		public const double BOIDS_BOID_VISION				= 400d;
		public const double BOIDS_BOID_FOV_RADIANS			= -1;
		public const double BOIDS_PREDATOR_VISION			= 750d;
		public const double BOIDS_PREDATOR_FOV_RADIANS		= -1;

		public const double BOIDS_BOID_MIN_DIST				= 60d;
		public const double BOIDS_BOID_COHESION_DIST		= 100d;
		public const double BOIDS_BOID_GROUP_AVOID_DIST		= 200d;
		public const double BOIDS_PREDATOR_MIN_DIST			= 10d;
		public const double BOIDS_PREDATOR_COHESION_DIST	= 40d;
		public const double BOIDS_PREDATOR_GROUP_AVOID_DIST	= 400d;

		public const double BOIDS_BOID_DISPERSE_W			= 0.0005d;
		public const double BOIDS_BOID_COHESION_W			= 0.05d;
		public const double BOIDS_BOID_ALIGNMENT_W			= 0.1d;
		public const double BOIDS_BOID_GROUP_AVOID_WE		= 0.2d;

		public const double BOIDS_PREDATOR_DISPERSE_W		= 0.0002d;
		public const double BOIDS_PREDATOR_COHESION_W		= 0.001d;
		public const double BOIDS_PREDATOR_ALIGNMENT_W		= 0.00001d;
		public const double BOIDS_PREDATOR_GROUP_AVOID_WE	= 2.0d;

		public const double BOIDS_FLEE_WE					= 3.0d;
		public const double BOIDS_CHASE_WE					= 2.0d;
		
		public const ConsoleColor BOIDS_PREDATOR_COLOR		= ConsoleColor.DarkRed;
		#endregion Boids

		#region Gravity
		public const double MIN_MASS = 1;
		public const double MAX_MASS = 1;

		public const int NEIGHBORHOOD_FILTER = 5;
		#endregion Gravity

		#region Aux
		public const char CHAR_LOW  = '\u2584';//▄
		public const char CHAR_BOTH = '\u2588';//█
		public const char CHAR_TOP  = '\u2580';//▀

		public const double TARGET_FPS_DEFAULT = 30d;
		public const int PERF_WARN_MS = 2000;

		public const int NUMBER_ACCURACY = 2;
		public const int NUMBER_SPACING = 5;
		public const double PERF_SMA_ALPHA = 0.1d;

		public const int PERF_GRAPH_REFRESH_MS = 125;
		public const int PERF_GRAPH_DEFAULT_WIDTH = 30;
		public const int PERF_GRAPH_FRAMES_PER_COLUMN = 10;
		public const double PERF_GRAPH_PERCENTILE_LOW_CUTOFF = 0d;
		public const double PERF_GRAPH_PERCENTILE_HIGH_CUTOFF = 0d;
		public const int PERF_GRAPH_NUMBER_ACCURACY = 3;

		public const double AUTOSCALING_SMA_ALPHA = 0.4d;
		public const int AUTOSCALE_INTERVAL_MS = 250;

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

		public static readonly Tuple<double, ConsoleColor>[] RatioColors = new Tuple<double, ConsoleColor>[] {
			new Tuple<double, ConsoleColor>(1.05d, ConsoleColor.Cyan),
			new Tuple<double, ConsoleColor>(1.00d, ConsoleColor.DarkGreen),
			new Tuple<double, ConsoleColor>(0.95d, ConsoleColor.Green),
			new Tuple<double, ConsoleColor>(0.67d, ConsoleColor.Yellow),
			new Tuple<double, ConsoleColor>(0.50d, ConsoleColor.DarkYellow),
			new Tuple<double, ConsoleColor>(0.33d, ConsoleColor.Magenta),
			new Tuple<double, ConsoleColor>(0.25d, ConsoleColor.Red),
			new Tuple<double, ConsoleColor>(0.10d, ConsoleColor.DarkRed),
			new Tuple<double, ConsoleColor>(0.00d, ConsoleColor.DarkRed),
			new Tuple<double, ConsoleColor>(double.NegativeInfinity, ConsoleColor.White)
		};
		
		public static readonly double WORLD_ASPECT_RATIO = WINDOW_WIDTH / (2d * WINDOW_HEIGHT);
		public static readonly double[] DOMAIN = Enumerable.Repeat(1d, DIMENSIONALITY - 1).Prepend(WORLD_ASPECT_RATIO).ToArray().Multiply(WORLD_SCALE);
		public static readonly double[] DOMAIN_CENTER = DOMAIN.Multiply(0.5d); 
		public static readonly double DOMAIN_MAX_RADIUS = DOMAIN.Max()/2D;
		public static readonly double DOMAIN_HIDDEN_DIMENSIONAL_HEIGHT = DOMAIN.Length < 3 ? 0d : DOMAIN.Skip(2).ToArray().Magnitude();
		#endregion Aux
	}
}