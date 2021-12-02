using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Generic.Extensions;
using Generic.Models.Vectors;

namespace ParticleSimulator.Simulation.Gravity {
	public class GravitySimulator : AParticleSimulator<CelestialBody, BaryonQuadTree, Clustering> {
		protected override void InteractAll(BaryonQuadTree tree) {
			Parallel.ForEach(
				tree.Leaves,
				Parameters.MulithreadedOptions,
				leaf => {
					CelestialBody[] particles = leaf.NodeElements.ToArray();
					if (particles.Length > 0) {
						double[] toOther;
						double distance;

						List<BaryonQuadTree> nearNodes = new(), farNodes = new();
						foreach (BaryonQuadTree n in leaf.GetNeighborhoodNodes(Parameters.GRAVITY_NEIGHBORHOOD_FILTER))
							if (_inRangeTest(leaf, n))
								nearNodes.Add(n);
							else farNodes.Add(n);

						Tuple<BaryonQuadTree[], BaryonQuadTree[]> temp, splitInteractionNodes = new(Array.Empty<BaryonQuadTree>(), Array.Empty<BaryonQuadTree>());
						for (int i = 0; i < nearNodes.Count; i++) {
							temp = nearNodes[i].RecursiveFilter(n => _inRangeTest(n, leaf));
							splitInteractionNodes = new(
								splitInteractionNodes.Item1.Concat(temp.Item1).ToArray(),
								splitInteractionNodes.Item2.Concat(temp.Item2).ToArray());
						}
						BaryonQuadTree[]
							nearNodes2 = splitInteractionNodes.Item1.ToArray(),
							farNodes2 = splitInteractionNodes.Item2.Concat(farNodes).ToArray();
						
						double[] baryonFarImpulse = farNodes2.Aggregate(new double[Parameters.DIM], (agg, other) => {
							toOther = other.Barycenter.Current.Subtract(leaf.Barycenter.Current);
							distance = toOther.Magnitude();
							return agg.Add(toOther.Multiply(//third division normalizes
								Parameters.GRAVITATIONAL_CONSTANT * other.Barycenter.TotalWeight / distance / distance / distance));
						});

						double[] neighborImpulse;
						for (int i = 0; i < particles.Length; i++) {
							if (particles[i].IsActive) {
								if (particles[i].LiveCoordinates.Any((c, i) =>
								c < -Parameters.GRAVITY_DEATH_BOUND_CNT*Parameters.DOMAIN[i]
								|| c > Parameters.DOMAIN[i] *(1d + Parameters.GRAVITY_DEATH_BOUND_CNT))) {
									particles[i].IsActive = false;
								} else {
									particles[i].NetForce = particles[i].NetForce.Add(baryonFarImpulse.Multiply(particles[i].Mass));

									for (int j = i + 1; j < particles.Length; j++) {//symmetric interaction
										neighborImpulse = particles[i].ComputeInteractionForce(particles[j]);
										particles[i].NetForce = particles[i].NetForce.Add(neighborImpulse);
										particles[j].NetForce = particles[j].NetForce.Subtract(neighborImpulse);
									}

									for (int b = 0; b < nearNodes2.Length; b++)
										foreach (CelestialBody p in nearNodes2[b].AllElements)//asymmetric interaction
											particles[i].NetForce = particles[i].NetForce.Add(particles[i].ComputeInteractionForce(p));
			}}}}});
		}

		private static readonly Func<BaryonQuadTree, BaryonQuadTree, bool> _inRangeTest = (a, b) =>
			a.Barycenter.Current.Distance(b.Barycenter.Current) <=
				Parameters.GRAVITY_NEIGHBORHOOD_RADIUS_MULTIPLE*CelestialBody.RadiusOfMass(a.Barycenter.TotalWeight)
				+ Parameters.GRAVITY_NEIGHBORHOOD_RADIUS_MULTIPLE*CelestialBody.RadiusOfMass(b.Barycenter.TotalWeight);

		protected override Clustering NewParticleGroup() { return new(); }
		protected override BaryonQuadTree NewTree(double[] leftCorner, double[] rightCorner) { return new(leftCorner, rightCorner); }
		protected override double GetParticleWeight(CelestialBody particle) { return particle.Mass; }
	}
}