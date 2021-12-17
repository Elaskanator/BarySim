using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Generic.Extensions;
using Generic.Models;
using Generic.Vectors;

namespace ParticleSimulator.Simulation {
	public interface IParticleSimulator {
		public IEnumerable<ISimulationParticle> Particles { get; }

		public ConsoleColor ChooseGroupColor(IEnumerable<ParticleData> particles);
		public ParticleData[] RefreshSimulation();
	}

	public abstract class AParticleSimulator<TParticle, TTree> : IParticleSimulator
	where TParticle : ABaryonParticle<TParticle>
	where TTree : AQuadTree<TParticle, TTree> {
		public AParticleSimulator() {
			this.ParticleGroups = Enumerable
				.Range(0, Parameters.PARTICLES_GROUP_COUNT)
				.Select(i => this.NewParticleGroup())
				.ToArray();
			
			this.Tree = this.InitializeTree(
				this.ParticleGroups.SelectMany(g => g.MemberParticles));

			this.WrappedParticles = this.HandleBounds(
				this.Tree.LeavesNonempty
					.SelectMany(leaf => leaf.NodeElements.Select(p =>
						new WrappedParticle(p, leaf))))
				.ToArray();
		}
		
		protected WrappedParticle[] WrappedParticles { get; private set; }
		public IEnumerable<ISimulationParticle> Particles => this.WrappedParticles.Select(wp => wp.Particle);
		public AParticleGroup<TParticle>[] ParticleGroups { get; private set; }
		public readonly TTree Tree;
		
		public Vector<float> NearfieldImpulse { get; set; }
		public Vector<float> FarfieldImpulse { get; set; }
		public Vector<float> CollisionImpulse { get; set; }

		public virtual bool EnableCollisions => false;
		public virtual float WorldBounceWeight => 0f;

		protected abstract AParticleGroup<TParticle> NewParticleGroup();
		protected abstract TTree NewTree(Vector<float> leftCorner, Vector<float> rightCorner);

		protected virtual bool DoCombine(float distance, TParticle smaller, TParticle larger) { return false; }
		protected virtual Vector<float> ComputeCollisionAcceleration(float distance, Vector<float> toOther, TParticle smaller, TParticle larger) { return Vector<float>.Zero; }

		public ParticleData[] RefreshSimulation() {//modified Barnes-Hut Algorithm
			if (this.WrappedParticles.Length == 0)
				Program.CancelAction(null, null);
			//throw new NotImplementedException();
			this.WrappedParticles = this.HandleBounds(this.WrappedParticles).ToArray();
			return this.WrappedParticles.Select(wp => new ParticleData(wp.Particle)).ToArray();
		}

		protected float HandleCollisions(IEnumerable<TParticle> particles, bool intraNode) {
			float largestDelta = 0f;
			if (this.EnableCollisions) {
				TParticle other;
				Vector<float> toOther;
				float distance, collisionAcceleration;
				Queue<TParticle> pendingCollisions;
				HashSet<TParticle> evaluatedCollisions = new();

				foreach (TParticle self in particles) {
					if (self.IsEnabled) {
						pendingCollisions = intraNode
							? new(self.NodeCollisions.Where(p => p.IsEnabled).Cast<TParticle>())
							: new(self.NeighborNodeCollisions.Where(p => p.IsEnabled).Cast<TParticle>());

						while (pendingCollisions.TryDequeue(out other) && evaluatedCollisions.Add(other)) {
							toOther = other.Position - self.Position;
							distance = toOther.Magnitude(Parameters.DIM);

							if (distance <= self.Radius + other.Radius) {
								if (self.Absorb(distance, toOther, other)) {
									self.MergedParticles.Add(other);

									if (intraNode)
										foreach (TParticle tail in other.NodeCollisions.Where(tail => tail.IsEnabled))
											pendingCollisions.Enqueue(tail);
									else foreach (TParticle tail in other.NeighborNodeCollisions.Where(tail => tail.IsEnabled))
											pendingCollisions.Enqueue(tail);
								} else {
									collisionAcceleration = self.ComputeCollision(distance, toOther, other);
									largestDelta = largestDelta > collisionAcceleration ? largestDelta : collisionAcceleration;
								}
							}
						}

						if (intraNode)
							self.NodeCollisions.Clear();
						else self.NeighborNodeCollisions.Clear();
					}
				}
			}
			return largestDelta;
		}

		public virtual ConsoleColor ChooseGroupColor(IEnumerable<ParticleData> particles) {
			int dominantGroupID;
			if (Parameters.DIM > 2)
				dominantGroupID = particles.MinBy(p => Renderer.GetDepthScalar(p.Position)).GroupID;
			else dominantGroupID = particles.GroupBy(p => p.GroupID).MaxBy(g => g.Count()).Key;
			return Parameters.COLOR_ARRAY[dominantGroupID % Parameters.COLOR_ARRAY.Length];
		}
		
		private IEnumerable<WrappedParticle> HandleBounds(IEnumerable<WrappedParticle> particles) {
			foreach (WrappedParticle wrappedParticle in particles.Where(wp => wp.Particle.IsEnabled)) {
				if (Parameters.WORLD_WRAPPING)
					wrappedParticle.WrapPosition();
				else if (Parameters.WORLD_BOUNDING)
					wrappedParticle.BoundPosition();
				else for (int d = 0; d < Parameters.DIM; d++)
					if (wrappedParticle.Particle.Position[d] < -Parameters.WORLD_DEATH_BOUND_CNT * Parameters.DOMAIN_SIZE[d]
					|| wrappedParticle.Particle.Position[d] > Parameters.DOMAIN_SIZE[d] * (1f + Parameters.WORLD_DEATH_BOUND_CNT))
						wrappedParticle.Particle.IsEnabled = false;

				if (wrappedParticle.Particle.IsEnabled) {
					if (this.WorldBounceWeight > 0f)
						wrappedParticle.BounceVelocity(this.WorldBounceWeight);

					yield return wrappedParticle;
				}
			}
		}

		private TTree InitializeTree(IEnumerable<TParticle> particles) {
			float[]
				leftCorner = Enumerable.Repeat(float.PositiveInfinity, Parameters.DIM).ToArray(),
				rightCorner = Enumerable.Repeat(float.NegativeInfinity, Parameters.DIM).ToArray();

			foreach (TParticle particle in particles) {
				for (int d = 0; d < Parameters.DIM; d++) {
					leftCorner[d] = leftCorner[d] < particle.Position[d] ? leftCorner[d] : particle.Position[d];
					rightCorner[d] = rightCorner[d] > particle.Position[d] ? rightCorner[d] : particle.Position[d];
				}
			}
			rightCorner = rightCorner.Select(x => x + Parameters.WORLD_EPSILON).ToArray();

			float maxSize = leftCorner.Zip(rightCorner, (l, r) => r - l).Max();
			rightCorner = leftCorner.Select(l => l + maxSize).ToArray();

			TTree result = this.NewTree(VectorFunctions.New(leftCorner), VectorFunctions.New(rightCorner));
			result.AddRange(particles);
			return result;
		}

		public sealed class WrappedParticle {
			public readonly TParticle Particle;
			public TTree Node { get; private set; }
			public bool IsEnabled = true;
			public WrappedParticle(TParticle instance, TTree node) {
				this.Particle = instance;
				this.Node = node;
			}

			public bool WrapPosition() {
				bool result = false;
				float[] coords = new float[Parameters.DIM];
				for (int d = 0; d < Parameters.DIM; d++)
					if (this.Particle.Position[d] < 0f) {
						coords[d] = (this.Particle.Position[d] % Parameters.DOMAIN_SIZE[d]) + Parameters.DOMAIN_SIZE[d];//don't want symmetric modulus
						result = true;
					} else if (this.Particle.Position[d] >= Parameters.DOMAIN_SIZE[d]) {
						coords[d] = this.Particle.Position[d] % Parameters.DOMAIN_SIZE[d];
						result = true;
					} else coords[d] = this.Particle.Position[d];

				if (result)
					this.Particle.Position = VectorFunctions.New(coords);
				return result;
			}
			public bool BoundPosition() {
				bool result = false;
				float[] coords = new float[Parameters.DIM];
				for (int d = 0; d < Parameters.DIM; d++) 
					if (this.Particle.Position[d] < 0f) {
						coords[d] = 0f;
						result = true;
					} else if (this.Particle.Position[d] >= Parameters.DOMAIN_SIZE[d]) {
						coords[d] = Parameters.DOMAIN_SIZE[d] - Parameters.WORLD_EPSILON;
						result = true;
					}
				
				if (result)
					this.Particle.Position = VectorFunctions.New(coords);
				return result;
			}
			public void BounceVelocity(float weight) {
				float dist;
				bool result = false;
				float[] coords = new float[Parameters.DIM];
				for (int d = 0; d < Parameters.DIM; d++) {
					dist = this.Particle.Position[d] - Parameters.DOMAIN_CENTER[d];
					if (dist < -Parameters.DOMAIN_MAX_RADIUS) {
						coords[d] = this.Particle.Velocity[d] + weight * MathF.Pow(Parameters.DOMAIN_MAX_RADIUS - dist, 0.5f);
						result = true;
					} else if (dist > Parameters.DOMAIN_MAX_RADIUS){
						coords[d] = this.Particle.Velocity[d] - weight * MathF.Pow(dist - Parameters.DOMAIN_MAX_RADIUS, 0.5f);
						result = true;
					}
				}
				if (result)
					this.Particle.Velocity = VectorFunctions.New(coords);
			}

			public void UpdateParentNode() {
				TTree node = this.Node;
				while (!node.DoesContainCoordinates(this.Particle.Position))
					if (node.IsRoot) {
						this.Node = node.AddUp(this.Particle);
						return;
					}
					else node = node.Parent;
				this.Node = node.GetContainingLeaf(this.Particle.Position);
			}
		}
	}

