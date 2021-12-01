using System;
using System.Linq;
using System.Threading.Tasks;

namespace ParticleSimulator.Simulation.Boids {
	public class BoidSimulator : AParticleSimulator<Boid, BoidQuadTree, Flock> {
		public BoidSimulator() : base() { }

		protected override int InteractionLimit => Parameters.BOIDS_DESIRED_INTERACTION_NEIGHBORS > 0 ? Parameters.BOIDS_DESIRED_INTERACTION_NEIGHBORS : int.MaxValue;

		public override ConsoleColor ChooseGroupColor(AParticle[] others) {
			return others.Cast<Boid>().Any(p => p.IsPredator)
				? Parameters.BOIDS_PREDATOR_COLOR
				: base.ChooseGroupColor(others);
		}
		
		protected override Flock NewParticleGroup() { return new Flock(); }
		protected override BoidQuadTree NewTree(double[] leftCorner, double[] rightCorner) { return new(leftCorner, rightCorner); }

		protected override double GetParticleWeight(Boid particle) { return 1d; }

		protected override void InteractAll(BoidQuadTree tree) {
			Parallel.ForEach(
				tree.Leaves,
				Parameters.MulithreadedOptions,
				leaf => {
					
			});
		}
	}
}