using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Numerics;
using System.Threading;
using Generic.Trees;
using ParticleSimulator.Simulation.Baryon;
using ParticleSimulator.Simulation.Particles;
using ParticleSimulator.Simulation.Particles.Groups;

namespace ParticleSimulator.Simulation;

public abstract class ABinaryTreeSimulator<TParticle, TTree> : ASimulator<TParticle, TTree>
	where TParticle : AParticle<TParticle, TTree>
	where TTree : ABinaryTree<TTree, TParticle> {
	protected struct NodeParticles {
		public readonly TTree Node;
		public readonly TParticle[] Particles;

		public NodeParticles(TTree node, IEnumerable<TParticle> particles) {
			this.Node = node;
			this.Particles = [.. particles];
		}
	}

	private readonly CountdownEvent _cde = new(0);
	private readonly object _cdeLock = new();
	private readonly ConcurrentBag<Queue<NodeParticles>> _partitionedLeafData = new();

	public override ICollection<TParticle> Particles => this.Tree;

	public abstract TTree Tree { get; protected set; }
	protected abstract bool AccumulateTreeNodeData { get; }

	protected abstract void ComputeInteractions(NodeParticles leafParticles);
	protected virtual void AccumulateLeafNode(NodeParticles nodeParticles) => throw new NotSupportedException();
	protected virtual void AccumulateInnerNode(TTree node) => throw new NotSupportedException();
	protected virtual void PruneTreeTop() {
		this.Tree = (TTree)this.Tree.PruneTop();
	}
		
	public override void Init() {
		//initialize all the particles
		AParticleGroup<TParticle, TTree> group;
		for (int i = 0; i < Parameters.PARTICLES_GROUP_COUNT; i++) {
			group = this.NewParticleGroup();
			group.Init();
			this.Tree = (TTree)this.Tree.Add(group.InitialParticles);
		}
	}

	private void ValidateMembers() {
		foreach (var particle in this.Particles)
			System.Diagnostics.Debug.Assert(this.Tree.DoesEncompass(particle));

		foreach (var leaf in this.Tree.AllLeaves)
		foreach (var particle in leaf)
			System.Diagnostics.Debug.Assert(leaf.DoesEncompass(particle));
	}

	protected override List<ParticleData> Refresh() {
		//remove empty space from the top of the tree
		this.PruneTreeTop();

		//this.ValidateMembers(); // debugging

		//determine all leaves and aggregate barycenters in parallel
		this.PartitionAggregateSubtree(this.Tree);
		BaryCenter center = this.Center;//for escape velocity check

		//compute particle interactions on leaves in parallel (no movement occurs)
		this._cde.Reset(this._partitionedLeafData.Count);
		foreach (Queue<NodeParticles> nodeLeaves in this._partitionedLeafData)//do not consume yet
			ThreadPool.QueueUserWorkItem(this.LeafInteractionWorker, nodeLeaves);
		this._cde.Wait();

		//compute colliions serially
		Queue<TParticle> ready = new(this.Tree.Count);
		Queue<TParticle> collided = new();
		this.CheckLeaves(center, collided, ready);//also removes out-of-range particles
		HandleCollisions(collided, ready);
		//return a deep copy of the particle data for rendering so simulation can continue concurrently
		return this.IntegrateParticleMotion(ready);
	}

	private void CheckLeaves(BaryCenter center, Queue<TParticle> collided, Queue<TParticle> ready) {
		//collate all the leaves
		TParticle particle;
		TTree leaf;
		while (this._partitionedLeafData.TryTake(out Queue<NodeParticles> nodeLeaves)) {//random order
			while (nodeLeaves.TryDequeue(out NodeParticles leafParticles)) {
				leaf = leafParticles.Node;
				for (int i = 0; i < leafParticles.Particles.Length; i++) {
					particle = leafParticles.Particles[i];
					if (particle.Enabled && (Parameters.WORLD_PRUNE_RADII <= 0f || particle.IsInRange(center)))
						if (!(particle.Collisions is null) && particle.Collisions.Count > 0)
							collided.Enqueue(particle);
						else ready.Enqueue(particle);
					else leaf.RemoveFromLeaf(particle);//prune the particle
				}
			}
		}
	}

	private static void HandleCollisions(Queue<TParticle> collided, Queue<TParticle> ready) {
		//merge particles then collate remaining collision groups for drag forces
		Queue<Tuple<TParticle, Queue<TParticle>>> normalCollisions = new();

		//recursively merge particles
		bool anyConsumed;
		TTree node, otherNode;
		Queue<TParticle> remainder = new();//reuse when empty
		Vector<float> toOther;
		float distance, engulfRelativeDistance;//always recompute relative distances as they may change from mergers
		foreach (TParticle particle in collided) {
			if (particle.Enabled) {
				anyConsumed = false;
				node = particle.Node;

				while (particle.Collisions.TryDequeue(out TParticle other))
					if (other.Enabled) {
						toOther = other._position - particle._position;
						distance = MathF.Sqrt(Vector.Dot(toOther, toOther));
						engulfRelativeDistance = particle.EngulfRelativeDistance(other, distance);

						if (Parameters.MERGE_ENABLE && engulfRelativeDistance + Parameters.MERGE_ENGULF_RATIO <= 1f && !(other as MatterClump).IsCollapsed) {
							if (!anyConsumed) {
								anyConsumed = true;
								while (!node.IsLeaf)
									node = node.Children[node.ChildIndex(particle)];
							}

							particle.Consume(other);
							other.Enabled = false;
								
							otherNode = other.Node;
							while (!otherNode.IsLeaf)
								otherNode = otherNode.Children[otherNode.ChildIndex(other)];
							otherNode.RemoveFromLeaf(other, false);//defer leaf pruning

							if (!(other.Collisions is null))//have to consider other collided particle(s) again
								while (other.Collisions.TryDequeue(out TParticle tail))
									if (tail.Id != particle.Id)
										particle.Collisions.Enqueue(tail);
						} else remainder.Enqueue(other);//need to re-check collision again after all merging is complete
					}

				if (anyConsumed)
					particle.Node = node.MoveFromLeaf(particle, false);//defer leaf pruning
					
				if (remainder.Count > 0) {
					normalCollisions.Enqueue(new(particle, remainder));
					remainder = new();//reuse when unused
				} else ready.Enqueue(particle);
			}
		}

		//recheck and group remaining collisions
		if (normalCollisions.Count > 0) {
			Dictionary<TParticle, Dictionary<TParticle, float>> collisionDistances = new();//exactly one entry per collision pair, but could be split up
			TParticle particle;
			while (normalCollisions.TryDequeue(out Tuple<TParticle, Queue<TParticle>> t)) {
				particle = t.Item1;
				if (particle.Enabled) {
					ready.Enqueue(particle);

					while (t.Item2.TryDequeue(out TParticle other))
						if (other.Enabled)
							if (!(collisionDistances.ContainsKey(particle) && collisionDistances[particle].ContainsKey(other))
							    &&  !(collisionDistances.ContainsKey(other) && collisionDistances[other].ContainsKey(particle))) {
								toOther = other._position - particle._position;
								distance = MathF.Sqrt(Vector.Dot(toOther, toOther));
								engulfRelativeDistance = particle.EngulfRelativeDistance(other, distance);

								collisionDistances.TryAdd(particle, new());
								collisionDistances[particle][other] = engulfRelativeDistance;
							}
				}
			}

			Vector<float> impulse;
			foreach (KeyValuePair<TParticle, Dictionary<TParticle, float>> particleCollisionsKvp in collisionDistances)
			foreach (KeyValuePair<TParticle, float> collision in particleCollisionsKvp.Value)
				if (collision.Value < 1f) {
					impulse = particleCollisionsKvp.Key.ComputeCollisionImpulse(
						collision.Key,
						collision.Value >= 0f ? collision.Value : 0f);
					particleCollisionsKvp.Key.DragImpulse += impulse;
					collision.Key.DragImpulse -= impulse;
				}
		}
	}

	private List<ParticleData> IntegrateParticleMotion(Queue<TParticle> ready) {
		//move particles and add new ones
		List<ParticleData> results = new(this.Tree.Count);

		TTree leaf;
		while (ready.TryDequeue(out TParticle particle)) {
			if (particle.Enabled) {//will have already been removed in an earlier iteration if disabled
				leaf = particle.Node;
				while (!leaf.IsLeaf)
					leaf = leaf.Children[leaf.ChildIndex(particle)];
				//update particle information and location in tree in isolation of anything else affecting the tree
				particle.IntegrateMotion();
				leaf.MoveFromLeaf(particle, false);//defer leaf pruning
				results.Add(new(particle));
				//add any newborn particles
				if (!(particle.NewParticles is null))
					while (particle.NewParticles.TryDequeue(out TParticle birth)) {
						leaf.Add(birth);//presumably closer than the root
						results.Add(new(particle));
					}
			}
		}

		return results;
	}

	private void ResetParticles(TTree bin, Queue<NodeParticles> leaves) {
		TParticle[] leafParticles;
		leafParticles = new TParticle[bin.ItemCount];
		int idx = 0;
		foreach (TParticle particle in bin.Bin) {
			particle.Acceleration = particle.DragAcceleration = Vector<float>.Zero;//reset iteration data
			particle.Node = bin;
			leafParticles[idx++] = particle;
		}
		NodeParticles leafBin = new(bin, leafParticles);
		leaves.Enqueue(leafBin);
		//initialize the barycenter
		if (this.AccumulateTreeNodeData)
			this.AccumulateLeafNode(leafBin);
	}

	private void AggregateSubtreeBarycenters(TTree root, Queue<NodeParticles> leaves) {//recursively discover leaves and aggregate barycenters
		if (root.IsLeaf) {
			ResetParticles(root, leaves);
		} else {
			Stack<TTree[]> levelStack = this.AccumulateTreeNodeData ? new() : null;
			Stack<TTree> pendingNodes = new(), testNodes = new();
			//work down to leaf nodes with depth-first recursion
			pendingNodes.Push(root);
			TTree[] levelNodes; TTree child;
			do {
				while (pendingNodes.TryPop(out TTree node)) {
					for (int cIdx = 0; cIdx < node.Children.Length; cIdx++) {
						child = (TTree)node.Children[cIdx];
						if (child.ItemCount > 0)
							if (child.IsLeaf) ResetParticles(child, leaves);
							else testNodes.Push(child);//continue recursion (not at a leaf)
						else node.Children[cIdx].Children = null;//prune the leaves
					}
				}
				if (testNodes.Count > 0) {
					if (this.AccumulateTreeNodeData) {//copy layer for aggregation later
						levelNodes = new TTree[testNodes.Count];
						testNodes.CopyTo(levelNodes, 0);
						levelStack.Push(levelNodes);
					}
					//process next layer deeper in the tree
					(pendingNodes, testNodes) = (testNodes, pendingNodes);
				}
			} while (pendingNodes.Count > 0);
			//aggregate barycenters from bottom-up
			if (this.AccumulateTreeNodeData) {
				while (levelStack.TryPop(out levelNodes))
					for (int i = 0; i < levelNodes.Length; i++)
						this.AccumulateInnerNode(levelNodes[i]);
				//finish him
				this.AccumulateInnerNode(root);
			}
		}
	}

	private bool IsBelowPartitioningThreshold(int filled, int testAddend) =>
		filled + testAddend <= Parameters.TREE_BATCH_SIZE
		|| (double)((filled + testAddend) - Parameters.TREE_BATCH_SIZE) / Parameters.TREE_BATCH_SIZE < Parameters.TREE_BATCH_SLACK;

	private void PartitionAggregateSubtree(TTree root) {
		Queue<TTree> work = new();
		if (root.IsLeaf || Parameters.TREE_BATCH_SIZE < 1 || root.Count <= Parameters.TREE_BATCH_SIZE) {
			work.Enqueue(root);
			this._cde.Reset(1);
			ThreadPool.QueueUserWorkItem(this.SubtreeAggregationWorker, work);
			this._cde.Wait();
		} else {
			int numFilled = 0;
			Stack<TTree[]> levelStack = this.AccumulateTreeNodeData ? new() : null;
			Stack<TTree> pendingNodes = new(), testNodes = new();
			//work down to leaf nodes with depth-first recursion
			pendingNodes.Push(root);
			TTree[] levelNodes; TTree child;
			do {
				while (pendingNodes.TryPop(out TTree node)) {
					for (int cIdx = 0; cIdx < node.Children.Length; cIdx++) {
						child = (TTree)node.Children[cIdx];
						if (child.ItemCount > 0) {
							if (child.IsLeaf || IsBelowPartitioningThreshold(numFilled, child.Count))
							{//split further when too far over capacity
								work.Enqueue(child);
								numFilled += child.ItemCount;
								if (numFilled >= Parameters.TREE_BATCH_SIZE) {//dispatch workload
									EnqueueSubtreeAggregation(work);
									work = new();//must create a new instance (handing off work)
									numFilled = 0;
								}
							} else testNodes.Push(child);//continue recursion (accumulating work in partition)
						} else node.Children[cIdx].Children = null;//prune the leaves
					}
				}
				if (testNodes.Count > 0) {
					if (this.AccumulateTreeNodeData) {//copy layer for aggregation later
						levelNodes = new TTree[testNodes.Count];
						testNodes.CopyTo(levelNodes, 0);
						levelStack.Push(levelNodes);
					}
					//process next layer deeper in the tree
					(pendingNodes, testNodes) = (testNodes, pendingNodes);
				}
			} while (pendingNodes.Count > 0);
			//dispatch any leftover workload and wait for finish
			if (work.Count > 0)
				EnqueueSubtreeAggregation(work);
			this._cde.Wait();
			//aggregate barycenters from bottom-up
			if (this.AccumulateTreeNodeData) {
				while (levelStack.TryPop(out levelNodes))
					for (int i = 0; i < levelNodes.Length; i++)
						this.AccumulateInnerNode(levelNodes[i]);
				//finish him
				this.AccumulateInnerNode(root);
			}
		}
	}

	private void EnqueueSubtreeAggregation(Queue<TTree> work) {
		lock (this._cdeLock)
			if (this._cde.IsSet)
				this._cde.Reset(1);
			else this._cde.AddCount();
		ThreadPool.QueueUserWorkItem(this.SubtreeAggregationWorker, work);
	}

	private void SubtreeAggregationWorker(object work) {
		Queue<NodeParticles> result = new((int)(Parameters.TREE_BATCH_SIZE * (1f + Parameters.TREE_BATCH_SLACK)));

		Queue<TTree> nodes = (Queue<TTree>)work;
		while (nodes.TryDequeue(out TTree node))
			this.AggregateSubtreeBarycenters(node, result);

		this._partitionedLeafData.Add(result);

		lock (this._cdeLock)
			this._cde.Signal();
	}

	private void LeafInteractionWorker(object work) {
		Queue<NodeParticles> nodeLeaves = (Queue<NodeParticles>)work;
		foreach (NodeParticles leaf in nodeLeaves)//do not consume the queue
			this.ComputeInteractions(leaf);

		this._cde.Signal();
	}
}