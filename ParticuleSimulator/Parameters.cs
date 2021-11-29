using System;
using System.Linq;
using System.Threading.Tasks;
using Generic.Models.Vectors;
using ParticleSimulator.Simulation;

namespace ParticleSimulator {
	//NOTE sentinel value is usually -1 for unlimited
	public static class Parameters {
		public const SimulationType SimType = SimulationType.Gravity;

		public const bool ENABLE_ASYNCHRONOUS = true;
		public const bool LEGEND_ENABLE = true;
		public const bool PERF_ENABLE = true;
		public const bool PERF_STATS_ENABLE = true;
		public const bool PERF_GRAPH_ENABLE = true;
		
		public const int PARTICLES_PER_GROUP_MAX = 100;
		public const int PARTICLE_GROUPS_NUM = 1000;

		public const double WORLD_SCALE = 1E3;
		public const bool WORLD_WRAPPING = false;
		public const bool WORLD_BOUNDING = false;

		public const int PRECALCULATION_LIMIT = 2;
		public const int SIMULATION_PARALLELISM = 16;

		#region Particles
		public const ParticleColoringMethod COLOR_SCHEME = ParticleColoringMethod.Density;
		public const ParticleColorScale COLOR_SCALE = ParticleColorScale.Grayscale;
		public const bool DENSITY_AUTOSCALE_PERCENTILE = false;//only applies to Density coloring

		public const double INITIAL_SEPARATION = 2E0;
		
		public const double MAX_STARTING_SPEED_PCT = 1E-4;
		public const double MAX_GROUP_STARTING_SPEED_PCT = 2E-4;
		#endregion Particles

		#region Sizes
		public const int WINDOW_WIDTH = 160;
		public const int WINDOW_HEIGHT = 80;//using top and bottom halves of each character to get double the verticle resolution

		public const int DIM = 2;
		public const double WORLD_BOUNCE_WEIGHT = 0d;
		public const double WORLD_BOUNCE_EDGE_PCT = 10d;
		public const double WORLD_EPSILON = 0.0001d;

		public const int GRAPH_WIDTH = -1;
		public const int GRAPH_HEIGHT = 7;//at least 2
		#endregion Sizes

		#region Timings
		public const double TARGET_FPS = -1;
		public const double MAX_FPS = 36;

		public const int SIMULATION_SKIPS = 0;//run the simulation multiple times per render
		public const int TREE_REFRESH_REUSE_ALLOWANCE = 7;

		public const bool SYNC_SIMULATION = true;
		public const bool SYNC_TREE_REFRESH = false;
		#endregion Timings

		#region Gravity
		public const double GRAVITATIONAL_CONSTANT = 1E-3;

		public const double GRAVITY_MIN_MASS = 1E-2;
		public const double GRAVITY_MAX_MASS = 1E1;
		public const double GRAVITY_MASS_BIAS = 2d;

		public const double GRAVITY_DENSITY = 1d;
		public const double GRAVITY_COMPRESSION_SCALING_POW = 10d;

		public const double GRAVITY_DEATH_BOUND_CNT = 100d;
		public const int GRAVITY_QUADTREE_NODE_CAPACITY = 5;

		public const int GRAVITY_NEIGHBORHOOD_FILTER = 2;
		#endregion Gravity

		#region Boids
		public const bool BOIDS_ENABLE_COHESION				= true;
		public const bool BOIDS_ENABLE_ALIGNMENT			= true;
		public const bool BOIDS_ENABLE_SEPARATION			= true;

		public const int BOIDS_DESIRED_INTERACTION_NEIGHBORS= 8;

		public const double BOIDS_PREDATOR_CHANCE			= 0d;
		public const double BOIDS_PREDATOR_CHANCE_BIAS		= 1d;

		public const double BOIDS_BOID_MIN_SPEED			= 3d;
		public const double BOIDS_BOID_MAX_SPEED			= 5d;
		public const double BOIDS_BOID_SPEED_DECAY			= 0.0d;//used as E^-val
		public const double BOIDS_PREDATOR_MIN_SPEED		= 1d;
		public const double BOIDS_PREDATOR_MAX_SPEED		= 12d;
		public const double BOIDS_PREDATOR_SPEED_DECAY		= 0.01d;//used as E^-val

