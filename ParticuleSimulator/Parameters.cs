using System;
using System.Linq;
using System.Threading.Tasks;
using Generic.Vectors;
using ParticleSimulator.Simulation;

namespace ParticleSimulator {
	//NOTE sentinel value is usually -1 for unlimited
	public static class Parameters {
		public const SimulationType SimType = SimulationType.Gravity;

		public const int PARTICLES_GROUP_COUNT = 500;
		public const int PARTICLES_GROUP_MIN = 1;
		public const int PARTICLES_GROUP_MAX = 200;
		public const double PARTICLES_GROUP_SIZE_SKEW_POWER = 4d;

		public const double WORLD_SCALE = 1d;
		public const double TIME_SCALE = 1d;
		public const int DIM = 2;

		public const double STARTING_SPEED_MAX_GROUP = 8E-3;
		public const double STARTING_SPEED_MAX_GROUP_RAND = 0E-3;
		public const double STARTING_SPEED_MAX_INTRAGROUP = 8E-3;
		public const double STARTING_SPEED_MAX_INTRAGROUP_RAND = 0E-3;
		
		public const double WORLD_DEATH_BOUND_CNT = 1E3;
		public const bool WORLD_WRAPPING = false;
		public const bool WORLD_BOUNDING = false;
		public const double WORLD_PADDING_PCT = 10d;
		public const double WORLD_BOUNCE_WEIGHT = 0;//0.00001d;
		public const double WORLD_EPSILON = 1E-8;
		
		public const bool LEGEND_ENABLE = true;
		public const ParticleColoringMethod COLOR_SCHEME = ParticleColoringMethod.Density;
		public static readonly ConsoleColor[] COLOR_ARRAY = ColoringScales.Grayscale;
		public const bool DENSITY_AUTOSCALE_PERCENTILE = true;//only applies to Density coloring
		public const double DENSITY_AUTOSCALE_CUTOFF_PCT = 0d;
		
		public const double TARGET_FPS = -1;
		public const double MAX_FPS = 30;
		public static readonly int WINDOW_WIDTH = Console.LargestWindowWidth;
		public static readonly int WINDOW_HEIGHT = Console.LargestWindowHeight;//using top and bottom halves of each character to get double the verticle resolution

		public const bool PERF_ENABLE = true;
		public const bool PERF_STATS_ENABLE = true;
		public const bool PERF_GRAPH_ENABLE = true;
		
		public const bool ENABLE_ASYNCHRONOUS = true;
		public const int PRECALCULATION_LIMIT = 1;
		public const int SIMULATION_PARALLELISM = 16;
		public const int SIMULATION_SKIPS = 0;//run the simulation multiple times per render
		public const int TREE_REFRESH_REUSE_ALLOWANCE = 7;
		public const bool SYNC_SIMULATION = true;
		public const bool SYNC_TREE_REFRESH = false;

		#region Gravity
		public const double GRAVITATIONAL_CONSTANT = 1E-10;
		public const double GRAVITY_DENSITY = 1E3;
		public const double GRAVITY_INITIAL_SEPARATION = 0.008d;
		public const double GRAVITY_ALIGNMENT_SKEW_POW = 4d;
		public const double GRAVITY_ALIGNMENT_SKEW_RANGE_PCT = 0d;

		public const double GRAVITY_MIN_MASS = 1E0;
		public const double GRAVITY_MAX_MASS = 1E0;
		public const double GRAVITY_MASS_BIAS = 3d;
		
		public const double GRAVITY_MAX_ACCEL = 10d;
		public const int GRAVITY_QUADTREE_NODE_CAPACITY = 24;

		public const double GRAVITY_NEIGHBORHOOD_RADIUS_MULTIPLE = 8d;
		public const int GRAVITY_NEIGHBORHOOD_FILTER = 3;
		#endregion Gravity

		#region Boids;
		public const int BOIDS_DESIRED_INTERACTION_NEIGHBORS= 8;
		public const int BOIDS_QUADTREE_NODE_CAPACITY		= 5;

		public const double BOIDS_INITIAL_SEPARATION	= 0.015;

		public const double BOIDS_BOID_VISION			= -1;
		public const double BOIDS_PREDATOR_VISION		= -1;
		public const double BOIDS_BOID_FOV_RADIANS		= -1;
		public const double BOIDS_PREDATOR_FOV_RADIANS	= -1;

