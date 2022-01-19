using System;
using System.Linq;
using System.Numerics;
using Generic.Vectors;
using ParticleSimulator.Rendering;
using ParticleSimulator.Rendering.SystemConsole;

namespace ParticleSimulator {
	//sentinel value is usually -1 for unlimited or to disable the feature
	public static class Parameters {
		public static readonly int WINDOW_WIDTH = 120;
		public static readonly int WINDOW_HEIGHT = 60;
		public const int SUPERSAMPLING = 2;
		//using top and bottom halves of each character to get double the verticle resolution
		public const float TARGET_FPS = 30;
		public const bool VSYNC = false;

		public const int FRAME_LIMIT = 600;
		public const int DETERMINISTIC_RANDOM_SEED = 0;

		public const int DIM = 3;
		public const float ZOOM_SCALE = 1f;
		public const float TIME_SCALE = 1f;
		public const float WORLD_SCALE = 2f;
		public const bool AUTOFOCUS_DEFAULT = true;
		public const float WORLD_ROTATION_RADS_PER_STEP = 0.005f;

		public const int PARTICLES_GROUP_COUNT = 1;
		public const int PARTICLES_GROUP_MIN = 1;
		public const int PARTICLES_GROUP_MAX = 20000;
		public const float PARTICLES_GROUP_SIZE_SKEW_POWER = 0f;//0 for max size

		public const float INACCURCY = 2f;
		public const float PIXEL_ROUNDOFF = 0.5f;
		public const int PRECALCULATION_LIMIT = 1;//how many calculations ahead steps can work
		public const bool PARALLEL_SIMULATION = true;//once tree data are ready, whether to parallelize particle interaction
		//public const int PARALLELISM_THREADS = 8;//tree preparation and particle evaluation
		public const bool SYNC_SIMULATION = true;//controls synchronization of rendering to simulation (e.g. faster rotation)
		public const int SIMULATION_SKIPS = 0;//refresh the simulation multiple times between renders
		public const int QUADTREE_LEAF_CAPACITY = 1;
		
		public const bool COLLISION_ENABLE = true;
		public const bool MERGE_ENABLE = true;
		public const float MERGE_ENGULF_RATIO = 0.9f;
		public const float DRAG_CONSTANT = 0.15f;

		public const float WORLD_PADDING_PCT = 5f;
		public const float WORLD_DEATH_BOUND_RADIUS = 10f;
		public const float WORLD_EPSILON = 2E-5f;
		
		public const bool WORLD_WRAPPING = false;
		public const bool WORLD_BOUNCING = false;
		public const float WORLD_X_ASPECT = 1f;
		public const float WORLD_Y_ASPECT = 1f;
		public const float WORLD_Z_ASPECT = 1f;
		
		public const ParticleColoringMethod COLOR_METHOD = ParticleColoringMethod.Luminosity;
		public static readonly ConsoleColor[] COLOR_ARRAY = ColoringScales.StarColors;
		//public static readonly ConsoleColor[] COLOR_ARRAY = new ConsoleColor[] { ConsoleColor.White };
		public const bool COLOR_USE_FIXED_BANDS = true;
		public static readonly float[] COLOR_FIXED_BANDS = Enumerable.Range(0, COLOR_ARRAY.Length).Select(i => MathF.Pow(10f, i)).ToArray();

		public const bool AUTOSCALE_PERCENTILE = false;
		public const float AUTOSCALE_FIXED_MIN = -1f;
		public const float AUTOSCALE_FIXED_MAX = -1f;
		public const float AUTOSCALE_CUTOFF_PCT = 0f;
		public const float AUTOSCALE_MIN_STEP = 1f;

		//public const bool EXPORT_FRAMES = false;//TODO
		//public const string EXPORT_DIR = null;	//TODO

		#region Gravity
		public const float GRAVITATIONAL_CONSTANT	= 1E-10f;
		//TODO add electrostatic forces

