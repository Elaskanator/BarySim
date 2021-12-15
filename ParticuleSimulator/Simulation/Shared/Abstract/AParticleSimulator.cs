using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Generic.Extensions;
using Generic.Models;
using Generic.Vectors;

namespace ParticleSimulator.Simulation {
	public interface IParticleSimulator {
		public IParticle[] EnabledParticles { get; }
		public IParticleGroup[] ParticleGroups { get; }

		public ITree RebuildTree();
		public ConsoleColor ChooseGroupColor(IParticle[] particles);
		public IParticle[] RefreshSimulation(object[] parameters);
	}

	public abstract class AParticleSimulator<TParticle> : IParticleSimulator
	where TParticle : AParticle<TParticle> {
		public AParticleSimulator() {
			this.ParticleGroups = Enumerable.Range(0, Parameters.PARTICLES_GROUP_COUNT)
				.Select(i => this.NewParticleGroup())
				.ToArray();
			this.EnabledParticles = this.ParticleGroups.SelectMany(g => g.MemberParticles).Where(p => p.Enabled).ToArray();

			this.HandleBounds();
		}
		
		public double[] NearfieldImpulse { get; set; }
		public double[] FarfieldImpulse { get; set; }
		public double[] CollisionImpulse { get; set; }

		public TParticle[] EnabledParticles { get; private set; }
		IParticle[] IParticleSimulator.EnabledParticles => this.EnabledParticles;
		public AParticleGroup<TParticle>[] ParticleGroups { get; private set; }
		IParticleGroup[] IParticleSimulator.ParticleGroups => this.ParticleGroups;

		public virtual bool EnableCollisions => false;
		public virtual double WorldBounceWeight => 0d;

		protected abstract AParticleGroup<TParticle> NewParticleGroup();

		protected virtual bool DoCombine(double distance, TParticle smaller, TParticle larger) { return false; }
		protected virtual double[] ComputeCollisionAcceleration(double distance, double[] toOther, TParticle smaller, TParticle larger) { return new double[Parameters.DIM]; }

		public TParticle[] RefreshSimulation(QuadTree<TParticle> tree) {
			if (this.EnabledParticles.Length == 0)
				Program.CancelAction(null, null);

			this.Refresh(tree);

			this.HandleCollisions(this.EnabledParticles, false);//outside of node

			this.HandleBounds();
			
			this.EnabledParticles = Program.AllParticles.Where(p => p.Enabled).Cast<TParticle>().ToArray();

			return this.EnabledParticles;
		}
		IParticle[] IParticleSimulator.RefreshSimulation(object[] parameters) { return this.RefreshSimulation((QuadTree<TParticle>)parameters[0]); }

		protected abstract void Refresh(QuadTree<TParticle> tree);
		protected abstract ATree<TParticle> NewTree(double[] leftCorner, double[] rightCorner);

		protected void HandleCollisions(TParticle[] particles, bool intraNode) {
			if (this.EnableCollisions) {
				double distance;
				double[] toOther, collisionAcceleration;
				TParticle self, other, smaller, larger;
				HashSet<TParticle> evaluatedCollisions = new();
				Queue<TParticle> pendingCollisions;

				for (int i = 0; i < particles.Length; i++) {
					self = particles[i];
					if (self.Enabled) {
						pendingCollisions = intraNode
							? new(self.NodeCollisions.Where(p => p.Enabled).Cast<TParticle>())
							: new(self.NeighborNodeCollisions.Where(p => p.Enabled).Cast<TParticle>());
						while (pendingCollisions.TryDequeue(out other) && !evaluatedCollisions.Contains(other)) {
							evaluatedCollisions.Add(other);

							toOther = other.LiveCoordinates.Subtract(self.LiveCoordinates);
							distance = toOther.Magnitude();
							if (distance < self.Radius + other.Radius) {
								smaller = self.Radius < other.Radius ? self : other;
								larger = self.Radius < other.Radius ? other : self;
								if (this.DoCombine(distance, smaller, larger)) {
									if (intraNode)
										foreach (TParticle tail in other.NodeCollisions.Where(tail => tail.Enabled && !evaluatedCollisions.Contains(tail)))
											pendingCollisions.Enqueue(tail);
									else foreach (TParticle tail in other.NeighborNodeCollisions.Where(tail => tail.Enabled && !evaluatedCollisions.Contains(tail)))
											pendingCollisions.Enqueue(tail);

									self.CombineWith(other);
								} else {
									collisionAcceleration = this.ComputeCollisionAcceleration(distance, toOther, smaller, larger);
									self.CollisionAcceleration = self.CollisionAcceleration.Add(collisionAcceleration);
									other.CollisionAcceleration = other.CollisionAcceleration.Subtract(collisionAcceleration);
								}
							}
						}

						if (intraNode)
							self.NodeCollisions.Clear();
						else self.NeighborNodeCollisions.Clear();
					}
				}
			}
		}

		private void HandleBounds() {
			Parallel.ForEach(
				this.EnabledParticles.Where(p => p.Enabled),
				Parameters.MulithreadedOptions,
				p => {
					if (Parameters.WORLD_WRAPPING)
						p.WrapPosition();
					else if (Parameters.WORLD_BOUNDING)
						p.BoundPosition();
					else if (p.LiveCoordinates.Any((c, d) => c < -Parameters.WORLD_DEATH_BOUND_CNT*Parameters.DOMAIN_SIZE[d] || c > Parameters.DOMAIN_SIZE[d] *(1d + Parameters.WORLD_DEATH_BOUND_CNT)))
						p.Enabled = false;

					if (this.WorldBounceWeight > 0d)
						p.BounceVelocity(this.WorldBounceWeight);
			});
		}

		public ITree RebuildTree() {
			double[]
				leftCorner = Enumerable.Repeat(double.PositiveInfinity, Parameters.DIM).ToArray(),
				rightCorner = Enumerable.Repeat(double.NegativeInfinity, Parameters.DIM).ToArray();
			TParticle[] particles = (TParticle[])this.EnabledParticles.Clone();
			TParticle particle;
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

			ATree<TParticle> result = this.NewTree(leftCorner, rightCorner);
			result.AddRange(particles);
			return result;
		}

		public virtual ConsoleColor ChooseGroupColor(IEnumerable<TParticle> particles) {
			int dominantGroupID;
			if (Parameters.DIM > 2)
				dominantGroupID = particles.MinBy(p => Renderer.GetDepthScalar(p.LiveCoordinates)).GroupID;
			else dominantGroupID  = particles.GroupBy(p => p.GroupID).MaxBy(g => g.Count()).Key;
			return Parameters.COLOR_ARRAY[dominantGroupID % Parameters.COLOR_ARRAY.Length];
		}
		public ConsoleColor ChooseGroupColor(IParticle[] particles) { return this.ChooseGroupColor(particles.Cast<TParticle>()); }
	}
}