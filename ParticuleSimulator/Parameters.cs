using System;
using System.Linq;
using System.Numerics;
using Generic.Extensions;
using Generic.Vectors;
using ParticleSimulator.Simulation;

namespace ParticleSimulator {
	//sentinel value is usually -1 for unlimited
	public static class Parameters {
		public const SimulationType SIM_TYPE = SimulationType.Gravity;

		public const float PARTICLE_MAX_ACCELERATION = 0.0025f;
		public const int PARTICLES_GROUP_COUNT = 100;
		public const int PARTICLES_GROUP_MIN = 1;
		public const int PARTICLES_GROUP_MAX = 10;
		public const float PARTICLES_GROUP_SIZE_SKEW_POWER = 1f;//0 for max size

		public const float WORLD_SCALE = 2f;
		public const float TIME_SCALE = 1f;
		public const int DIM = 2;
		
		public const float ADAPTIVE_TIME_GRANULARITY = 0.01f;//subdivide time steps as necessary for very close interactions
		public const float ADAPTIVE_TIME_CRITERION = 0.01f;//a weighted value based on range and velocity to other particles in the nearfield group
		
		public const bool WORLD_WRAPPING = false;
		public const bool WORLD_BOUNDING = false;
		public const float WORLD_EPSILON = 1E-8f;
		public const float WORLD_DEATH_BOUND_CNT = 1E3f;
		public const float WORLD_PADDING_PCT = 25f;
		
		public const ParticleColoringMethod COLOR_METHOD = ParticleColoringMethod.Luminosity;
		public const bool COLOR_USE_FIXED_BANDS = true;
		public static readonly ConsoleColor[] COLOR_ARRAY = ColoringScales.StarColors;
		public static readonly float[] COLOR_FIXED_BANDS = Enumerable.Range(0, COLOR_ARRAY.Length).Select(i => (float)(1 << 2*i)).ToArray();
		public const bool LEGEND_ENABLE = true;
		public const bool AUTOSCALE_PERCENTILE = true;
		public const float AUTOSCALE_CUTOFF_PCT = 0f;
		public const float AUTOSCALE_MIN_STEP = 1f;
		
		public const float TARGET_FPS = -1;
		public const float MAX_FPS = 30f;
		public static readonly int WINDOW_WIDTH = Console.LargestWindowWidth;
		public static readonly int WINDOW_HEIGHT = Console.LargestWindowHeight;//using top and bottom halves of each character to get float the verticle resolution

		public const bool PERF_ENABLE = true;
		public const bool PERF_STATS_ENABLE = true;
		public const bool PERF_GRAPH_ENABLE = true;
		
		public const bool SIMULATION_PARALLEL_ENABLE = true;
		public const int PRECALCULATION_LIMIT = 1;//how many calculations ahead steps can work
		//public const int SIMULATION_PARALLELISM = 16;//number of simulation threads available for processing trea leaves
		public const int SIMULATION_SKIPS = 0;//run the simulation multiple times between result sets
		public const int TREE_REFRESH_REUSE_ALLOWANCE = 1;//how many simulation cycles can occur on stale data
		public const bool SYNC_SIMULATION = true;//synchronizes simulation to not start until rendering finishes (with precalculation limit still)
		public const bool SYNC_TREE_REFRESH = false;//synchronizes tree refresh to not start until simulation refreshes (with reuse limit still)

		#region Gravity
		public const float GRAVITY_INITIAL_SEPARATION = 0.1f;

		public const float GRAVITATIONAL_CONSTANT = 6E-10f;
		public const float ELECTROSTATIC_CONSTANT = 1E-9f;
		public const float MASS_LUMINOSITY_SCALAR = 1E-1f;
		public const float GRAVITY_RADIAL_DENSITY = 1E4f;

		public const float ELECTROSTATIC_MIN_CHARGE = 0f;
		public const float ELECTROSTATIC_MAX_CHARGE = 0f;

		public const float GRAVITY_MIN_STARTING_MASS = 1E1f;
		public const float GRAVITY_MAX_STARTING_MASS = 1E2f;
		public const float GRAVITY_MASS_BIAS = 16f;

		public const float GRAVITY_CRITICAL_MASS = 1024f;
		public const int GRAVITY_EJECTA_NUM_PARTICLES = 16;
		public const float GRAVITY_EJECTA_SPEED = 0.002f;

		public const float GRAVITY_STARTING_SPEED_MAX_GROUP = 0E-2f;
		public const float GRAVITY_STARTING_SPEED_MAX_GROUP_RAND = 0f;
		public const float GRAVITY_STARTING_SPEED_MAX_INTRAGROUP = 4E-3f;
		public const float GRAVITY_STARTING_SPEED_MAX_INTRAGROUP_RAND = 0f;
		public const float GRAVITY_ALIGNMENT_SKEW_POW = 4f;
		public const float GRAVITY_ALIGNMENT_SKEW_RANGE_PCT = 0f;

		public const bool GRAVITY_COLLISION_COMBINE = true;
		public const float GRAVITY_COMBINE_OVERLAP_CUTOFF_BARYON_ERROR = 0.2f;//1 means merely touching
		//public const float GRAVITY_COLLISION_DRAG_STRENGTH = 0f;//1 means instant stop
		