	//public abstract class AParticleSimulator<TParticle> : IParticleSimulator
	//where TParticle : AParticle<TParticle> {
	//	public AParticleSimulator() {
	//		this.ParticleGroups = Enumerable
	//			.Range(0, Parameters.PARTICLES_GROUP_COUNT)
	//			.Select(i => this.NewParticleGroup())
	//			.ToArray();
	//		this.Particles = this.HandleBounds(
	//			this.ParticleGroups
	//				.SelectMany(g => g.MemberParticles))
	//			.ToArray();;
	//	}
		
	//	public Vector<float> NearfieldImpulse { get; set; }
	//	public Vector<float> FarfieldImpulse { get; set; }
	//	public Vector<float> CollisionImpulse { get; set; }

	//	public TParticle[] Particles { get; private set; }
	//	IParticle[] IParticleSimulator.Particles => this.Particles;
	//	public AParticleGroup<TParticle>[] ParticleGroups { get; private set; }
	//	IParticleGroup[] IParticleSimulator.ParticleGroups => this.ParticleGroups;

	//	public virtual bool EnableCollisions => false;
	//	public virtual float WorldBounceWeight => 0f;

	//	protected abstract AParticleGroup<TParticle> NewParticleGroup();

	//	protected virtual bool DoCombine(float distance, TParticle smaller, TParticle larger) { return false; }
	//	protected virtual Vector<float> ComputeCollisionAcceleration(float distance, Vector<float> toOther, TParticle smaller, TParticle larger) { return new float[Parameters.DIM]; }

