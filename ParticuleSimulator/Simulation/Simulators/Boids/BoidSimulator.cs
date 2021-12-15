using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Generic.Extensions;
using Generic.Models;
using Generic.Vectors;

namespace ParticleSimulator.Simulation.Boids {
	public class BoidSimulator : AParticleSimulator<Boid> {
		public BoidSimulator() : base() { }
		
		public override bool EnableCollisions => false;
		public override double WorldBounceWeight => Parameters.BOIDS_WORLD_BOUNCE_WEIGHT;
		public int InteractionLimit => Parameters.BOIDS_DESIRED_INTERACTION_NEIGHBORS < 0 ? int.MaxValue : Parameters.BOIDS_DESIRED_INTERACTION_NEIGHBORS;
		
		protected override Flock NewParticleGroup() { return new Flock(); }
		protected override ATree<Boid> NewTree(double[] leftCorner, double[] rightCorner) { return new NearFieldQuadTree<Boid>(leftCorner, rightCorner); }

		protected override void Refresh(QuadTree<Boid> tree) {
			Parallel.ForEach(
				tree.Leaves.Where(n => n.NumMembers > 0),
				Parameters.MulithreadedOptions,
				leaf => {
					Boid[] neighbors = leaf.GetNeighbors()
						.Where(other => other.Enabled)
						.Take(this.InteractionLimit + 1)
						.Cast<Boid>()
						.ToArray();
					foreach (Boid p in leaf.NodeElements.Where(particle => particle.Enabled).Cast<Boid>())
						p.Acceleration = p.Acceleration.Add(
							p.ComputeAcceleration(neighbors));

			});
			for (int i = 0; i < this.EnabledParticles.Length; i++) {
				this.EnabledParticles[i].ApplyTimeStep(
					this.EnabledParticles[i].Acceleration
					//	.Add(this.EnabledParticles[i].CollisionAcceleration)
					,Parameters.TIME_SCALE);
				this.EnabledParticles[i].Acceleration = new double[Parameters.DIM];
				//this.EnabledParticles[i].CollisionAcceleration = new double[Parameters.DIM];
			}
		}

		public override ConsoleColor ChooseGroupColor(IEnumerable<Boid> others) {
			return others.Any(p => p.IsPredator)
				? Parameters.BOIDS_PREDATOR_COLOR
				: base.ChooseGroupColor(others.AsEnumerable());
		}
	}
}