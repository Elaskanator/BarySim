using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Generic.Extensions;
using Generic.Models;
using Generic.Vectors;

namespace ParticleSimulator.Simulation {
	public interface IParticleSimulator {
		public AClassicalParticle[] AllParticles { get; }
		public Scaling Scaling { get; }
		public AInverseSquareForce[] Forces { get; }

		public ITree RebuildTree();
		public ConsoleColor ChooseColor(Tuple<char, AClassicalParticle[], double> others);
		public AClassicalParticle[] RefreshSimulation(object[] parameters);
	}

	public abstract class AParticleSimulator : IParticleSimulator {
		public AParticleSimulator(params AInverseSquareForce[] forces) {
			this.Forces = forces;

			this.Scaling = new();
			this.ParticleGroups = Enumerable.Range(0, Parameters.PARTICLES_GROUP_COUNT)
				.Select(i => this.NewParticleGroup())
				.ToArray();
			this.AllParticles = this.ParticleGroups.SelectMany(g => g.Particles).ToArray();
			this.HandleBounds();
		}

		public AClassicalParticle[] AllParticles { get; private set; }
		public AParticleGroup[] ParticleGroups { get; private set; }
		public AInverseSquareForce[] Forces { get; private set; }
		public Scaling Scaling { get; private set; }

		public virtual double WorldBounceWeight => 0d;
		public virtual int InteractionLimit => int.MaxValue;

		protected abstract AParticleGroup NewParticleGroup();

		protected virtual double[] ComputeCollisionImpulse(double distance, double[] toOther, AClassicalParticle smaller, AClassicalParticle larger) { return new double[Parameters.DIM]; }
		protected virtual bool DoCombine(double distance, AClassicalParticle smaller, AClassicalParticle larger) { return false; }

		public AClassicalParticle[] RefreshSimulation(object[] parameters) {
			if (this.AllParticles.Length == 0)
				Program.CancelAction(null, null);

			for (int i = 0; i < this.AllParticles.Length; i++) {
				this.AllParticles[i].Impulse = new double[Parameters.DIM];
				this.AllParticles[i].Collisions.Clear();
				this.AllParticles[i].EvaluatedCollisions.Clear();
			}

			this.InteractNearField((QuadTree<AClassicalParticle>)parameters[0]);
			if (this.Forces.Length > 0)
				this.InteractFullFieldSymmetric((FarFieldQuadTree)parameters[0]);

			this.HandleCollisions();
			this.HandleBounds();
			this.AllParticles = this.AllParticles.Where(p => p.IsAlive).ToArray();
			
			for (int i = 0; i < this.AllParticles.Length; i++)
				this.AllParticles[i].ApplyTimeStep();

			return this.AllParticles;
		}

		protected virtual void InteractNearField(QuadTree<AClassicalParticle> tree) { }

		private void InteractFullFieldSymmetric(FarFieldQuadTree tree) {//modified Barnes-Hut Algorithm
			Parallel.ForEach(
				tree.Leaves.Where(n => n.NumMembers > 0),
				Parameters.MulithreadedOptions,
				leaf => {
					AClassicalParticle[] particles = leaf.NodeElements.ToArray();
					if (particles.Length > 0) {
						//recursively discover near and far nodes based on distance
						List<ATree<AClassicalParticle>> nearNodes = new(), farNodes = new();
						foreach (ATree<AClassicalParticle> n in leaf.GetNeighborhoodNodes(Parameters.FARFIELD_NEIGHBORHOOD_FILTER))
							if (((FarFieldQuadTree)n).BaryCenter_Mass.Coordinates.Distance(((FarFieldQuadTree)leaf).BaryCenter_Mass.Coordinates) <= Parameters.TREE_FARFIELD_THRESHOLD)
								nearNodes.Add(n);
							else farNodes.Add(n);
						//further refine to leaves
						Tuple<ATree<AClassicalParticle>[], ATree<AClassicalParticle>[]> temp, splitInteractionNodes = new(Array.Empty<ATree<AClassicalParticle>>(), Array.Empty<ATree<AClassicalParticle>>());
						for (int i = 0; i < nearNodes.Count; i++) {
							temp = nearNodes[i].RecursiveFilter(n => ((FarFieldQuadTree)n).BaryCenter_Mass.Coordinates.Distance(((FarFieldQuadTree)leaf).BaryCenter_Mass.Coordinates) <= Parameters.TREE_FARFIELD_THRESHOLD);
							splitInteractionNodes = new(
								splitInteractionNodes.Item1.Concat(temp.Item1).ToArray(),
								splitInteractionNodes.Item2.Concat(temp.Item2).ToArray());
						}
						ATree<AClassicalParticle>[]
							nearLeaves = splitInteractionNodes.Item1.ToArray(),
							farLeaves = splitInteractionNodes.Item2.Concat(farNodes).ToArray();

						double[] baryonFarImpulse = farLeaves.Aggregate(new double[Parameters.DIM], (totalImpuse, other) =>
							totalImpuse.Add(
								this.Forces.Aggregate(new double[Parameters.DIM], (impulse, force) =>
									impulse.Add(force.ComputeImpulse((FarFieldQuadTree)leaf, (FarFieldQuadTree)other)))));

						double[] impulse, totalImpulse;
						for (int i = 0; i < particles.Length; i++) {
							particles[i].Impulse = particles[i].Impulse.Add(baryonFarImpulse);//asymmetric interaction

							for (int j = i + 1; j < particles.Length; j++) {//symmetric interaction
								totalImpulse = new double[Parameters.DIM];
								for (int fIdx = 0; fIdx < this.Forces.Length; fIdx++) {
									impulse = this.Forces[fIdx].ComputeImpulse(particles[i], particles[j]);
									totalImpulse = totalImpulse.Add(impulse);
								}
								particles[i].Impulse = particles[i].Impulse.Add(totalImpulse);
								particles[j].Impulse = particles[j].Impulse.Subtract(totalImpulse);
							}

							for (int b = 0; b < nearLeaves.Length; b++)//asymmetric interaction
								foreach (AClassicalParticle p in nearLeaves[b].AllElements) {
									totalImpulse = new double[Parameters.DIM];
									for (int fIdx = 0; fIdx < this.Forces.Length; fIdx++) {
										impulse = this.Forces[fIdx].ComputeImpulse(particles[i], p);
										totalImpulse = totalImpulse.Add(impulse);
									}
									particles[i].Impulse = particles[i].Impulse.Add(totalImpulse);
								}
						}
					}
				});
		}

