using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading;
using Generic.Trees;
using ParticleSimulator.Simulation.Baryon;
using ParticleSimulator.Simulation.Particles;

namespace ParticleSimulator.Simulation {
	public abstract class ABinaryTreeSimulator<TParticle, TTree> : ASimulator<TParticle>
	where TParticle : AParticle<TParticle>
	where TTree : ABinaryTree<TParticle> {
		private readonly CountdownEvent _cde = new(0);
		private readonly object _cdeLock = new();
		private ConcurrentBag<Queue<Tuple<TTree, TParticle[]>>> _partitionedLeafData = new();

		public override ICollection<TParticle> Particles => this.Tree;

		public abstract TTree Tree { get; protected set; }
		protected abstract bool AccumulateTreeNodeData { get; }

		protected abstract void ComputeInteractions(TTree leaf, TParticle[] particles);
		protected virtual void AccumulateLeafNode(TTree node, TParticle[] particles) => throw null;
		protected virtual void AccumulateInnerNode(TTree node) => throw null;
		protected virtual void PruneTreeTop() {
			this.Tree = (TTree)this.Tree.PruneTop();
		}
		
		public override void Init() {
			this.Tree = (TTree)this.Tree.Add(
				this.ParticleGroups.SelectMany(g => g.InitialParticles));
		}

		protected override List<ParticleData> Refresh() {
			//remove empty space from the top of the tree
			this.PruneTreeTop();

			//determine all leaves and aggregate barycenters in parallel
			this.AggregateSubtree(this.Tree, true, null);
			BaryCenter center = this.Center;//for escape velocity check

			//compute particle interactions on leaves in parallel (no movement occurs)
			this._cde.Reset(this._partitionedLeafData.Count);
			foreach (Queue<Tuple<TTree, TParticle[]>> nodeLeaves in this._partitionedLeafData)//do not consume yet
				ThreadPool.QueueUserWorkItem(this.LeafInteractionWorker, nodeLeaves);
			this._cde.Wait();

			Queue<Tuple<TParticle, ABinaryTree<TParticle>>>
				collisions = new(),
				ready = new(this.ParticleCount);

			this.DiscoverLeaves(center, collisions, ready);
			int births = this.HandleCollisions(collisions, ready);
			//return a deep copy of the particle data for rendering so simulation can continue concurrently
			return this.MoveParticles(center, ready, births);
		}

		private void DiscoverLeaves(BaryCenter center, Queue<Tuple<TParticle, ABinaryTree<TParticle>>> collisions, Queue<Tuple<TParticle, ABinaryTree<TParticle>>> ready) {
			//collate all the leaves
			TParticle particle;
			ABinaryTree<TParticle> leaf;
			while (this._partitionedLeafData.TryTake(out Queue<Tuple<TTree, TParticle[]>> nodeLeaves)) {//random order
				while (nodeLeaves.TryDequeue(out Tuple<TTree, TParticle[]> leafParticles)) {
					leaf = leafParticles.Item1;
					for (int i = 0; i < leafParticles.Item2.Length; i++) {
						particle = leafParticles.Item2[i];
						if (Parameters.WORLD_PRUNE_RADII <= 0f || particle.IsInRange(center)) {
							if (!(particle.Collisions is null) && particle.Collisions.Count > 0)
								collisions.Enqueue(new(particle, leaf));
							else ready.Enqueue(new(particle, leaf));
						} else leaf.RemoveFromLeaf(particle);
					}
				}
			}
		}

		private int HandleCollisions(Queue<Tuple<TParticle, ABinaryTree<TParticle>>> collisions, Queue<Tuple<TParticle, ABinaryTree<TParticle>>> ready) {
			int births = 0;
			Queue<Tuple<TParticle, ABinaryTree<TParticle>, Queue<TParticle>>> normalCollisions = new();
			//recursively merge particles
			bool moved;
			float distance, engulfRelativeDistance;
			TParticle particle;
			ABinaryTree<TParticle> leaf;
			Queue<TParticle> remainder;
			while (collisions.TryDequeue(out Tuple<TParticle, ABinaryTree<TParticle>> t)) {
				particle = t.Item1;
				if (particle.Enabled) {
					leaf = t.Item2;
					while (!leaf.IsLeaf)
						leaf = leaf.Children[leaf.ChildIndex(particle)];

					moved = false;
					remainder = new();
					while (particle.Collisions.TryDequeue(out TParticle other)) {
						if (other.Enabled) {
							Vector<float> toOther = other._position - particle._position;
							distance = MathF.Sqrt(Vector.Dot(toOther, toOther));
							engulfRelativeDistance = particle.EngulfRelativeDistance(other, distance);
							if (Parameters.MERGE_ENABLE && engulfRelativeDistance + Parameters.MERGE_ENGULF_RATIO <= 1f) {
								moved = true;
								particle.Consume(other);//adds other's collied particle(s) back
								t.Item2.FindContainingLeaf(other).RemoveFromLeaf(other, false);//defer leaf pruning
							} else remainder.Enqueue(other);
						}
					}
					if (moved)
						leaf = leaf.MoveFromLeaf(particle, false);//defer leaf pruning
					normalCollisions.Enqueue(new(particle, leaf, remainder));
				}
			}
			//collide remainder
			while (normalCollisions.TryDequeue(out Tuple<TParticle, ABinaryTree<TParticle>, Queue<TParticle>> t)) {
				particle = t.Item1;
				if (particle.Enabled) {
					ready.Enqueue(new(particle, t.Item2));

					if (!(particle.NewParticles is null))
						births += particle.NewParticles.Count;

					while (t.Item3.TryDequeue(out TParticle other)) {
						if (other.Enabled) {
							Vector<float> toOther = other._position - particle._position;
							distance = MathF.Sqrt(Vector.Dot(toOther, toOther));
							if (distance > Parameters.PRECISION_EPSILON) {
								engulfRelativeDistance = particle.EngulfRelativeDistance(other, distance);
								if (engulfRelativeDistance < 1f)
									particle.Impulse += particle.ComputeCollisionImpulse(other, engulfRelativeDistance);;
							}
						}
					}
				}
			}

			return births;
		}

		private List<ParticleData> MoveParticles(BaryCenter center, Queue<Tuple<TParticle, ABinaryTree<TParticle>>> ready, int births) {
			//refresh particle and tree location data
			List<ParticleData> results = new(ready.Count + births);

			TParticle particle;
			ABinaryTree<TParticle> leaf;
			while (ready.TryDequeue(out Tuple<TParticle, ABinaryTree<TParticle>> t)) {
				particle = t.Item1;
				if (particle.Enabled) {//will have already been removed in an earlier iteration if disabled
					leaf = t.Item2;
					while (!leaf.IsLeaf)
						leaf = leaf.Children[leaf.ChildIndex(particle)];
					//update particle information and location in tree in isolation of anything else affecting the tree
					particle.ApplyTimeStep(center);
					leaf.MoveFromLeaf(particle, false);//defer leaf pruning
					results.Add(new(particle));
					//add any newborn particles
					if (!(particle.NewParticles is null))
						while (particle.NewParticles.TryDequeue(out TParticle birth)) {
							leaf.Add(birth);//hopefully closer than the root
							results.Add(new(particle));
						}
				}
			}

			return results;
		}

		private void AggregateSubtree(TTree root, bool isPrep, Queue<Tuple<TTree, TParticle[]>> leaves) {//recursively discover leaves and aggregate barycenters
			//identical recursive behavior is needed for both prep and "normal" modes, so this remains one convoluted method with all the conditionals split...
			if (isPrep && (root.IsLeaf || Parameters.TREE_BATCH_SIZE < 1 || root.Count <= Parameters.TREE_BATCH_SIZE)) {
				Queue<TTree> work = new();
				work.Enqueue(root);
				this._cde.Reset(1);
				ThreadPool.QueueUserWorkItem(this.SubtreeAggregationWorker, work);
				this._cde.Wait();
			} else if (root.IsLeaf) {//and not prep mode
				TParticle[] leafParticles = new TParticle[root.ItemCount];
				int idx = 0;
				foreach (TParticle particle in root.Bin) {
					particle.Acceleration = Vector<float>.Zero;
					leafParticles[idx++] = particle;
				}
				leaves.Enqueue(new(root, leafParticles));
				if (this.AccumulateTreeNodeData)
					this.AccumulateLeafNode(root, leafParticles);
			} else {//not leaf
				int idx, numFilled = 0;
				Queue<TTree> work = isPrep ? new() : null;
				Stack<TTree[]> levelStack = this.AccumulateTreeNodeData ? new() : null;
				Stack<TTree> pendingNodes = new(), testNodes = new();
				//work down to leaf nodes with depth-first recursion
				pendingNodes.Push(root);
				TTree[] levelNodes; TTree child; TParticle[] leafParticles;
				do {
					while (pendingNodes.TryPop(out TTree node)) {
						for (int cIdx = 0; cIdx < node.Children.Length; cIdx++) {
							child = (TTree)node.Children[cIdx];
							if (child.ItemCount > 0) {
								if (isPrep && child.IsLeaf) {
									work.Enqueue(child);
									numFilled += child.ItemCount;
									//not worth splitting a new worker if near capacity already
									if (numFilled >= Parameters.TREE_BATCH_SIZE) {//dispatch workload
										lock (this._cdeLock)
											if (this._cde.IsSet)
												this._cde.Reset(1);
											else this._cde.AddCount();
										ThreadPool.QueueUserWorkItem(this.SubtreeAggregationWorker, work);
										work = new();
										numFilled = 0;
									}
								} else if (child.IsLeaf) {//and not prep mode
									leafParticles = new TParticle[child.ItemCount];
									idx = 0;
									foreach (TParticle particle in child.Bin) {
										particle.Acceleration = Vector<float>.Zero;
										leafParticles[idx++] = particle;
									}
									leaves.Enqueue(new(child, leafParticles));
									//initialize the barycenter
									if (this.AccumulateTreeNodeData)
										this.AccumulateLeafNode(child, leafParticles);
								} else if (isPrep//and not leaf
								&& (numFilled + child.Count <= Parameters.TREE_BATCH_SIZE
									|| (double)((numFilled + child.Count) - Parameters.TREE_BATCH_SIZE) / Parameters.TREE_BATCH_SIZE < Parameters.TREE_BATCH_SLACK))
								{//skip enqueueing work, to split further when too far over capacity
									work.Enqueue(child);
									numFilled += child.ItemCount;
									if (numFilled >= Parameters.TREE_BATCH_SIZE) {//dispatch workload
										lock (this._cdeLock)
											if (this._cde.IsSet)
												this._cde.Reset(1);
											else this._cde.AddCount();
										ThreadPool.QueueUserWorkItem(this.SubtreeAggregationWorker, work);
										work = new();
										numFilled = 0;
									}
								} else testNodes.Push(child);//continue recursion
							} else node.Children[cIdx].Children = null;//prune the leaves
						}
					}
					if (testNodes.Count > 0) {
						if (this.AccumulateTreeNodeData) {//copy layer for aggregation later
							levelNodes = new TTree[testNodes.Count];
							testNodes.CopyTo(levelNodes, 0);//casting magic?
							levelStack.Push(levelNodes);
						}
						//process next layer deeper in the tree
						(pendingNodes, testNodes) = (testNodes, pendingNodes);
					}
				} while (pendingNodes.Count > 0);
				//wait for child aggregations to finish
				if (isPrep) {
					if (work.Count > 0) {//dispatch any leftover workload
						lock (this._cdeLock)
							if (this._cde.IsSet)
								this._cde.Reset(1);
							else this._cde.AddCount();
						ThreadPool.QueueUserWorkItem(this.SubtreeAggregationWorker, work);
					}
					this._cde.Wait();
				}
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

		private void SubtreeAggregationWorker(object work) {
			Queue<Tuple<TTree, TParticle[]>> result = new((int)(Parameters.TREE_BATCH_SIZE * (1f + Parameters.TREE_BATCH_SLACK)));

			Queue<TTree> nodes = (Queue<TTree>)work;
			while (nodes.TryDequeue(out TTree node))
				this.AggregateSubtree(node, false, result);//"normal" mode

			this._partitionedLeafData.Add(result);

			lock (this._cdeLock)
				this._cde.Signal();
		}

		private void LeafInteractionWorker(object work) {
			Queue<Tuple<TTree, TParticle[]>> nodeLeaves = (Queue<Tuple<TTree, TParticle[]>>)work;
			foreach (Tuple<TTree, TParticle[]> leaf in nodeLeaves)//do not consume the queue
				this.ComputeInteractions(leaf.Item1, leaf.Item2);

			this._cde.Signal();
		}
	}
}