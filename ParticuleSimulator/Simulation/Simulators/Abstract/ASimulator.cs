using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Generic.Extensions;
using Generic.Models;
using Generic.Vectors;

namespace ParticleSimulator.Simulation {
	public interface ISimulator {
		public IEnumerable<ISimulationParticle> Particles { get; }

		public ParticleData[] RefreshSimulation();
	}

	public abstract partial class ASimulator<TParticle> : ISimulator
	where TParticle : ABaryonParticle<TParticle> {
		public ASimulator() {
			this.ParticleGroups = Enumerable
				.Range(0, Parameters.PARTICLES_GROUP_COUNT)
				.Select(i => this.NewParticleGroup())
				.ToArray();

			this.WrappedParticles = this.ParticleGroups
				.SelectMany(g => g.InitialParticles
					.Select(p => new WrappedParticle(p)))
				.ToArray();
			this.Tree = this.NewTree(this.WrappedParticles.Select(wp => wp.Particle));

			//this.WrappedParticles = this.Tree
			//	.LeafNodesNonEmpty
			//	.SelectMany(leaf => leaf.AllElements
			//		.Select(p => new WrappedParticle(p, leaf)))
			//	.ToArray();
		}
		
		protected WrappedParticle[] WrappedParticles { get; private set; }
		public IEnumerable<ISimulationParticle> Particles => this.WrappedParticles.Select(wp => wp.Particle);
		public AParticleGroup<TParticle>[] ParticleGroups { get; private set; }
		public MagicTree<TParticle> Tree { get; private set; }

		public abstract bool EnableFarfield { get; }
		public virtual bool EnableCollisions => false;
		public virtual float WorldBounceWeight => 0f;

		protected abstract AParticleGroup<TParticle> NewParticleGroup();
		protected abstract MagicTree<TParticle> NewTree(IEnumerable<TParticle> particles);

		protected virtual bool DoCombine(float distance, TParticle smaller, TParticle larger) { return false; }
		protected virtual Vector<float> ComputeCollisionAcceleration(float distance, Vector<float> toOther, TParticle smaller, TParticle larger) { return Vector<float>.Zero; }

		public ParticleData[] RefreshSimulation() {//modified Barnes-Hut Algorithm
			this.Refresh();

			this.Tree = this.Tree.Refresh();

			return this.WrappedParticles.Select(wp => new ParticleData(wp.Particle)).ToArray();
		}

		private void Refresh() {
			//TODODOD
		}

		protected float HandleCollisions(IEnumerable<TParticle> particles) {
			float largestDelta = 0f;//used for adative time steps
			if (this.EnableCollisions) {
				TParticle other;
				Vector<float> toOther;
				float distance, strength;

				Queue<TParticle> pending;
				HashSet<TParticle> eavluated = new();
				foreach (TParticle self in particles) {
					if (self.IsEnabled && eavluated.Add(self)) {
						pending = new();
						while (self.Collisions.TryDequeue(out other)) {
							if (other.IsEnabled)
								pending.Enqueue(other);
						}

						while (pending.TryDequeue(out other) && eavluated.Add(other)) {
							toOther = other.Position - self.Position;
							distance = toOther.Magnitude();

							if (distance <= self.Radius + other.Radius) {
								strength = 0f;
								if (self.CollideCombine(distance, toOther, other, ref strength)) {
									self.MergedParticles.Add(other);
									foreach (TParticle tail in other.Collisions.Where(tail => tail.IsEnabled))
										pending.Enqueue(tail);
								} else largestDelta = largestDelta > strength ? largestDelta : strength;
							}
						}
					}
				}
			}
			return largestDelta;
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