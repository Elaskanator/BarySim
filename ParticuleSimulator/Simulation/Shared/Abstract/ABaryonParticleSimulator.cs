﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Generic.Models;
using Generic.Vectors;

namespace ParticleSimulator.Simulation {
	public abstract class ABaryonParticleSimulator<TParticle> : AParticleSimulator<TParticle>
	where TParticle : ABaryonParticle<TParticle> {
		public ABaryonParticleSimulator(params ABaryonForce<TParticle>[] forces) {
			this.Forces = forces;
		}

		public readonly ABaryonForce<TParticle>[] Forces;

		protected override void Refresh(QuadTree<TParticle> tree) {//modified Barnes-Hut Algorithm
			Parallel.ForEach(
				tree.Leaves.Where(n => n.NumMembers > 0),
				Parameters.MulithreadedOptions,
				leaf => {
					//recursively discover near and far nodes based on distance
					List<ATree<TParticle>> nearNodes = new(), farNodes = new();
					foreach (ATree<TParticle> n in leaf.GetNeighborhoodNodes(Parameters.FARFIELD_NEIGHBORHOOD_FILTER_DEPTH))
						if (((FarFieldQuadTree<TParticle>)n).BaryCenter_Position.Coordinates.Distance(((FarFieldQuadTree<TParticle>)leaf).BaryCenter_Position.Coordinates) <= Parameters.FARFIELD_THRESHOLD_DIST)
							nearNodes.Add(n);
						else farNodes.Add(n);

					//further refine to leaves
					Tuple<ATree<TParticle>[], ATree<TParticle>[]> temp, splitInteractionNodes = new(Array.Empty<ATree<TParticle>>(), Array.Empty<ATree<TParticle>>());
					for (int i = 0; i < nearNodes.Count; i++) {
						temp = nearNodes[i].RecursiveFilter(n => ((FarFieldQuadTree<TParticle>)n).BaryCenter_Position.Coordinates.Distance(((FarFieldQuadTree<TParticle>)leaf).BaryCenter_Position.Coordinates) <= Parameters.FARFIELD_THRESHOLD_DIST);
						splitInteractionNodes = new(
							splitInteractionNodes.Item1.Concat(temp.Item1).ToArray(),
							splitInteractionNodes.Item2.Concat(temp.Item2).ToArray());
					}

					this.AdaptiveTimeStepIntegration(
						leaf.NodeElements.ToArray(),
						leaf,
						splitInteractionNodes.Item1,
						splitInteractionNodes.Item2.Concat(farNodes).ToArray());
				});
		}

		private void AdaptiveTimeStepIntegration(TParticle[] particles, ATree<TParticle> leaf, ATree<TParticle>[] nearfieldLeaves, ATree<TParticle>[] farfieldNodes) {
			double[] baryonFarImpulseAsym = farfieldNodes.Aggregate(new double[Parameters.DIM], (totalImpuse, other) =>
				totalImpuse.Add(
					this.Forces.Aggregate(new double[Parameters.DIM], (impulse, force) =>
						impulse.Add(force.ComputeAsymmetricImpulse((FarFieldQuadTree<TParticle>)leaf, (FarFieldQuadTree<TParticle>)other)))));

			double remainingTimeStep = 1d,
				timeStep,
				delta,
				largestDelta;
			int subdivisionPow;
			bool anyLeft = true;
			while (anyLeft && remainingTimeStep > Parameters.WORLD_EPSILON) {
				anyLeft = false;

				largestDelta = this.ComputeImpulses(particles, leaf, nearfieldLeaves) * remainingTimeStep * Parameters.TIME_SCALE;
				delta = this.HandleCollisions(particles, true);//inside of node only
				largestDelta = largestDelta > delta ? largestDelta : delta;

				timeStep = remainingTimeStep;
				subdivisionPow = 0;
				while (subdivisionPow < Parameters.ADAPTIVE_TIME_MAX_DIVISIONS && timeStep > 2d*Parameters.WORLD_EPSILON && largestDelta > Parameters.ADAPTIVE_TIME_GRANULARITY) {
					subdivisionPow++;
					largestDelta /= 2d;
					timeStep /= 2d;
				}

				for (int i = 0; i < particles.Length; i++)
					if ((anyLeft |= particles[i].Enabled)) {
						particles[i].FarfieldImpulse = baryonFarImpulseAsym.Multiply(particles[i].Mass);
						particles[i].ApplyTimeStep(
							particles[i].NearfieldImpulse
								.Add(particles[i].FarfieldImpulse)
								.Divide(particles[i].Mass)
								.Clamp(timeStep * Parameters.TIME_SCALE * Parameters.PARTICLE_MAX_ACCELERATION),
							timeStep * Parameters.TIME_SCALE);
						particles[i].CollisionImpulse = new double[Parameters.DIM];
					}

				remainingTimeStep -= timeStep;
			}
		}

		private double ComputeImpulses(TParticle[] particles, ATree<TParticle> leaf, ATree<TParticle>[] nearfieldLeaves) {
			double accelerationDelta, velocityDelta, distance,
				largestDelta = 0d;
			double[] toOther, impulse, totalImpulse;
			bool collision;
			for (int selfIdx = 0; selfIdx < particles.Length; selfIdx++) {
				if (particles[selfIdx].Enabled) {
					particles[selfIdx].NearfieldImpulse = new double[Parameters.DIM];

					for (int otherIdx = selfIdx + 1; otherIdx < particles.Length; otherIdx++) {//symmetric interaction (handshake optimization)
						if (particles[otherIdx].Enabled) {
							totalImpulse = new double[Parameters.DIM];
							toOther = particles[otherIdx].LiveCoordinates.Subtract(particles[selfIdx].LiveCoordinates);
							distance = toOther.Magnitude();

							for (int fIdx = 0; fIdx < this.Forces.Length; fIdx++) {
								impulse = this.Forces[fIdx].ComputeImpulse(distance, toOther, particles[selfIdx], particles[otherIdx], out collision);
								totalImpulse = totalImpulse.Add(impulse);
								if (collision)
									particles[selfIdx].NodeCollisions.Enqueue(particles[otherIdx]);
							}
							particles[selfIdx].NearfieldImpulse = particles[selfIdx].NearfieldImpulse.Add(totalImpulse);
							particles[otherIdx].NearfieldImpulse = particles[otherIdx].NearfieldImpulse.Subtract(totalImpulse);

							accelerationDelta = totalImpulse.Magnitude()
								/ (particles[selfIdx].Mass < particles[otherIdx].Mass ? particles[selfIdx].Mass : particles[otherIdx].Mass);
							largestDelta = accelerationDelta > largestDelta ? accelerationDelta : largestDelta;
					
							if (distance > Parameters.WORLD_EPSILON) {
								velocityDelta = particles[selfIdx].Velocity.Subtract(particles[otherIdx].Velocity).Magnitude() / distance;
								largestDelta = velocityDelta > largestDelta ? velocityDelta : largestDelta;
							}
						}
					}

					for (int b = 0; b < nearfieldLeaves.Length; b++) {//asymmetric interaction
						foreach (TParticle other in nearfieldLeaves[b].NodeElements) {
							if (other.Enabled) {
								totalImpulse = new double[Parameters.DIM];
								toOther = other.LiveCoordinates.Subtract(particles[selfIdx].LiveCoordinates);
								distance = toOther.Magnitude();

								for (int fIdx = 0; fIdx < this.Forces.Length; fIdx++) {
									impulse = this.Forces[fIdx].ComputeImpulse(distance, toOther, particles[selfIdx], other, out collision);
									totalImpulse = totalImpulse.Add(impulse);
									if (collision)
										particles[selfIdx].NeighborNodeCollisions.Enqueue(other);
								}
								particles[selfIdx].NearfieldImpulse = particles[selfIdx].NearfieldImpulse.Add(totalImpulse);
							}
						}
					}
				}
			}
			return largestDelta;
		}
	}
}