using System;
using System.Linq;
using System.Numerics;
using Generic.Extensions;
using Generic.Vectors;
using ParticleSimulator.Rendering;

namespace ParticleSimulator {
	//sentinel value is usually -1 for unlimited
	public static class Parameters {
		public const int PARTICLES_GROUP_COUNT = 128;
		public const int PARTICLES_GROUP_MIN = 1;
		public const int PARTICLES_GROUP_MAX = 1;
		public const float PARTICLES_GROUP_SIZE_SKEW_POWER = 0f;//0 for max size

		public const int DIM = 3;
		public const float WORLD_SCALE = 1f;
		public const float TIME_SCALE = 1f;//can be any value, including negative
		
		public const float ADAPTIVE_TIME_GRANULARITY = 0.01f;//subdivide time steps as necessary for very close interactions
		public const float ADAPTIVE_TIME_CRITERION = 0.01f;//a weighted value based on range and velocity to other particles in the nearfield group
		public const float PARTICLE_MAX_ACCELERATION = 0.0025f;
		
		public const bool WORLD_BOUNCING = true;
		public const bool WORLD_WRAPPING = false;
		public const bool WORLD_BOUNDING = false;
		public const float WORLD_EPSILON = 1E-4f;
		public const float WORLD_DEATH_BOUND_CNT = 100f;
		public const float WORLD_PADDING_PCT = 25f;
		
		public const ParticleColoringMethod COLOR_METHOD = ParticleColoringMethod.Random;
		public static readonly ConsoleColor[] COLOR_ARRAY = ColoringScales.DEFAULT_CONSOLE_COLORS;
		public const bool COLOR_USE_FIXED_BANDS = false;
		public static readonly float[] COLOR_FIXED_BANDS = Enumerable.Range(0, COLOR_ARRAY.Length).Select(i => (float)(1 << 2*i)).ToArray();

		public const bool LEGEND_ENABLE = true;
		public const bool AUTOSCALE_PERCENTILE = true;
		public const float AUTOSCALE_CUTOFF_PCT = 0f;
		public const float AUTOSCALE_MIN_STEP = 1f;
		
		public const float MAX_FPS = 30;
		public const float TARGET_FPS = -1;
		public static readonly int WINDOW_WIDTH = Console.LargestWindowWidth;
		public static readonly int WINDOW_HEIGHT = Console.LargestWindowHeight;//using top and bottom halves of each character to get float the verticle resolution

		public const bool PERF_ENABLE = true;
		public const bool PERF_STATS_ENABLE = true;
		public const bool PERF_GRAPH_ENABLE = true;
		
		public const int PRECALCULATION_LIMIT = 1;//how many calculations ahead steps can work
		public const int SIMULATION_SKIPS = 0;//run the simulation multiple times between result sets
		public const bool SYNC_SIMULATION = true;//synchronizes simulation to not start until rendering finishes (with precalculation limit still)

		#region Gravity
		public const float GRAVITY_INITIAL_SEPARATION_SCALER = 0.2f;

		public const float GRAVITATIONAL_CONSTANT = 6E-10f;
		public const float ELECTROSTATIC_CONSTANT = 1E-9f;
		public const float MASS_LUMINOSITY_SCALAR = 1E-1f;
		public const float GRAVITY_RADIAL_DENSITY = 2E3f;

		public const float ELECTROSTATIC_MIN_CHARGE = 0f;
		public const float ELECTROSTATIC_MAX_CHARGE = 0f;

		public const float GRAVITY_MIN_STARTING_MASS = 100f;
		public const float GRAVITY_MAX_STARTING_MASS = 200f;

		public const float GRAVITY_CRITICAL_MASS = 1024f;
		public const int GRAVITY_EJECTA_NUM_PARTICLES = 16;
		public const float GRAVITY_EJECTA_SPEED = 0.002f;

		public const float GRAVITY_STARTING_SPEED_MAX_GROUP = 0E-4f;
		public const float GRAVITY_STARTING_SPEED_MAX_GROUP_RAND = 0E-4f;
		public const float GRAVITY_STARTING_SPEED_MAX_INTRAGROUP = 0E-4f;
		public const float GRAVITY_STARTING_SPEED_MAX_INTRAGROUP_RAND = 5E-3f;
		public const float GRAVITY_ALIGNMENT_SKEW_POW = 4f;
		public const float GRAVITY_ALIGNMENT_SKEW_RANGE_PCT = 0f;

		public const bool GRAVITY_COLLISION_COMBINE = true;
		public const float GRAVITY_COMBINE_OVERLAP_CUTOFF_BARYON_ERROR = 0.2f;//1 means merely touching
		//public const float GRAVITY_COLLISION_DRAG_STRENGTH = 0f;//1 means instant stop
		
		public const int QUADTREE_NODE_CAPACITY = 1;
		#endregion Gravity

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
		public const float PERF_SMA_ALPHA = 0.05f;

		public const int GRAPH_WIDTH = -1;
		public const int GRAPH_HEIGHT = 7;//at least 2
		public const int PERF_GRAPH_DEFAULT_WIDTH = 32;
		public const int PERF_GRAPH_FRAMES_PER_COLUMN = 20;
		public const float PERF_GRAPH_PERCENTILE_CUTOFF = 10f;
		
		public static readonly float WORLD_ASPECT_RATIO = WINDOW_WIDTH / (2f * WINDOW_HEIGHT);
		public static readonly Vector<float> DOMAIN_SIZE = VectorFunctions.New(Enumerable.Repeat(WORLD_SCALE, DIM - 1).Prepend(WORLD_SCALE * WORLD_ASPECT_RATIO));
		public static readonly Vector<float> DOMAIN_CENTER = DOMAIN_SIZE * 0.5f; 
		public static readonly float DOMAIN_MAX_RADIUS = Enumerable.Range(0, DIM).MinBy(d => DOMAIN_SIZE[d]) / (2f + WORLD_PADDING_PCT/25f);
		public static readonly bool AUTOSCALER_ENABLE =
			!COLOR_USE_FIXED_BANDS && COLOR_ARRAY.Length > 1
			&& COLOR_METHOD != ParticleColoringMethod.Depth
			&& COLOR_METHOD != ParticleColoringMethod.Group
			&& COLOR_METHOD != ParticleColoringMethod.Random;
		#endregion Aux
	}
}