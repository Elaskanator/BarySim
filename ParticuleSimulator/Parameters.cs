using System;
using System.Linq;
using System.Numerics;
using Generic.Vectors;
using ParticleSimulator.Rendering;
using ParticleSimulator.Rendering.SystemConsole;

namespace ParticleSimulator {
	//sentinel value is usually -1 for unlimited or to disable the feature
	public static class Parameters {
		#region Primary
		//global
		public const int DIM						= 3;
		public const int RANDOM_SEED				= 1;
		//evaluation
		public const float TARGET_FPS				= 30f;
		public const int FRAME_LIMIT				= -1;
		public const bool VSYNC						= false;
		//rendering size (using top and bottom halves of each character to get double the verticle resolution)
		public static readonly int WINDOW_WIDTH		= Console.LargestWindowHeight*2;
		public static readonly int WINDOW_HEIGHT	= Console.LargestWindowHeight;
		public const int SUPERSAMPLING				= 2;
		public const float PIXEL_ROUNDOFF			= 0.5f;
		//camera
		public const float WORLD_SCALE				= 400f;
		public const float ZOOM_SCALE				= 1f;
		public const bool AUTOFOCUS_DEFAULT			= true;
		public const float ROT_DEG_PER_FRAME		= 0.33333f;
		//particle count
		public const int PARTICLES_GROUP_COUNT		= 1;
		public const int PARTICLES_GROUP_MIN		= 1;
		public const int PARTICLES_GROUP_MAX		= 25000;
		public const float PARTICLES_GROUP_SIZE_POW	= 0f;//0 for max size
		//particle features
		public const float TIME_SCALE				= 1f;
		public const bool COLLISION_ENABLE			= true;
		public const float DRAG_CONSTANT			= 0f;
		public const bool MERGE_ENABLE				= true;
		public const float MERGE_ENGULF_RATIO		= 0.8f;
		//world
		public const float WORLD_PADDING_PCT		= 0f;
		public const float WORLD_PRUNE_RADII		= 10f;
		public const bool WORLD_WRAPPING			= false;
		public const bool WORLD_BOUNCING			= false;
		public const float WORLD_X_ASPECT			= 1f;
		public const float WORLD_Y_ASPECT			= 1f;
		public const float WORLD_Z_ASPECT			= 1f;
		//accuracy
		public const float ACCURACY_CRITERION		= 2f;//Barnes-Hut condition of approximating a node (smaller = more accurate)
		public const float NODE_EPSILON				= 4f;//nodes too close must be directly evaluated (helps prevent tight groupings not merging)
		public const float PRECISION_EPSILON		= 1E-4f;//minimum distance to support
		public const int TREE_LEAF_CAPACITY			= 8;//degrades integrity of approximation check
		//parallelism
		public const int PRECALCULATION_LIMIT		= 1;//how many calculations ahead steps can work
		public const bool SYNC_SIMULATION			= true;//controls synchronization of rendering to simulation (e.g. faster rotation)
		public const int SIMULATION_SKIPS			= 0;//refresh the simulation multiple times between renders
		public const int TREE_BATCH_SIZE			= 1800;//tree preparation and particle evaluation parallelism
		public const double TREE_BATCH_SLACK		= 0.1d;//relative overage allowed without further refining the tree
		//render coloring
		public const ParticleColoringMethod COLORING= ParticleColoringMethod.Luminosity;
		public static readonly ConsoleColor[] COLORS= ColoringScales.StarColors;
		public const bool COLOR_USE_FIXED_BANDS		= true;
		public static readonly float[] FIXED_BANDS	= Enumerable.Range(0, COLORS.Length).Select(i => (float)(1 << i)).ToArray();
		//render coloring autoscaling
		public const int AUTOSCALE_INTERVAL_MS		= -1;
		public const float AUTOSCALE_STRENGTH		= 0.25f;
		public const float AUTOSCALE_FIXED_MIN		= -1f;
		public const float AUTOSCALE_FIXED_MAX		= -1f;
		public const bool AUTOSCALE_PERCENTILE		= false;
		public const float AUTOSCALE_CUTOFF_PCT		= 0f;
		public const float AUTOSCALE_MIN_STEP		= 1f;
		public const float AUTOSCALE_DIFF_THRESH	= 0f;
		//render exporting
		//public const bool EXPORT_FRAMES = false;//TODO
		//public const string EXPORT_DIR = null;//TODO
		#endregion Primary

