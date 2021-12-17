using System.Collections.Generic;
using System.Linq;
using Generic.Models;
using Generic.Vectors;

namespace ParticleSimulator.Simulation.Boids {
	//public class BoidSimulator : AParticleSimulator<Boid> {
	//	public BoidSimulator() : base() { }
		
	//	public override bool EnableCollisions => false;
	//	public override double WorldBounceWeight => Parameters.BOIDS_WORLD_BOUNCE_WEIGHT;
	//	public int InteractionLimit => Parameters.BOIDS_DESIRED_INTERACTION_NEIGHBORS < 0 ? int.MaxValue : Parameters.BOIDS_DESIRED_INTERACTION_NEIGHBORS;
		
	//	protected override Flock NewParticleGroup() { return new Flock(); }
	//	protected override ATree<Boid> NewTree(double[] leftCorner, double[] rightCorner) { return new NearFieldQuadTree<Boid>(leftCorner, rightCorner); }

	//	protected override IEnumerable<Boid> Refresh(ATree<Boid> leaf) {
	//		Boid[] neighbors = leaf.GetNeighbors()
	//			.Where(other => other.Enabled)
	//			.Take(this.InteractionLimit + 1)
	//			.Cast<Boid>()
	//			.ToArray();
	//		foreach (Boid p in leaf.NodeElements.Where(particle => particle.Enabled).Cast<Boid>())
	//			p.Acceleration = p.Acceleration.Add(
	//				p.ComputeAcceleration(neighbors));

	//		for (int i = 0; i < this.Particles.Length; i++) {
	//			this.Particles[i].ApplyTimeStep(
	//				this.Particles[i].Acceleration
	//					.Add(this.Particles[i].CollisionAcceleration));
	//			this.Particles[i].Acceleration = new double[Parameters.DIM];
	//			this.Particles[i].CollisionAcceleration = new double[Parameters.DIM];
	//			yield return this.Particles[i];
	//		}
	//	}
	//}
}