		public const float MASS_SCALAR				= 0.1f;
		public const float MASS_RADIAL_DENSITY		= 2E8f;
		public const float MASS_LUMINOSITY_SCALAR	= 1E-1f;
		public const float MASS_LUMINOSITY_POW		= 3f;

		public const float GALAXY_RADIUS			= 0.5f;
		public const float GALAXY_THINNESS			= 2f;
		public const float GALAXY_CONCENTRATION		= 1f;
		public const float GALAXY_RADIAL_SPEED_POW	= 0f;

		public const bool GRAVITY_SUPERNOVA_ENABLE = true;
		public const float GRAVITY_CRITICAL_MASS = 4000f;
		public static readonly float GRAVITY_EJECTA_PARTICLE_MASS = 0.1f;
		public const float GRAVITY_EJECTA_SPEED = 3E-3f;

		public const bool GRAVITY_BLACK_HOLE_ENABLE = false;
		public const float GRAVITY_BLACKHOLE_THRESHOLD_RATIO = 1.5f;

		public const float GRAVITY_STARTING_SPEED_MAX_GROUP				= 0.2E-3f;
		public const float GRAVITY_STARTING_SPEED_MAX_GROUP_RAND		= 0.1E-3f;
		public const float GRAVITY_STARTING_SPEED_MAX_INTRAGROUP		= 0.5E-3f;
		public const float GRAVITY_STARTING_SPEED_MAX_INTRAGROUP_RAND	= 0.1E-3f;
		#endregion Gravity

		#region Aux
		public const float TARGET_FPS_DEFAULT = 30f;
		public const int PERF_WARN_MS = 2000;
		public const int AUTOSCALE_INTERVAL_MS = -1;
		public const float AUTOSCALE_STRENGTH = 0.25f;
		public const float AUTOSCALE_DIFF_THRESH = 0f;
		public const int PERF_GRAPH_REFRESH_MS = 250;

		public const int NUMBER_ACCURACY = 2;
		public const int NUMBER_SPACING = 5;
		public const float PERF_SMA_ALPHA = 0.05f;

		public const int GRAPH_HEIGHT = 8;//at least 2
		public const int PERF_GRAPH_DEFAULT_WIDTH = 32;
		public const int PERF_GRAPH_FRAMES_PER_COLUMN = 30;
		public const float PERF_GRAPH_PERCENTILE_CUTOFF = 25f;
		#endregion Aux

		#region
		public static readonly bool AUTOSCALER_ENABLE =
			!COLOR_USE_FIXED_BANDS && COLOR_ARRAY.Length > 1
			&& COLOR_METHOD != ParticleColoringMethod.Group
			&& COLOR_METHOD != ParticleColoringMethod.Random;

		public static readonly float INACCURCY_SQUARED = INACCURCY * INACCURCY;
		public static readonly float WORLD_DEATH_BOUND_RADIUS_SQUARED = WORLD_DEATH_BOUND_RADIUS*WORLD_DEATH_BOUND_RADIUS;

		public static readonly Vector<float> WORLD_LEFT = VectorFunctions.New(-WORLD_X_ASPECT * WORLD_SCALE / 2f, -WORLD_Y_ASPECT * WORLD_SCALE / 2f, -WORLD_Z_ASPECT * WORLD_SCALE / 2f);
		public static readonly Vector<float> WORLD_LEFT_INF = Vector.ConditionalSelect(VectorFunctions.DimensionSignals[DIM], WORLD_LEFT, new Vector<float>(float.NegativeInfinity));
		public static readonly Vector<float> WORLD_RIGHT = VectorFunctions.New(WORLD_X_ASPECT * WORLD_SCALE / 2f, WORLD_Y_ASPECT * WORLD_SCALE / 2f, WORLD_Z_ASPECT * WORLD_SCALE / 2f);
		public static readonly Vector<float> WORLD_RIGHT_INF = Vector.ConditionalSelect(VectorFunctions.DimensionSignals[DIM], WORLD_RIGHT, new Vector<float>(float.PositiveInfinity));
		public static readonly Vector<float> WORLD_SIZE = WORLD_RIGHT - WORLD_LEFT;
		#endregion
	}
}