		public const double BOIDS_BOID_MIN_SPEED		= 0.0005d;
		public const double BOIDS_PREDATOR_MIN_SPEED	= 0d;
		public const double BOIDS_BOID_MAX_SPEED		= 0.002d;
		public const double BOIDS_PREDATOR_MAX_SPEED	= 0.01d;
		
		public const double BOIDS_PREDATOR_CHANCE		= 0d;
		public const double BOIDS_PREDATOR_CHANCE_BIAS	= 1d;
		public const ConsoleColor BOIDS_PREDATOR_COLOR	= ConsoleColor.White;

		public const bool BOIDS_REPULSION_ENABLE				= true;
		public const double BOIDS_BOID_REPULSION_DIST			= 0.05d;
		public const double BOIDS_BOID_GROUP_REPULSION_DIST		= 0.05d;
		public const double BOIDS_BOID_REPULSION_W				= 0.001d;
		public const double BOIDS_BOID_FLEE_REPULSION_W			= 0.005d;
		public const double BOIDS_BOID_GROUP_REPULSION_W		= 0.0015d;
		public const double BOIDS_PREDATOR_REPULSION_DIST		= 0.01d;
		public const double BOIDS_PREDATOR_GROUP_REPULSION_DIST	= 0.4d;
		public const double BOIDS_PREDATOR_REPULSION_W			= 0.0003d;
		public const double BOIDS_PREDATOR_GROUP_REPULSION_W	= 0.001d;

		public const bool BOIDS_COHERE_ENABLE				= true;
		public const double BOIDS_BOID_COHESION_W			= 0.0001d;
		public const double BOIDS_BOID_COHESION_DIST		= 0.1d;
		public const double BOIDS_PREDATOR_COHESION_W		= 0.0006d;
		public const double BOIDS_PREDATOR_COHESION_DIST	= 0.1d;

		public const bool BOIDS_ALIGN_ENABLE			= true;
		public const double BOIDS_BOID_ALIGNMENT_W		= 0.01d;
		public const double BOIDS_PREDATOR_ALIGNMENT_W	= 0.00002d;
		#endregion Boids

		#region Aux
		public const char CHAR_LOW  = '\u2584';//▄
		public const char CHAR_BOTH = '\u2588';//█
		public const char CHAR_TOP  = '\u2580';//▀

		public const double TARGET_FPS_DEFAULT = 30d;
		public const int PERF_WARN_MS = 2000;
		public const int CONSOLE_TITLE_INTERVAL_MS = 500;
		public const int AUTOSCALE_INTERVAL_MS = 1000;
		public const int PERF_GRAPH_REFRESH_MS = 250;

		public const int NUMBER_ACCURACY = 2;
		public const int NUMBER_SPACING = 5;
		public const double PERF_SMA_ALPHA = 0.1d;

		public const int GRAPH_WIDTH = -1;
		public const int GRAPH_HEIGHT = 5;//at least 2
		public const int PERF_GRAPH_DEFAULT_WIDTH = 32;
		public const int PERF_GRAPH_FRAMES_PER_COLUMN = 20;
		public const double PERF_GRAPH_PERCENTILE_CUTOFF = 10d;
		
		public static readonly double WORLD_ASPECT_RATIO = WINDOW_WIDTH / (2d * WINDOW_HEIGHT);
		public static readonly double[] DOMAIN_SIZE = Enumerable.Repeat(1d, DIM - 1).Prepend(WORLD_ASPECT_RATIO).ToArray().Multiply(WORLD_SCALE);
		public static readonly double[] DOMAIN_CENTER = DOMAIN_SIZE.Multiply(0.5d); 
		public static readonly double DOMAIN_MAX_RADIUS = DOMAIN_SIZE.Max() / (2d + WORLD_PADDING_PCT/25d);
		public static readonly double DOMAIN_HIDDEN_DIMENSIONAL_HEIGHT = DIM < 3 ? 0d : DOMAIN_SIZE.Skip(2).ToArray().Magnitude();
		public static readonly ParallelOptions MulithreadedOptions = new() { MaxDegreeOfParallelism = SIMULATION_PARALLELISM };
		#endregion Aux
	}
}