		public const int GRAVITY_QUADTREE_NODE_CAPACITY = 4;
		#endregion Gravity

		#region Boids
		public const float BOIDS_INITIAL_SEPARATION	= 0.015f;
		public const float BOIDS_WORLD_BOUNCE_WEIGHT= 0.00001f;

		public const float BOIDS_BOID_VISION			= -1;
		public const float BOIDS_PREDATOR_VISION		= -1;
		public const float BOIDS_BOID_FOV_RADIANS		= -1;
		public const float BOIDS_PREDATOR_FOV_RADIANS	= -1;

		public const float BOIDS_BOID_MIN_SPEED		= 0.0005f;
		public const float BOIDS_PREDATOR_MIN_SPEED	= 0f;
		public const float BOIDS_BOID_MAX_SPEED		= 0.002f;
		public const float BOIDS_PREDATOR_MAX_SPEED	= 0.01f;

		public const float BOIDS_STARTING_SPEED_MAX_GROUP = 0f;
		public const float BOIDS_STARTING_SPEED_MAX_GROUP_RAND = 5E-3f;
		public const float BOIDS_STARTING_SPEED_MAX_INTRAGROUP = 0f;
		public const float BOIDS_STARTING_SPEED_MAX_INTRAGROUP_RAND = 2E-3f;
		
		public const float BOIDS_PREDATOR_CHANCE		= 0f;
		public const float BOIDS_PREDATOR_CHANCE_BIAS	= 1f;

		public const bool BOIDS_REPULSION_ENABLE				= true;
		public const float BOIDS_BOID_REPULSION_DIST			= 0.05f;
		public const float BOIDS_BOID_GROUP_REPULSION_DIST		= 0.05f;
		public const float BOIDS_BOID_REPULSION_W				= 0.001f;
		public const float BOIDS_BOID_FLEE_REPULSION_W			= 0.005f;
		public const float BOIDS_BOID_GROUP_REPULSION_W			= 0.0015f;
		public const float BOIDS_PREDATOR_REPULSION_DIST		= 0.01f;
		public const float BOIDS_PREDATOR_GROUP_REPULSION_DIST	= 0.4f;
		public const float BOIDS_PREDATOR_REPULSION_W			= 0.0003f;
		public const float BOIDS_PREDATOR_GROUP_REPULSION_W		= 0.001f;

		public const bool BOIDS_COHERE_ENABLE			= true;
		public const float BOIDS_BOID_COHESION_W		= 0.0001f;
		public const float BOIDS_BOID_COHESION_DIST		= 0.1f;
		public const float BOIDS_PREDATOR_COHESION_W	= 0.0006f;
		public const float BOIDS_PREDATOR_COHESION_DIST	= 0.1f;

		public const bool BOIDS_ALIGN_ENABLE			= true;
		public const float BOIDS_BOID_ALIGNMENT_W		= 0.01f;
		public const float BOIDS_PREDATOR_ALIGNMENT_W	= 0.00002f;

		public const int BOIDS_QUADTREE_NODE_CAPACITY		= 5;
		public const int BOIDS_DESIRED_INTERACTION_NEIGHBORS= 8;
		#endregion Boids

		#region Aux
		public const char CHAR_LOW  = '\u2584';//▄
		public const char CHAR_BOTH = '\u2588';//█
		public const char CHAR_TOP  = '\u2580';//▀

		public const float TARGET_FPS_DEFAULT = 30f;
		public const int PERF_WARN_MS = 2000;
		public const int CONSOLE_TITLE_INTERVAL_MS = 500;
		public const int AUTOSCALE_INTERVAL_MS = 1000;
		public const int PERF_GRAPH_REFRESH_MS = 250;

		public const int NUMBER_ACCURACY = 2;
		public const int NUMBER_SPACING = 5;
		public const float PERF_SMA_ALPHA = 0.1f;

		public const int GRAPH_WIDTH = -1;
		public const int GRAPH_HEIGHT = 10;//at least 2
		public const int PERF_GRAPH_DEFAULT_WIDTH = 32;
		public const int PERF_GRAPH_FRAMES_PER_COLUMN = 20;
		public const float PERF_GRAPH_PERCENTILE_CUTOFF = 10f;
		
		public static readonly float WORLD_ASPECT_RATIO = WINDOW_WIDTH / (2f * WINDOW_HEIGHT);
		public static readonly Vector<float> DOMAIN_SIZE = VectorFunctions.New(Enumerable.Repeat(WORLD_SCALE, DIM - 1).Prepend(WORLD_SCALE * WORLD_ASPECT_RATIO));
		public static readonly Vector<float> DOMAIN_CENTER = DOMAIN_SIZE * 0.5f; 
		public static readonly float DOMAIN_MAX_RADIUS = Enumerable.Range(0, DIM).MinBy(d => DOMAIN_SIZE[d]) / (2f + WORLD_PADDING_PCT/25f);
		public static readonly float DOMAIN_HIDDEN_DIMENSIONAL_HEIGHT = DIM < 3 ? 0f : MathF.Sqrt(Enumerable.Range(2, DIM - 2).Select(d => DOMAIN_SIZE[d]).Sum(x => x * x));
		#endregion Aux
	}
}