		public const double BOIDS_BOID_VISION				= 400d;
		public const double BOIDS_BOID_FOV_RADIANS			= -1;
		public const double BOIDS_PREDATOR_VISION			= 800d;
		public const double BOIDS_PREDATOR_FOV_RADIANS		= -1;

		public const double BOIDS_BOID_MIN_DIST				= 20d;
		public const double BOIDS_BOID_COHESION_DIST		= 100d;
		public const double BOIDS_BOID_GROUP_AVOID_DIST		= 150d;
		public const double BOIDS_PREDATOR_MIN_DIST			= 10d;
		public const double BOIDS_PREDATOR_COHESION_DIST	= 40d;
		public const double BOIDS_PREDATOR_GROUP_AVOID_DIST	= 500d;

		public const double BOIDS_BOID_DISPERSE_W			= 0.01d;
		public const double BOIDS_BOID_COHESION_W			= 0.1d;
		public const double BOIDS_BOID_ALIGNMENT_W			= 0.2d;
		public const double BOIDS_BOID_GROUP_AVOID_WE		= 0.03d;

		public const double BOIDS_PREDATOR_DISPERSE_W		= 0.0003d;
		public const double BOIDS_PREDATOR_COHESION_W		= 0.0006d;
		public const double BOIDS_PREDATOR_ALIGNMENT_W		= 0.00002d;
		public const double BOIDS_PREDATOR_GROUP_AVOID_WE	= 2.0d;

		public const double BOIDS_FLEE_WE					= 4.0d;
		public const double BOIDS_CHASE_WE					= 4.0d;
		
		public const ConsoleColor BOIDS_PREDATOR_COLOR		= ConsoleColor.White;
		#endregion Boids

		#region Aux
		public const char CHAR_LOW  = '\u2584';//▄
		public const char CHAR_BOTH = '\u2588';//█
		public const char CHAR_TOP  = '\u2580';//▀

		public const double TARGET_FPS_DEFAULT = 30d;
		public const int PERF_WARN_MS = 2000;
		public const int CONSOLE_TITLE_INTERVAL_MS = 500;
		public const int AUTOSCALE_INTERVAL_MS = 1000;

		public const int NUMBER_ACCURACY = 2;
		public const int NUMBER_SPACING = 5;
		public const double PERF_SMA_ALPHA = 0.15d;

		public const int PERF_GRAPH_REFRESH_MS = 125;
		public const int PERF_GRAPH_DEFAULT_WIDTH = 30;
		public const int PERF_GRAPH_FRAMES_PER_COLUMN = 10;
		public const double PERF_GRAPH_PERCENTILE_LOW_CUTOFF = 0d;
		public const double PERF_GRAPH_PERCENTILE_HIGH_CUTOFF = 0d;
		public const int PERF_GRAPH_NUMBER_ACCURACY = 3;
		
		public static readonly double WORLD_ASPECT_RATIO = WINDOW_WIDTH / (2d * WINDOW_HEIGHT);
		public static readonly double[] DOMAIN = Enumerable.Repeat(1d, DIM - 1).Prepend(WORLD_ASPECT_RATIO).ToArray().Multiply(WORLD_SCALE);
		public static readonly double[] DOMAIN_CENTER = DOMAIN.Multiply(0.5d); 
		public static readonly double DOMAIN_MAX_RADIUS = DOMAIN.Max() / (2d + WORLD_BOUNCE_EDGE_PCT/25d);
		public static readonly double DOMAIN_HIDDEN_DIMENSIONAL_HEIGHT = DIM < 3 ? 0d : DOMAIN.Skip(2).ToArray().Magnitude();
		public static readonly ConsoleColor[] COLOR_ARRAY =
			COLOR_SCALE == ParticleColorScale.DefaultConsoleColors
				? ColoringScales.DEFAULT_CONSOLE_COLORS
				: COLOR_SCALE == ParticleColorScale.Grayscale
					? ColoringScales.Grayscale
					: COLOR_SCALE == ParticleColorScale.ReducedColors
						? ColoringScales.Reduced
						: ColoringScales.Radar;
		public static readonly ParallelOptions MulithreadedOptions = new() { MaxDegreeOfParallelism = SIMULATION_PARALLELISM };
		#endregion Aux
	}
}