using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Generic.Extensions;
using Generic.Models;
using Generic.Vectors;
using ParticleSimulator.ConsoleRendering;

namespace ParticleSimulator.Simulation {
	public class BaryonSimulator {//modified Barnes-Hut Algorithm
		public BaryonSimulator() {
			this.InitialParticleGroups = Enumerable
				.Range(0, Parameters.PARTICLES_GROUP_COUNT)
				.Select(i => new Galaxy())
				.ToArray();

			this.ParticleTree = new BarnesHutTree<BaryonParticle>(Parameters.DIM, 
				this.InitialParticleGroups.SelectMany(g => g.InitialParticles));
			this._livingParticles = new(this.ParticleTree.Count);
		}

		public Galaxy[] InitialParticleGroups { get; private set; }
		public BarnesHutTree<BaryonParticle> ParticleTree { get; private set; }

		public virtual bool EnableCollisions => false;
		public virtual float WorldBounceWeight => 0f;

		private Queue<BaryonParticle> _livingParticles;
		public IEnumerable<ParticleData> RefreshSimulation(object[] parameters) {
			_livingParticles.Clear();
			foreach (BaryonParticle particle in this.ParticleTree) {
				_livingParticles.Enqueue(particle);
				particle.ApplyTimeStep(Vector<float>.Zero, Parameters.TIME_SCALE);
			}
			return _livingParticles.Select(p => new ParticleData(p)).ToArray();
		}

		//private float HandleCollisions(IEnumerable<ABaryonParticle> particles) {
		//	float largestDelta = 0f;//used for adative time steps
		//	if (this.EnableCollisions) {
		//		ABaryonParticle other;
		//		Vector<float> toOther;
		//		float distance, strength;

		//		Queue<ABaryonParticle> pending;
		//		HashSet<ABaryonParticle> eavluated = new();
		//		foreach (ABaryonParticle self in particles) {
		//			if (self.IsEnabled && eavluated.Add(self)) {
		//				pending = new();
		//				while (self.Collisions.TryDequeue(out other)) {
		//					if (other.IsEnabled)
		//						pending.Enqueue(other);
		//				}

		//				while (pending.TryDequeue(out other) && eavluated.Add(other)) {
		//					toOther = other.Position - self.Position;
		//					distance = toOther.Magnitude();