	//	public ParticleData[] RefreshSimulation(object[] parameters) {//modified Barnes-Hut Algorithm
	//		if (this.Particles.Length == 0)
	//			Program.CancelAction(null, null);

	//		IEnumerable<ATree<TParticle>> leaves = ((ATree<TParticle>)parameters[0]).Leaves.Where(n => n.NumMembers > 0);
	//		if (Parameters.SIMULATION_PARALLEL_ENABLE)
	//			leaves = leaves.AsParallel();
	//		TParticle[] resultingParticles = leaves.SelectMany(leaf => this.Refresh(leaf)).ToArray();

	//		this.HandleCollisions(resultingParticles, false);//outside of node
			
	//		this.Particles = this.HandleBounds(resultingParticles).ToArray();
	//		return this.Particles.Where(p => p.Visible).Select(p => p.CloneData()).ToArray();
	//	}

	//	protected abstract IEnumerable<TParticle> Refresh(ATree<TParticle> leaf);

	//	protected abstract ATree<TParticle> NewTree(Vector<float> leftCorner, Vector<float> rightCorner);

	//	private IEnumerable<TParticle> HandleBounds(IEnumerable<TParticle> particles) {
	//		foreach (TParticle particle in particles.Where(p => p.Enabled)) {
	//			if (Parameters.WORLD_WRAPPING)
	//				particle.WrapPosition();
	//			else if (Parameters.WORLD_BOUNDING)
	//				particle.BoundPosition();
	//			else if (particle.LiveCoordinates.Any((c, d) => c < -Parameters.WORLD_DEATH_BOUND_CNT*Parameters.DOMAIN_SIZE[d] || c > Parameters.DOMAIN_SIZE[d] *(1d + Parameters.WORLD_DEATH_BOUND_CNT)))
	//				particle.Enabled = false;

