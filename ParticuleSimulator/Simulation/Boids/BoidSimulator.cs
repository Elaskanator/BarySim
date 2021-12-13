using System;
using System.Linq;
using System.Threading.Tasks;
using Generic.Extensions;
using Generic.Vectors;

namespace ParticleSimulator.Simulation.Boids {
	public class BoidSimulator : AParticleSimulator<Boid, BoidQuadTree, Flock> {
		public BoidSimulator() : base() { }
		
		public override double WorldBounceWeight => Parameters.BOIDS_WORLD_BOUNCE_WEIGHT;
		public override int InteractionLimit => Parameters.BOIDS_DESIRED_INTERACTION_NEIGHBORS < 0 ? int.MaxValue : Parameters.BOIDS_DESIRED_INTERACTION_NEIGHBORS;

		public override ConsoleColor ChooseGroupColor(AParticle[] others) {
			return others.Cast<Boid>().Any(p => p.IsPredator)
				? Parameters.BOIDS_PREDATOR_COLOR
				: base.ChooseGroupColor(others);
		}
		
		protected override Flock NewParticleGroup() { return new Flock(); }
		protected override BoidQuadTree NewTree(double[] leftCorner, double[] rightCorner) { return new(leftCorner, rightCorner); }

		protected override void InteractAll(BoidQuadTree tree) {
			Parallel.ForEach(
				tree.Leaves,
				Parameters.MulithreadedOptions,
				leaf => {
					foreach (Boid boid in leaf.NodeElements.Where(boid => boid.IsActive))
						boid.NetForce = boid.NetForce.Add(
							boid.ComputeInteractionForce(
								leaf.GetNeighbors()
									.Where(other => other.IsActive)
									.Without(other => boid.ID == other.ID)
									.Take(this.InteractionLimit)));

			});
		}
	}
}