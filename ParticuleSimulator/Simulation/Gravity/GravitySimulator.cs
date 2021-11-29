using System;
using System.Linq;
using System.Threading.Tasks;
using Generic.Extensions;
using Generic.Models.Vectors;

namespace ParticleSimulator.Simulation.Gravity {
	public class GravitySimulator : AParticleSimulator<CelestialBody, BaryonQuadTree, Clustering> {
		protected override bool UseMaxDensity => true;

		protected override void InteractAll(BaryonQuadTree tree) {
			Parallel.ForEach(
				tree.Leaves,
				Parameters.MulithreadedOptions,
				leaf => {
					CelestialBody[] particles = leaf.NodeElements.ToArray();
					if (particles.Length > 0) {
						Tuple<BaryonQuadTree[], BaryonQuadTree[]> baryonNeighbors = leaf.GetNeighborhoodNodes(Parameters.GRAVITY_NEIGHBORHOOD_FILTER);
						double[] baryonFarImpulse =
							baryonNeighbors.Item2
								.Aggregate(new double[Parameters.DIM], (agg, n) =>
									agg.Add(CelestialBody.ComputeInteraction(
										leaf.Barycenter.Current, 1d, n.Barycenter.Current, n.TotalMass)));
						double[] neighborImpulse;
						double dist;
						for (int i = 0; i < particles.Length; i++) {
							if (particles[i].IsActive) {
								if (particles[i].LiveCoordinates.Any((c, i) =>
								c < -Parameters.GRAVITY_DEATH_BOUND_CNT*Parameters.DOMAIN[i]
								|| c > Parameters.DOMAIN[i] *(1d + Parameters.GRAVITY_DEATH_BOUND_CNT))) {
									particles[i].IsActive = false;
								} else {
									particles[i].Acceleration = particles[i].Acceleration
										.Add(baryonFarImpulse)
										.Add(baryonNeighbors.Item1.Aggregate(new double[Parameters.DIM], (agg, n) =>
											agg.Add(CelestialBody.ComputeInteraction(particles[i].LiveCoordinates, 1d, n.Barycenter.Current, n.TotalMass))));

									for (int j = i + 1; j < particles.Length; j++) {
										if (particles[j].IsActive) {
											dist = particles[i].LiveCoordinates.Distance(particles[j].LiveCoordinates);
											if (dist >= particles[i].Radius + particles[j].Radius) {
												neighborImpulse = CelestialBody.ComputeInteraction(
													particles[i].LiveCoordinates,
													particles[i].Mass,
													particles[j].LiveCoordinates,
													particles[j].Mass);

												particles[i].Acceleration = particles[i].Acceleration.Add(
													neighborImpulse.Divide(particles[i].Mass));
												particles[j].Acceleration = particles[j].Acceleration.Subtract(
													neighborImpulse.Divide(particles[j].Mass));
											} else {//collision
												particles[i].LiveCoordinates =
													particles[i].LiveCoordinates.Multiply(particles[i].Mass)
													.Add(particles[j].LiveCoordinates.Multiply(particles[j].Mass))
													.Divide(particles[i].Mass + particles[j].Mass);
												particles[i].Velocity =
													particles[i].Velocity.Multiply(particles[i].Mass)
													.Add(particles[j].Velocity.Multiply(particles[j].Mass))
													.Divide(particles[i].Mass + particles[j].Mass);

												particles[j].IsActive = false;
												particles[i].Mass += particles[j].Mass;
			}}}}}}}});
		}
		
		protected override Clustering NewParticleGroup() { return new(); }
		protected override BaryonQuadTree NewTree(double[] leftCorner, double[] rightCorner) { return new(leftCorner, rightCorner); }
		protected override double GetParticleWeight(CelestialBody particle) { return particle.Mass; }
	}
}