	//			if (particle.Enabled) {
	//				if (this.WorldBounceWeight > 0f)
	//					particle.BounceVelocity(this.WorldBounceWeight);
	//				yield return particle;
	//			}
	//		}
	//	}

	//	public ITree RebuildTree() {
	//		//throw new NotImplementedException();
	//		Vector<float>
	//			leftCorner = Enumerable.Repeat(float.PositiveInfinity, Parameters.DIM).ToArray(),
	//			rightCorner = Enumerable.Repeat(float.NegativeInfinity, Parameters.DIM).ToArray();
	//		TParticle[] particles = (TParticle[])this.Particles.Clone();
	//		TParticle particle;
	//		for (int i = 0; i < particles.Length; i++) {
	//			particle = particles[i];
	//			particle._coordinates = (Vector<float>)particle.LiveCoordinates.Clone();
	//			for (int d = 0; d < Parameters.DIM; d++) {
	//				leftCorner[d] = leftCorner[d] < particle.LiveCoordinates[d] ? leftCorner[d] : particle.LiveCoordinates[d];
	//				rightCorner[d] = rightCorner[d] > particle.LiveCoordinates[d] ? rightCorner[d] : particle.LiveCoordinates[d];
	//			}
	//		}
	//		rightCorner = rightCorner.Select(x => x += Parameters.WORLD_EPSILON).ToArray();
			
	//		float maxSize = leftCorner.Zip(rightCorner, (l, r) => r - l).Max();
	//		rightCorner = leftCorner.Select(l => l + maxSize).ToArray();

	//		ATree<TParticle> result = this.NewTree(leftCorner, rightCorner);
	//		result.AddRange(particles);
	//		return result;
	//	}

	//	public virtual ConsoleColor ChooseGroupColor(IEnumerable<ParticleData> particles) {
	//		int dominantGroupID;
	//		if (Parameters.DIM > 2)
	//			dominantGroupID = particles.MinBy(p => Renderer.GetDepthScalar(p.Coordinates)).GroupID;
	//		else dominantGroupID  = particles.GroupBy(p => p.GroupID).MaxBy(g => g.Count()).Key;
	//		return Parameters.COLOR_ARRAY[dominantGroupID % Parameters.COLOR_ARRAY.Length];
	//	}
	//}
}