using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Generic.Extensions;
using Generic.Vectors;

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

		protected override void InteractAll(BoidQuadTree tree) {
			Parallel.ForEach(
				tree.Leaves,
				Parameters.MulithreadedOptions,
				leaf => {
					VectorIncrementalAverage center = new(), direction = new();
					double[] repulsion = new double[Parameters.DIM];

					double[] awayVector;
					double dist, separationDist, cohesionDist;
					foreach (Boid b1 in leaf.NodeElements) {
						foreach (Boid b2 in leaf.GetNeighbors().Without(b2 => b1.ID == b2.ID).Take(this.InteractionLimit)) {
							if (b1.IsPredator == b2.IsPredator) {
								if (b1.GroupID == b2.GroupID) {
									separationDist = b1.SeparationDist;
									cohesionDist = b1.CohesionDist;
								} else {
									separationDist = b1.FlockSeparation;
									cohesionDist = double.PositiveInfinity;
								}
							} else if (b1.IsPredator) {//chase
								separationDist = 0d;
								cohesionDist = double.PositiveInfinity;
							} else {//flee
								separationDist = double.PositiveInfinity;
								cohesionDist = double.PositiveInfinity;
							}

							awayVector = b1.LiveCoordinates.Subtract(b2.LiveCoordinates);
							dist = awayVector.Magnitude();

							if ((b1.Vision < 0 || b1.Vision <= dist))
								if (dist < Parameters.WORLD_EPSILON || b1.FoV < 0 || b1.FoV >= b1.LiveCoordinates.AngleTo_FullRange(b2.LiveCoordinates))
									if (dist < cohesionDist)
										if (dist < separationDist)
											if (dist <= Parameters.WORLD_EPSILON)
												repulsion = repulsion.Add(awayVector.Multiply(1d));
											else repulsion = repulsion.Add(awayVector.Multiply(1d - dist/separationDist));
										else direction.Update(b2.Velocity);
									else center.Update(b2.LiveCoordinates);
						}
						
						b1.NetForce = b1.NetForce
							.Add(repulsion.Multiply(b1.RepulsionWeight))
							.Add(center.NumUpdates > 0 ? center.Current.Subtract(b1.LiveCoordinates).Normalize().Multiply(b1.CohesionWeight) : new double[Parameters.DIM])
							.Add(direction.NumUpdates > 0 && direction.Current.Magnitude() > Parameters.WORLD_EPSILON ? direction.Current.Subtract(b1.Velocity).Multiply(b1.AlignmentWeight) : new double[Parameters.DIM]);
			}});
		}
	}
}