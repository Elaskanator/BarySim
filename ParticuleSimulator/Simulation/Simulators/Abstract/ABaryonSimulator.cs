using System;
using System.Collections.Generic;
using System.Linq;
using Generic.Models;
using Generic.Vectors;

namespace ParticleSimulator.Simulation {
	public abstract class ABaryonSimulator<TParticle> : ASimulator<TParticle, MagicTree<TParticle>>
	where TParticle : ABaryonParticle<TParticle> {
		public ABaryonSimulator(params ABaryonForce<TParticle>[] forces) {
			this.Forces = forces;
		}

		public override bool EnableFarfield => true;
		public readonly ABaryonForce<TParticle>[] Forces;

		//protected override IEnumerable<TParticle> Refresh(ATree<TParticle> leaf) {
		//	//recursively discover near and far nodes based on distance
		//	List<ATree<TParticle>> nearNodes = new(), farNodes = new();
		//	foreach (ATree<TParticle> n in leaf.GetNeighborhoodNodes(Parameters.FARFIELD_NEIGHBORHOOD_FILTER_DEPTH))
		//		if (((FarFieldQuadTree<TParticle>)n).BaryCenter_Position.Coordinates.Distance(((FarFieldQuadTree<TParticle>)leaf).BaryCenter_Position.Coordinates) <= Parameters.FARFIELD_THRESHOLD_DIST)
		//			nearNodes.Add(n);
		//		else farNodes.Add(n);

		//	//further refine to leaves
		//	Tuple<ATree<TParticle>[], ATree<TParticle>[]> temp, splitInteractionNodes = new(Array.Empty<ATree<TParticle>>(), Array.Empty<ATree<TParticle>>());
		//	for (int i = 0; i < nearNodes.Count; i++) {
		//		temp = nearNodes[i].RecursiveFilter(n => ((FarFieldQuadTree<TParticle>)n).BaryCenter_Position.Coordinates.Distance(((FarFieldQuadTree<TParticle>)leaf).BaryCenter_Position.Coordinates) <= Parameters.FARFIELD_THRESHOLD_DIST);
		//		splitInteractionNodes = new(
		//			splitInteractionNodes.Item1.Concat(temp.Item1).ToArray(),
		//			splitInteractionNodes.Item2.Concat(temp.Item2).ToArray());
		//	}

		//	return this.AdaptiveTimeStepIntegration(
		//		leaf.NodeElements,
		//		leaf,
		//		splitInteractionNodes.Item1,
		//		splitInteractionNodes.Item2.Concat(farNodes));
		//}

		//private IEnumerable<TParticle> AdaptiveTimeStepIntegration(IEnumerable<TParticle> particles, ATree<TParticle> leaf, ATree<TParticle>[] nearfieldLeaves, IEnumerable<ATree<TParticle>> farfieldNodes) {
		//	double[] baryonFarImpulseAsym = farfieldNodes.Aggregate(new double[Parameters.DIM], (totalImpuse, other) =>
		//		totalImpuse.Add(
		//			this.Forces.Aggregate(new double[Parameters.DIM], (impulse, force) =>
		//				impulse.Add(force.ComputeAsymmetricImpulse((FarFieldQuadTree<TParticle>)leaf, (FarFieldQuadTree<TParticle>)other)))));

		//	double remainingTimeStep = 1d,
		//		timeStep,
		//		delta,
		//		largestDelta;
		//	List<TParticle> activeParticles = new(particles), newParticles = new();
		//	while (remainingTimeStep >= Parameters.ADAPTIVE_TIME_GRANULARITY) {
		//		largestDelta = this.ComputeImpulses(activeParticles, leaf, nearfieldLeaves) * remainingTimeStep * Parameters.TIME_SCALE;
		//		delta = this.HandleCollisions(activeParticles, true);//inside of node only
		//		largestDelta = largestDelta > delta ? largestDelta : delta;

		//		timeStep =
		//			largestDelta > Parameters.ADAPTIVE_TIME_CRITERION
		//				&& remainingTimeStep >= 2d * Parameters.ADAPTIVE_TIME_GRANULARITY
		//				&& remainingTimeStep * Parameters.ADAPTIVE_TIME_CRITERION / largestDelta >= Parameters.ADAPTIVE_TIME_GRANULARITY
		//			? timeStep = remainingTimeStep * Parameters.ADAPTIVE_TIME_CRITERION / largestDelta
		//			: timeStep = remainingTimeStep;

		//		for (int i = 0; i < activeParticles.Count; i++) {
		//			if (activeParticles[i].Enabled) {
		//				activeParticles[i].FarfieldImpulse = baryonFarImpulseAsym.Multiply(activeParticles[i].Mass);
		//				activeParticles[i].CollisionImpulse = new double[Parameters.DIM];

		//				newParticles.AddRange(
		//					activeParticles[i].ApplyTimeStep(
		//						activeParticles[i].NearfieldImpulse
		//							.Add(activeParticles[i].FarfieldImpulse)
		//							.Divide(activeParticles[i].Mass),
		//						timeStep)
		//					?? Enumerable.Empty<TParticle>());
		//			}
		//		}

		//		remainingTimeStep -= timeStep;
		//		(activeParticles, newParticles) = (newParticles, activeParticles);
		//		newParticles.Clear();
		//	}
		//	return activeParticles;
		//}

		//private double ComputeImpulses(List<TParticle> particles, ATree<TParticle> leaf, ATree<TParticle>[] nearfieldLeaves) {
		//	double accelerationDelta, velocityDelta, distance,
		//		largestDelta = 0f;
		//	double[] toOther, impulse, totalImpulse;
		//	bool collision;
		//	for (int selfIdx = 0; selfIdx < particles.Count; selfIdx++) {
		//		if (particles[selfIdx].Enabled) {
		//			particles[selfIdx].NearfieldImpulse = new double[Parameters.DIM];

		//			for (int otherIdx = selfIdx + 1; otherIdx < particles.Count; otherIdx++) {//symmetric interaction (handshake optimization)
		//				if (particles[otherIdx].Enabled) {
		//					totalImpulse = new double[Parameters.DIM];
		//					toOther = particles[otherIdx].LiveCoordinates.Subtract(particles[selfIdx].LiveCoordinates);
		//					distance = toOther.Magnitude();

		//					for (int fIdx = 0; fIdx < this.Forces.Length; fIdx++) {
		//						impulse = this.Forces[fIdx].ComputeImpulse(distance, toOther, particles[selfIdx], particles[otherIdx], out collision);
		//						totalImpulse = totalImpulse.Add(impulse);
		//						if (collision)
		//							particles[selfIdx].NodeCollisions.Enqueue(particles[otherIdx]);
		//					}
		//					particles[selfIdx].NearfieldImpulse = particles[selfIdx].NearfieldImpulse.Add(totalImpulse);
		//					particles[otherIdx].NearfieldImpulse = particles[otherIdx].NearfieldImpulse.Subtract(totalImpulse);

		//					accelerationDelta = totalImpulse.Magnitude()
		//						/ (particles[selfIdx].Mass < particles[otherIdx].Mass ? particles[selfIdx].Mass : particles[otherIdx].Mass);
		//					largestDelta = accelerationDelta > largestDelta ? accelerationDelta : largestDelta;
					
		//					if (distance > Parameters.WORLD_EPSILON) {
		//						velocityDelta = particles[selfIdx].Velocity.Subtract(particles[otherIdx].Velocity).Magnitude() / distance;
		//						largestDelta = velocityDelta > largestDelta ? velocityDelta : largestDelta;
		//					}
		//				}
		//			}

		//			for (int b = 0; b < nearfieldLeaves.Length; b++) {//asymmetric interaction
		//				foreach (TParticle other in nearfieldLeaves[b].NodeElements) {
		//					if (other.Enabled) {
		//						totalImpulse = new double[Parameters.DIM];
		//						toOther = other.LiveCoordinates.Subtract(particles[selfIdx].LiveCoordinates);
		//						distance = toOther.Magnitude();

		//						for (int fIdx = 0; fIdx < this.Forces.Length; fIdx++) {
		//							impulse = this.Forces[fIdx].ComputeImpulse(distance, toOther, particles[selfIdx], other, out collision);
		//							totalImpulse = totalImpulse.Add(impulse);
		//							if (collision)
		//								particles[selfIdx].NeighborNodeCollisions.Enqueue(other);
		//						}
		//						particles[selfIdx].NearfieldImpulse = particles[selfIdx].NearfieldImpulse.Add(totalImpulse);
		//					}
		//				}
		//			}
		//		}
		//	}
		//	return largestDelta;
		//}
	}
}