using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Generic.Extensions;
using Generic.Models;
using Generic.Vectors;

namespace ParticleSimulator.Simulation {
	public interface IParticleSimulator {
		public AClassicalParticle[] AliveParticles { get; }
		public AParticleGroup[] ParticleGroups { get; }
		public Scaling Scaling { get; }
		public AForce[] Forces { get; }

		public ITree RebuildTree();
		public ConsoleColor ChooseColor(Tuple<char, AClassicalParticle[], double> others);
		public AClassicalParticle[] RefreshSimulation(object[] parameters);
	}

	public abstract class AParticleSimulator : IParticleSimulator {
		public AParticleSimulator(params AForce[] forces) {
			this.Forces = forces;

			this.Scaling = new();
			this.ParticleGroups = Enumerable.Range(0, Parameters.PARTICLES_GROUP_COUNT)
				.Select(i => this.NewParticleGroup())
				.ToArray();
			this.AliveParticles = this.ParticleGroups.SelectMany(g => g.Particles).ToArray();
			this.HandleBounds();
		}

		public AClassicalParticle[] AliveParticles { get; private set; }
		public AParticleGroup[] ParticleGroups { get; private set; }
		public AForce[] Forces { get; private set; }
		public Scaling Scaling { get; private set; }

		public virtual double WorldBounceWeight => 0d;
		public virtual int InteractionLimit => int.MaxValue;

		protected abstract AParticleGroup NewParticleGroup();

		protected virtual bool DoCombine(double distance, AClassicalParticle smaller, AClassicalParticle larger) { return false; }
		protected virtual double[] ComputeCollisionImpulse(double distance, double[] toOther, AClassicalParticle smaller, AClassicalParticle larger) { return new double[Parameters.DIM]; }

		public AClassicalParticle[] RefreshSimulation(object[] parameters) {
			if (this.AliveParticles.Length == 0)
				Program.CancelAction(null, null);

			for (int i = 0; i < this.AliveParticles.Length; i++) {
				this.AliveParticles[i].CollisionImpulse = new double[Parameters.DIM];
			}

			this.Refresh((FarFieldQuadTree)parameters[0]);

			this.HandleCollisions(this.AliveParticles, false);//outside of node

			this.HandleBounds();

			this.AliveParticles = this.AliveParticles.Where(p => p.IsAlive).ToArray();

			return this.AliveParticles;
		}