		#region Gravity
		public const float GRAVITATIONAL_CONSTANT	= 1E-4f;
		//TODO add electrostatic force

		public const float MASS_SCALAR				= 1f;
		public const float MASS_RADIAL_DENSITY		= 1f;
		public const float MASS_LUMINOSITY_SCALAR	= 1f;
		public const float MASS_LUMINOSITY_POW		= 1f;

		public const float GALAXY_RADIUS			= 150f;
		public const float GALAXY_THINNESS			= 3f;
		public const float GALAXY_CONCENTRATION		= 1.5f;

		public const float GALAXY_SPEED_ANGULAR		= 0.0f;
		public const float GALAXY_SPEED_RAND		= 0.0f;
		public const float GALAXY_SPIN_ANGULAR		= 0.07f;
		public const float GALAXY_SPIN_RAND			= 0.1f;
		public const float GALAXY_SPIN_POW			= 0.5f;

		public const bool SUPERNOVA_ENABLE			= false;
		public const float SUPERNOVA_CRITICAL_MASS	= 10000f;
		public const float SUPERNOVA_EJECTA_MASS	= 1f;
		public const float SUPERNOVA_EJECTA_SPEED	= 0.4f;
		public const float SUPERNOVA_RADIUS_SCALAR	= 1f;

		public const bool BLACKHOLE_ENABLE			= false;
		public const float BLACKHOLE_THRESHOLD		= 1.75f;
		#endregion Gravity

		#region Monitoring
		public const float MON_FPS_DEFAULT			= 30f;
		public const int MON_WARN_MS				= 2500;

		public const int MON_NUMBER_ACCURACY		= 2;
		public const int MON_NUMBER_SPACING			= 5;
		public const float MON_SMA_ALPHA			= 0.05f;
		
		public const int MON_GRAPH_REFRESH_MS		= 250;
		public const int MON_GRAPH_HEIGHT			= 8;//at least 2
		public const int MON_GRAPH_DEFAULT_WIDTH	= 32;
		public const int MON_GRAPH_COLUMN_FRAMES	= 30;
		public const float MON_GRAPH_PERC_CUTOFF	= 25f;
		#endregion Monitoring

		#region Precomputed constants
		public static readonly bool AUTOSCALER_ENABLE =
			!COLOR_USE_FIXED_BANDS && COLORS.Length > 1
			&& COLORING != ParticleColoringMethod.Group
			&& COLORING != ParticleColoringMethod.Random;
		public static readonly float WORLD_ROTATION_RAD_PER_FRAME = MathF.PI * ROT_DEG_PER_FRAME / 180f;

		public static readonly float INACCURCY_SQUARED = ACCURACY_CRITERION * ACCURACY_CRITERION;
		public static readonly float WORLD_PRUNE_RADII_SQUARED = WORLD_PRUNE_RADII*WORLD_PRUNE_RADII;
		public static readonly float TIME_SCALE2 = TIME_SCALE*TIME_SCALE;

		public static readonly Vector<float> WORLD_LEFT = VectorFunctions.New(-WORLD_X_ASPECT * WORLD_SCALE / 2f, -WORLD_Y_ASPECT * WORLD_SCALE / 2f, -WORLD_Z_ASPECT * WORLD_SCALE / 2f);
		public static readonly Vector<float> WORLD_LEFT_INF = Vector.ConditionalSelect(VectorFunctions.DimensionSignals[DIM], WORLD_LEFT, new Vector<float>(float.NegativeInfinity));
		public static readonly Vector<float> WORLD_RIGHT = VectorFunctions.New(WORLD_X_ASPECT * WORLD_SCALE / 2f, WORLD_Y_ASPECT * WORLD_SCALE / 2f, WORLD_Z_ASPECT * WORLD_SCALE / 2f);
		public static readonly Vector<float> WORLD_RIGHT_INF = Vector.ConditionalSelect(VectorFunctions.DimensionSignals[DIM], WORLD_RIGHT, new Vector<float>(float.PositiveInfinity));
		public static readonly Vector<float> WORLD_SIZE = WORLD_RIGHT - WORLD_LEFT;
		#endregion Precomputed constants
	}
}