		//					if (distance <= self.Radius + other.Radius) {
		//						strength = 0f;
		//						if (self.CollideCombine(distance, toOther, other, ref strength)) {
		//							self.MergedParticles.Add(other);
		//							foreach (ABaryonParticle tail in other.Collisions.Where(tail => tail.IsEnabled))
		//								pending.Enqueue(tail);
		//						} else largestDelta = largestDelta > strength ? largestDelta : strength;
		//					}
		//				}
		//			}
		//		}
		//	}
		//	return largestDelta;
		//}
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
	//			.ToArray();
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


//using System.Collections.Generic;
//using System.Numerics;
//using Generic.Vectors;

//namespace ParticleSimulator.Simulation.Gravity {
//	public class GravitySimulator : ABaryonSimulator<MatterClump> {
//		public GravitySimulator()
//		: base(new GravitationalForce<MatterClump>(), new ElectrostaticForce<MatterClump>()) { }

//		public override bool EnableCollisions => true;

//		protected override AParticleGroup<MatterClump> NewParticleGroup() => new Galaxy();
//		protected override BarnesHutTree<MatterClump> NewTree(IEnumerable<MatterClump> particles) =>
//			new BarnesHutTree<MatterClump>(Parameters.DIM, particles);

//		protected override bool DoCombine(float distance, MatterClump smaller, MatterClump larger) =>
//			Parameters.GRAVITY_COLLISION_COMBINE
//			&& (distance <= Parameters.WORLD_EPSILON
//				|| distance <= smaller.Position.Distance(
//					(smaller.Mass*smaller.Position + larger.Mass*larger.Position)
//						* (1f/(smaller.Mass + larger.Mass))));

//		/*
//		TODO drag
//		public double[] ComputeInteractionForce(Matter other) {
//			double[] netForce = new double[Parameters.DIM];
//			if (this.IsAlive && other.IsAlive) {
//				//compute gravity
				
//				Matter
//					smaller = this.PhysicalAttributes[PhysicalAttribute.Mass] < other.PhysicalAttributes[PhysicalAttribute.Mass] ? this : other,
//					larger = this.PhysicalAttributes[PhysicalAttribute.Mass] < other.PhysicalAttributes[PhysicalAttribute.Mass] ? other : this;
//				bool tooClose = distance <= Parameters.WORLD_EPSILON,
//					engulfed = distance <= larger.Radius - Parameters.GRAVITY_COMBINE_OVERLAP_CUTOFF * smaller.Radius,
//					excessiveForce = netForce.Magnitude() / this.PhysicalAttributes[PhysicalAttribute.Mass] / distance / distance > Parameters.GRAVITY_MAX_ACCEL * Parameters.TIME_SCALE;//time step of resulting velocity
//				if (tooClose || engulfed) {//into one larger particle
//					if (Parameters.GRAVITY_COLLISION_COMBINE) {//momentum-preserving
//					}
//				}
//				if (tooClose || engulfed) {//into one larger particle
//					if (Parameters.GRAVITY_COLLISION_COMBINE) {//momentum-preserving
//						netForce = new double[Parameters.DIM];//ignore gravity
//						double newMass = this.Mass + other.Mass;
//						double[]
//							newCoordinates = this.Coordinates.Multiply(this.Mass)
//								.Add(other.Coordinates.Multiply(other.Mass))
//								.Divide(newMass),
//							newVelocity = this.Velocity.Multiply(this.Mass)
//								.Add(other.Velocity.Multiply(other.Mass))
//								.Divide(this.Mass + other.Mass),
//							newNetForce = this.NetForce.Add(other.NetForce);
//						//remove smaller particle
//						larger.Coordinates = newCoordinates;
//						larger.Velocity = newVelocity;
//						larger.Mass = newMass;
//						larger.NetForce = newNetForce;
//						smaller.IsActive = false;
//					} else if (tooClose)//treat as in the same spot
//						netForce = new double[Parameters.DIM];//ignore gravity
//					else if (excessiveForce)
//						netForce = netForce.Normalize(
//							Parameters.GRAVITY_MAX_ACCEL * Parameters.TIME_SCALE
//							* distance * distance * this.Mass);
//				} else if (Parameters.GRAVITY_COLLISION_DRAG_STRENGTH > 0f && distance < this.Radius + other.Radius) {//overlap - drag
//					if (excessiveForce)
//						netForce = netForce.Normalize(
//							Parameters.GRAVITY_MAX_ACCEL * Parameters.TIME_SCALE
//							* distance * distance * this.Mass);
//					double overlapRange = this.Radius + other.Radius - larger.Radius;
//					double[] dragForce =
//						other.Velocity
//							.Subtract(this.Velocity)
//							.Multiply(Parameters.GRAVITY_COLLISION_DRAG_STRENGTH * smaller.Radius * (distance - larger.Radius) / overlapRange);
//					//do not include in result, apply directly
//					this.NetForce = this.NetForce.Add(dragForce);
//					other.NetForce = other.NetForce.Subtract(dragForce);
//				} else if (excessiveForce)
//					netForce = netForce.Normalize(
//						Parameters.GRAVITY_MAX_ACCEL * Parameters.TIME_SCALE
//						* distance * distance * this.Mass);
//			}

//			return netForce;
//		}
//	*/
//	}
//}


//using System;
//using System.Collections.Generic;
//using System.Linq;
//using Generic.Models;
//using Generic.Vectors;

//namespace ParticleSimulator.Simulation {
//	public abstract class ABaryonSimulator<TParticle> : ASimulator<TParticle>
//	where TParticle : ABaryonParticle<TParticle>, IEquatable<TParticle>, IEqualityComparer<TParticle> {
//		public ABaryonSimulator(params ABaryonForce<TParticle>[] forces) {
//			this.Forces = forces;
//		}


//		//protected override IEnumerable<TParticle> Refresh(ATree<TParticle> leaf) {
//		//	//recursively discover near and far nodes based on distance
//		//	List<ATree<TParticle>> nearNodes = new(), farNodes = new();
//		//	foreach (ATree<TParticle> n in leaf.GetNeighborhoodNodes(Parameters.FARFIELD_NEIGHBORHOOD_FILTER_DEPTH))
//		//		if (((FarFieldQuadTree<TParticle>)n).BaryCenter_Position.Coordinates.Distance(((FarFieldQuadTree<TParticle>)leaf).BaryCenter_Position.Coordinates) <= Parameters.FARFIELD_THRESHOLD_DIST)
//		//			nearNodes.Add(n);
//		//		else farNodes.Add(n);

//		//	//further refine to leaves
//		//	Tuple<ATree<TParticle>[], ATree<TParticle>[]> temp, splitInteractionNodes = new(Array.Empty<ATree<TParticle>>(), Array.Empty<ATree<TParticle>>());
//		//	for (int i = 0; i < nearNodes.Count; i++) {
//		//		temp = nearNodes[i].RecursiveFilter(n => ((FarFieldQuadTree<TParticle>)n).BaryCenter_Position.Coordinates.Distance(((FarFieldQuadTree<TParticle>)leaf).BaryCenter_Position.Coordinates) <= Parameters.FARFIELD_THRESHOLD_DIST);
//		//		splitInteractionNodes = new(
//		//			splitInteractionNodes.Item1.Concat(temp.Item1).ToArray(),
//		//			splitInteractionNodes.Item2.Concat(temp.Item2).ToArray());
//		//	}

//		//	return this.AdaptiveTimeStepIntegration(
//		//		leaf.NodeElements,
//		//		leaf,
//		//		splitInteractionNodes.Item1,
//		//		splitInteractionNodes.Item2.Concat(farNodes));
//		//}

//		//private IEnumerable<TParticle> AdaptiveTimeStepIntegration(IEnumerable<TParticle> particles, ATree<TParticle> leaf, ATree<TParticle>[] nearfieldLeaves, IEnumerable<ATree<TParticle>> farfieldNodes) {
//		//	double[] baryonFarImpulseAsym = farfieldNodes.Aggregate(new double[Parameters.DIM], (totalImpuse, other) =>
//		//		totalImpuse.Add(
//		//			this.Forces.Aggregate(new double[Parameters.DIM], (impulse, force) =>
//		//				impulse.Add(force.ComputeAsymmetricImpulse((FarFieldQuadTree<TParticle>)leaf, (FarFieldQuadTree<TParticle>)other)))));

//		//	double remainingTimeStep = 1d,
//		//		timeStep,
//		//		delta,
//		//		largestDelta;
//		//	List<TParticle> activeParticles = new(particles), newParticles = new();
//		//	while (remainingTimeStep >= Parameters.ADAPTIVE_TIME_GRANULARITY) {
//		//		largestDelta = this.ComputeImpulses(activeParticles, leaf, nearfieldLeaves) * remainingTimeStep * Parameters.TIME_SCALE;
//		//		delta = this.HandleCollisions(activeParticles, true);//inside of node only
//		//		largestDelta = largestDelta > delta ? largestDelta : delta;

//		//		timeStep =
//		//			largestDelta > Parameters.ADAPTIVE_TIME_CRITERION
//		//				&& remainingTimeStep >= 2d * Parameters.ADAPTIVE_TIME_GRANULARITY
//		//				&& remainingTimeStep * Parameters.ADAPTIVE_TIME_CRITERION / largestDelta >= Parameters.ADAPTIVE_TIME_GRANULARITY
//		//			? timeStep = remainingTimeStep * Parameters.ADAPTIVE_TIME_CRITERION / largestDelta
//		//			: timeStep = remainingTimeStep;

//		//		for (int i = 0; i < activeParticles.Count; i++) {
//		//			if (activeParticles[i].Enabled) {
//		//				activeParticles[i].FarfieldImpulse = baryonFarImpulseAsym.Multiply(activeParticles[i].Mass);
//		//				activeParticles[i].CollisionImpulse = new double[Parameters.DIM];

//		//				newParticles.AddRange(
//		//					activeParticles[i].ApplyTimeStep(
//		//						activeParticles[i].NearfieldImpulse
//		//							.Add(activeParticles[i].FarfieldImpulse)
//		//							.Divide(activeParticles[i].Mass),
//		//						timeStep)
//		//					?? Enumerable.Empty<TParticle>());
//		//			}
//		//		}

//		//		remainingTimeStep -= timeStep;
//		//		(activeParticles, newParticles) = (newParticles, activeParticles);
//		//		newParticles.Clear();
//		//	}
//		//	return activeParticles;
//		//}

//		//private double ComputeImpulses(List<TParticle> particles, ATree<TParticle> leaf, ATree<TParticle>[] nearfieldLeaves) {
//		//	double accelerationDelta, velocityDelta, distance,
//		//		largestDelta = 0f;
//		//	double[] toOther, impulse, totalImpulse;
//		//	bool collision;
//		//	for (int selfIdx = 0; selfIdx < particles.Count; selfIdx++) {
//		//		if (particles[selfIdx].Enabled) {
//		//			particles[selfIdx].NearfieldImpulse = new double[Parameters.DIM];

//		//			for (int otherIdx = selfIdx + 1; otherIdx < particles.Count; otherIdx++) {//symmetric interaction (handshake optimization)
//		//				if (particles[otherIdx].Enabled) {
//		//					totalImpulse = new double[Parameters.DIM];
//		//					toOther = particles[otherIdx].LiveCoordinates.Subtract(particles[selfIdx].LiveCoordinates);
//		//					distance = toOther.Magnitude();

//		//					for (int fIdx = 0; fIdx < this.Forces.Length; fIdx++) {
//		//						impulse = this.Forces[fIdx].ComputeImpulse(distance, toOther, particles[selfIdx], particles[otherIdx], out collision);
//		//						totalImpulse = totalImpulse.Add(impulse);
//		//						if (collision)
//		//							particles[selfIdx].NodeCollisions.Enqueue(particles[otherIdx]);
//		//					}
//		//					particles[selfIdx].NearfieldImpulse = particles[selfIdx].NearfieldImpulse.Add(totalImpulse);
//		//					particles[otherIdx].NearfieldImpulse = particles[otherIdx].NearfieldImpulse.Subtract(totalImpulse);

//		//					accelerationDelta = totalImpulse.Magnitude()
//		//						/ (particles[selfIdx].Mass < particles[otherIdx].Mass ? particles[selfIdx].Mass : particles[otherIdx].Mass);
//		//					largestDelta = accelerationDelta > largestDelta ? accelerationDelta : largestDelta;
					
//		//					if (distance > Parameters.WORLD_EPSILON) {
//		//						velocityDelta = particles[selfIdx].Velocity.Subtract(particles[otherIdx].Velocity).Magnitude() / distance;
//		//						largestDelta = velocityDelta > largestDelta ? velocityDelta : largestDelta;
//		//					}
//		//				}
//		//			}

//		//			for (int b = 0; b < nearfieldLeaves.Length; b++) {//asymmetric interaction
//		//				foreach (TParticle other in nearfieldLeaves[b].NodeElements) {
//		//					if (other.Enabled) {
//		//						totalImpulse = new double[Parameters.DIM];
//		//						toOther = other.LiveCoordinates.Subtract(particles[selfIdx].LiveCoordinates);
//		//						distance = toOther.Magnitude();

//		//						for (int fIdx = 0; fIdx < this.Forces.Length; fIdx++) {
//		//							impulse = this.Forces[fIdx].ComputeImpulse(distance, toOther, particles[selfIdx], other, out collision);
//		//							totalImpulse = totalImpulse.Add(impulse);
//		//							if (collision)
//		//								particles[selfIdx].NeighborNodeCollisions.Enqueue(other);
//		//						}
//		//						particles[selfIdx].NearfieldImpulse = particles[selfIdx].NearfieldImpulse.Add(totalImpulse);
//		//					}
//		//				}
//		//			}
//		//		}
//		//	}
//		//	return largestDelta;
//		//}
//	}
//}