		private void Refresh(FarFieldQuadTree tree) {//modified Barnes-Hut Algorithm
			Parallel.ForEach(
				tree.Leaves.Where(n => n.NumMembers > 0),
				Parameters.MulithreadedOptions,
				leaf => {
					//recursively discover near and far nodes based on distance
					List<ATree<AClassicalParticle>> nearNodes = new(), farNodes = new();
					foreach (ATree<AClassicalParticle> n in leaf.GetNeighborhoodNodes(Parameters.FARFIELD_NEIGHBORHOOD_FILTER_DEPTH))
						if (((FarFieldQuadTree)n).BaryCenter_Position.Coordinates.Distance(((FarFieldQuadTree)leaf).BaryCenter_Position.Coordinates) <= Parameters.FARFIELD_THRESHOLD_DIST)
							nearNodes.Add(n);
						else farNodes.Add(n);

					//further refine to leaves
					Tuple<ATree<AClassicalParticle>[], ATree<AClassicalParticle>[]> temp, splitInteractionNodes = new(Array.Empty<ATree<AClassicalParticle>>(), Array.Empty<ATree<AClassicalParticle>>());
					for (int i = 0; i < nearNodes.Count; i++) {
						temp = nearNodes[i].RecursiveFilter(n => ((FarFieldQuadTree)n).BaryCenter_Position.Coordinates.Distance(((FarFieldQuadTree)leaf).BaryCenter_Position.Coordinates) <= Parameters.FARFIELD_THRESHOLD_DIST);
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

		private void AdaptiveTimeStepIntegration(AClassicalParticle[] particles, ATree<AClassicalParticle> leaf, ATree<AClassicalParticle>[] nearfieldLeaves, ATree<AClassicalParticle>[] farfieldNodes) {
			double[] baryonFarImpulse = farfieldNodes.Aggregate(new double[Parameters.DIM], (totalImpuse, other) =>
				totalImpuse.Add(
					this.Forces.Aggregate(new double[Parameters.DIM], (impulse, force) =>
						impulse.Add(force.ComputeImpulse((FarFieldQuadTree)leaf, (FarFieldQuadTree)other)))));

			double remainingTimeStep = 1d,
				timeStep,
				largestDelta;
			int subdivisionPow;
			bool anyLeft = true;
			while (anyLeft && remainingTimeStep > 0) {
				anyLeft = false;

				largestDelta = this.ComputeImpulses(particles, leaf, nearfieldLeaves, baryonFarImpulse) * remainingTimeStep * Parameters.TIME_SCALE;
				timeStep = remainingTimeStep;
				subdivisionPow = 0;
				while (subdivisionPow < Parameters.ADAPTIVE_TIME_MAX_DIVISIONS && largestDelta > Parameters.ADAPTIVE_TIME_GRANULARITY) {
					subdivisionPow++;
					largestDelta /= 2d;
					timeStep /= 2d;
				}

				this.HandleCollisions(particles, true);//inside of node only
				for (int i = 0; i < particles.Length; i++)
					if ((anyLeft |= particles[i].IsAlive))
						particles[i].ApplyTimeStep(timeStep * Parameters.TIME_SCALE);

				remainingTimeStep -= timeStep;
			}
		}

		private double ComputeImpulses(AClassicalParticle[] particles, ATree<AClassicalParticle> leaf, ATree<AClassicalParticle>[] nearfieldLeaves, double[] farFieldImpulse) {
			double accelerationDelta, velocityDelta, distance,
				largestDelta = 0d;
			double[] toOther, impulse, totalImpulse;
			bool collision;
			for (int selfIdx = 0; selfIdx < particles.Length; selfIdx++) {
				if (particles[selfIdx].IsAlive) {
					particles[selfIdx].NearfieldImpulse = new double[Parameters.DIM];
					particles[selfIdx].FarfieldImpulse = farFieldImpulse;

					for (int otherIdx = selfIdx + 1; otherIdx < particles.Length; otherIdx++) {//symmetric interaction (handshake optimization)
						if (particles[otherIdx].IsAlive) {
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
						foreach (AClassicalParticle other in nearfieldLeaves[b].NodeElements) {
							if (other.IsAlive) {
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

								accelerationDelta = totalImpulse.Magnitude()
									/ (particles[selfIdx].Mass < other.Mass ? particles[selfIdx].Mass : other.Mass);
								largestDelta = accelerationDelta > largestDelta ? accelerationDelta : largestDelta;
						
								if (distance > Parameters.WORLD_EPSILON) {
									velocityDelta = particles[selfIdx].Velocity.Subtract(other.Velocity).Magnitude() / distance;
									largestDelta = velocityDelta > largestDelta ? velocityDelta : largestDelta;
								}
							}
						}
					}
				}
			}
			return largestDelta;
		}

		private void HandleCollisions(AClassicalParticle[] particles, bool intraNode) {
			double distance;
			double[] toOther, collisionImpulse;
			AClassicalParticle self, other, smaller, larger;
			HashSet<AClassicalParticle> evaluatedCollisions = new();
			Queue<AClassicalParticle> pendingCollisions;

			for (int i = 0; i < particles.Length; i++) {
				self = particles[i];
				if (self.IsAlive) {
					pendingCollisions = intraNode
						? new(self.NodeCollisions.Where(p => p.IsAlive))
						: new(self.NeighborNodeCollisions.Where(p => p.IsAlive));
					while (pendingCollisions.TryDequeue(out other) && !evaluatedCollisions.Contains(other)) {
						evaluatedCollisions.Add(other);

						toOther = other.LiveCoordinates.Subtract(self.LiveCoordinates);
						distance = toOther.Magnitude();
						if (distance < self.Radius + other.Radius) {
							smaller = self.Radius < other.Radius ? self : other;
							larger = self.Radius < other.Radius ? other : self;
							if (this.DoCombine(distance, smaller, larger)) {
								if (intraNode)
									foreach (AClassicalParticle tail in other.NodeCollisions.Where(tail => tail.IsAlive && !evaluatedCollisions.Contains(tail)))
										pendingCollisions.Enqueue(tail);
								else foreach (AClassicalParticle tail in other.NeighborNodeCollisions.Where(tail => tail.IsAlive && !evaluatedCollisions.Contains(tail)))
										pendingCollisions.Enqueue(tail);

								this.CombineParticles(self, other);
							} else {
								collisionImpulse = this.ComputeCollisionImpulse(distance, toOther, smaller, larger);
								self.CollisionImpulse = self.CollisionImpulse.Add(collisionImpulse);
								other.CollisionImpulse = other.CollisionImpulse.Subtract(collisionImpulse);
							}
						}
					}

					if (intraNode)
						self.NodeCollisions.Clear();
					else self.NeighborNodeCollisions.Clear();
				}
			}
		}
		private void CombineParticles(AClassicalParticle self, AClassicalParticle other) {
			double totalMass = self.Mass + other.Mass;
			self.LiveCoordinates =
				self.LiveCoordinates.Multiply(self.Mass)
				.Add(other.LiveCoordinates.Multiply(other.Mass))
				.Divide(totalMass);
			self.Mass = totalMass;
			self.Charge += other.Charge;
			self.Momentum = self.Momentum.Add(other.Momentum);
			self.NearfieldImpulse = self.NearfieldImpulse.Add(other.NearfieldImpulse);
			self.FarfieldImpulse = self.FarfieldImpulse.Add(other.FarfieldImpulse);
			self.CollisionImpulse = self.CollisionImpulse.Add(other.CollisionImpulse);
			other.IsAlive = false;
		}

		public ITree RebuildTree() {
			double[]
				leftCorner = Enumerable.Repeat(double.PositiveInfinity, Parameters.DIM).ToArray(),
				rightCorner = Enumerable.Repeat(double.NegativeInfinity, Parameters.DIM).ToArray();
			AClassicalParticle[] particles = (AClassicalParticle[])this.AliveParticles.Clone();
			AClassicalParticle particle;
			for (int i = 0; i < particles.Length; i++) {
				particle = particles[i];
				particle._coordinates = (double[])particle.LiveCoordinates.Clone();
				for (int d = 0; d < Parameters.DIM; d++) {
					leftCorner[d] = leftCorner[d] < particle.Coordinates[d] ? leftCorner[d] : particle.Coordinates[d];
					rightCorner[d] = rightCorner[d] > particle.Coordinates[d] ? rightCorner[d] : particle.Coordinates[d];
				}
			}
			rightCorner = rightCorner.Select(x => x += Parameters.WORLD_EPSILON).ToArray();
			
			double maxSize = leftCorner.Zip(rightCorner, (l, r) => r - l).Max();
			rightCorner = leftCorner.Select(l => l + maxSize).ToArray();

			QuadTree<AClassicalParticle> result = this.Forces.Length > 0
				? new FarFieldQuadTree(leftCorner, rightCorner)
				: new NearFieldQuadTree(leftCorner, rightCorner);
			result.AddRange(particles);
			return result;
		}

		public ConsoleColor ChooseColor(Tuple<char, AClassicalParticle[], double> particleData) {
			int rank;
			switch (Parameters.COLOR_SCHEME) {
				case ParticleColoringMethod.Density:
					rank = this.Scaling.Values.Drop(1).TakeWhile(ds => ds < particleData.Item3).Count();
					return Parameters.COLOR_ARRAY[rank];
				case ParticleColoringMethod.Group:
					return this.ChooseGroupColor(particleData.Item2);
				case ParticleColoringMethod.Depth:
					if (Parameters.DIM > 2) {
						int numColors = Parameters.COLOR_ARRAY.Length;
						double depth = 1d - particleData.Item2.Min(p => GetDepthScalar(p.LiveCoordinates));
						rank = this.Scaling.Values.Take(numColors - 1).TakeWhile(a => a < depth).Count();
						return Parameters.COLOR_ARRAY[rank];
					} else return Parameters.COLOR_ARRAY[^1];
				default:
					throw new InvalidEnumArgumentException(nameof(Parameters.COLOR_SCHEME));
			}
		}

		public double GetDepthScalar(double[] v) {
			if (Parameters.DIM > 2)
				return 1d - (v.Skip(2).ToArray().Magnitude() / Parameters.DOMAIN_HIDDEN_DIMENSIONAL_HEIGHT);
			else return 1d;
		}

		public virtual ConsoleColor ChooseGroupColor(AClassicalParticle[] particles) {
			int dominantGroupID;
			if (Parameters.DIM > 2)
				dominantGroupID = particles.MinBy(p => this.GetDepthScalar(p.LiveCoordinates)).GroupID;
			else dominantGroupID  = particles.GroupBy(p => p.GroupID).MaxBy(g => g.Count()).Key;
			return Parameters.COLOR_ARRAY[dominantGroupID % Parameters.COLOR_ARRAY.Length];
		}

		private void HandleBounds() {
			Parallel.ForEach(
				this.AliveParticles.Where(p => p.IsAlive),
				Parameters.MulithreadedOptions,
				p => {
					if (Parameters.WORLD_WRAPPING)
						p.WrapPosition();
					else if (Parameters.WORLD_BOUNDING)
						p.BoundPosition();
					else if (p.LiveCoordinates.Any((c, d) => c < -Parameters.WORLD_DEATH_BOUND_CNT*Parameters.DOMAIN_SIZE[d] || c > Parameters.DOMAIN_SIZE[d] *(1d + Parameters.WORLD_DEATH_BOUND_CNT)))
						p.IsAlive = false;

					if (this.WorldBounceWeight > 0d)
						p.BounceVelocity(this.WorldBounceWeight);
			});
		}
	}
}