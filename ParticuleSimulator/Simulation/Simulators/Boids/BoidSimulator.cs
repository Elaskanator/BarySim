using System;
using System.Linq;
using System.Threading.Tasks;
using Generic.Extensions;
using Generic.Models;
using Generic.Vectors;

namespace ParticleSimulator.Simulation.Boids {
	public class BoidSimulator : AParticleSimulator {
		public BoidSimulator() : base() {
			throw new NotImplementedException();
		}
		
		public override double WorldBounceWeight => Parameters.BOIDS_WORLD_BOUNCE_WEIGHT;
		public override int InteractionLimit => Parameters.BOIDS_DESIRED_INTERACTION_NEIGHBORS < 0 ? int.MaxValue : Parameters.BOIDS_DESIRED_INTERACTION_NEIGHBORS;
		
		protected override Flock NewParticleGroup() { return new Flock(); }

		public override ConsoleColor ChooseGroupColor(AClassicalParticle[] others) {
			return others.Cast<Boid>().Any(p => p.IsPredator)
				? Parameters.BOIDS_PREDATOR_COLOR
				: base.ChooseGroupColor(others);
		}

		//protected override void Refresh(QuadTree<AClassicalParticle> tree) {
		//	Parallel.ForEach(
		//		tree.Leaves.Where(n => n.NumMembers > 0),
		//		Parameters.MulithreadedOptions,
		//		leaf => {
		//			Boid[] neighbors = leaf.GetNeighbors()
		//				.Where(other => other.IsAlive)
		//				.Take(this.InteractionLimit + 1)
		//				.Cast<Boid>()
		//				.ToArray();
		//			foreach (Boid p in leaf.NodeElements.Where(particle => particle.IsAlive).Cast<Boid>())
		//				p.Momentum = p.Momentum.Add(
		//					p.ComputeInteractionForce(neighbors));

		//	});
		//}
	}
}