		private void HandleCollisions() {
			double distance, newMass;
			double[] toOther, collisionImpulse;
			AClassicalParticle self, other, smaller, larger, tail;
			ConcurrentQueue<AClassicalParticle> pendingCollisions;

			for (int i = 0; i < this.AllParticles.Length; i++) {
				self = this.AllParticles[i];
				if (self.IsAlive) {
					pendingCollisions = self.Collisions;
					while (pendingCollisions.TryDequeue(out other) && other.IsAlive && !self.EvaluatedCollisions.Contains(other)) {
						self.EvaluatedCollisions.Add(other);

						toOther = other.LiveCoordinates.Subtract(self.LiveCoordinates);
						distance = toOther.Magnitude();
						if (distance < self.Radius + other.Radius) {
							smaller = self.Radius < other.Radius ? self : other;
							larger = self.Radius < other.Radius ? other : self;
							if (this.DoCombine(distance, smaller, larger)) {
								while (other.Collisions.TryDequeue(out tail) && !self.EvaluatedCollisions.Contains(tail))
									pendingCollisions.Enqueue(tail);

								newMass = self.Mass + other.Mass;
								self.LiveCoordinates = self.LiveCoordinates.Multiply(self.Mass)
										.Add(other.LiveCoordinates.Multiply(other.Mass))
										.Divide(newMass);
								self.Mass = newMass;
								self.Charge += other.Charge;
								self.Momentum = self.Momentum.Add(other.Momentum);
								self.Impulse = self.Impulse.Add(other.Impulse);
								other.IsAlive = false;
							} else {
								collisionImpulse = this.ComputeCollisionImpulse(distance, toOther, smaller, larger);
								self.Impulse = self.Impulse.Add(collisionImpulse);
								self.Impulse = self.Impulse.Subtract(collisionImpulse);
							}
						}
					}
				}
			}
		}

		public ITree RebuildTree() {
			double[]
				leftCorner = Enumerable.Repeat(double.PositiveInfinity, Parameters.DIM).ToArray(),
				rightCorner = Enumerable.Repeat(double.NegativeInfinity, Parameters.DIM).ToArray();
			AClassicalParticle[] particles = (AClassicalParticle[])this.AllParticles.Clone();
			AClassicalParticle particle;
			for (int i = 0; i < particles.Length; i++) {
				particle = particles[i];
				particle._coordinates = (double[])particle.LiveCoordinates.Clone();
				for (int d = 0; d < Parameters.DIM; d++) {
					leftCorner[d] = leftCorner[d] < particle.Coordinates[d] ? leftCorner[d] : particle.Coordinates[d];
					rightCorner[d] = rightCorner[d] > particle.Coordinates[d] ? rightCorner[d] : particle.Coordinates[d];
				}
			}

			QuadTree<AClassicalParticle> result;
			if (this.Forces.Length > 0)
				result = new FarFieldQuadTree(leftCorner, rightCorner.Select(x => x += Parameters.WORLD_EPSILON).ToArray(), null);
			else result = new NearFieldQuadTree(leftCorner, rightCorner.Select(x => x += Parameters.WORLD_EPSILON).ToArray());
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
				this.AllParticles,
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