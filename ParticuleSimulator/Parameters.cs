﻿using System;
using Generic.Models;

namespace ParticleSimulator {
	public static class Parameters {
		//NOTE sentinel value is -1 (means default)
		public const bool LEGEND_ENABLE = true;
		public const bool DENSITY_AUTOSCALE_ENABLE = true;
		public const bool PERF_ENABLE = true;
		public const bool PERF_STATS_ENABLE = false;
		public const bool PERF_GRAPH_ENABLE = false;

		public const double TARGET_FPS = -1;
		public const double MAX_FPS = 60;

		public const bool SYNC_SIMULATION = true;
		public const bool SYNC_TREE_REFRESH = false;
		public const int SIMULATION_SUBFRAME_MULTIPLE = 1;

		public const int PRECALCULATION_LIMIT = 1;//no benefit to larger values than one

		#region Particles
		public const int NUM_PARTICLES_PER_GROUP = 3000;
		public const int NUM_PARTICLE_GROUPS = 1;

		public const double DEFAULT_SEPARATION = 4;
		public static readonly float DEFAULT_SEPARATION_FLOAT = (float)DEFAULT_SEPARATION;
		#endregion Particles

		#region Sizes
		public const int WINDOW_WIDTH = 160;//160 x 80 max, practical min width of graph width if that is enabled
		public const int WINDOW_HEIGHT = 80;
		public const double WORLD_SCALE = 400d;
		public const double WORLD_ASPECT_RATIO = 1d;

		public const int GRAPH_WIDTH = -1;
		public const int GRAPH_HEIGHT = 7;//at least 2
		#endregion Sizes

		#region Boids

		public const int DESIRED_NEIGHBORS = 8;
		public const bool ENABLE_COHESION = true;
		public const bool ENABLE_ALIGNMENT = true;
		public const bool ENABLE_SEPARATION = true;
		
		public const double SPEED_DECAY = 0.1;//used as E^val
		public static readonly float SPEED_DECAY_FLOAT = (float)SPEED_DECAY;

		public const double SEPARATION_WEIGHT = 10d;
		public static readonly float SEPARATION_WEIGHT_FLOAT = (float)SEPARATION_WEIGHT;
		public const double ALIGNMENT_WEIGHT = 1d;
		public static readonly float ALIGNMENT_WEIGHT_FLOAT = (float)ALIGNMENT_WEIGHT;
		public const double COHESION_WEIGHT = 1d;
		public static readonly float COHESION_WEIGHT_FLOAT = (float)COHESION_WEIGHT;

		public const double MAX_ACCELERATION = 0.1;
		public static readonly float MAX_ACCELERATION_FLOAT = (float)MAX_ACCELERATION;
		public const double MAX_SPEED = 1;
		public static readonly float MAX_SPEED_FLOAT = (float)MAX_SPEED;
		public const double MAX_STARTING_SPEED = 1;
		public static readonly float MAX_STARTING_SPEED_FLOAT = (float)MAX_STARTING_SPEED;

		public const double MAX_IMPULSE_COHESION = 1;
		public static readonly float MAX_IMPULSE_COHESION_FLOAT = (float)MAX_IMPULSE_COHESION;
		public const double MAX_IMPULSE_ALIGNMENT = 1;
		public static readonly float MAX_IMPULSE_ALIGNMENT_FLOAT = (float)MAX_IMPULSE_ALIGNMENT;
		public const double MAX_IMPULSE_SEPARATION = 10;
		public static readonly float MAX_IMPULSE_SEPARATION_FLOAT = (float)MAX_IMPULSE_SEPARATION;
		#endregion Boids

		#region Gravity
		public const double DEFAULT_MIN_MASS = 1;
		public const double DEFAULT_MAX_MASS = 1;
		#endregion Gravity

		#region Aux
		public const char CHAR_TOP = '\u2580';//▀
		public const char CHAR_BOTTOM = '\u2584';//▄
		public const char CHAR_BOTH = '\u2588';//█

		public const double TARGET_FPS_DEFAULT = 30d;
		public const int PERF_WARN_MS = 2000;
		public const int NUMBER_ACCURACY = 2;
		public const int NUMBER_SPACING = 5;

		public const int PERF_GRAPH_FRAMES_PER_COLUMN = 10;
		public const double PERF_SMA_ALPHA = 0.1d;
		public const double PERF_GRA_SMA_ALPHA = 0.2d;

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
		
		public static readonly double[] DOMAIN_DOUBLE = VectorFunctions.Multiply(new double[] { WORLD_ASPECT_RATIO, 1d }, WORLD_SCALE);
		public static readonly float[] DOMAIN_FLOAT = VectorFunctions.Multiply(new float[] { (float)WORLD_ASPECT_RATIO, 1f }, (float)WORLD_SCALE);
	}
}