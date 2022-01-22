using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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

		protected override void Refresh() {
			//remove empty space from the top of the tree
			this.PruneTreeTop();
			//determine all leaves and aggregate barycenters in parallel
			this.AggregateSubtree(this.Tree, true, null);
			//compute particle interactions on leaves in parallel (no movement occurs)
			this._cde.Reset(this._partitionedLeafData.Count);
			foreach (Queue<Tuple<TTree, TParticle[]>> nodeLeaves in this._partitionedLeafData)//do not consume yet
				ThreadPool.QueueUserWorkItem(this.LeafInteractionWorker, nodeLeaves);
			this._cde.Wait();
			//refresh particle and tree location data
			BaryCenter center = this.Center;//for escape velocity check
			while (this._partitionedLeafData.TryTake(out Queue<Tuple<TTree, TParticle[]>> nodeLeaves))//random order
				while (nodeLeaves.TryDequeue(out Tuple<TTree, TParticle[]> leaf))
					this.RefreshLeafParticles(center, leaf.Item1, leaf.Item2);
		}

		private void RefreshLeafParticles(BaryCenter center, TTree originLeaf, TParticle[] particles) {
			ABinaryTree<TParticle> leaf;
			TParticle particle;
			for (int i = 0; i < particles.Length; i++) {
				particle = particles[i];
				if (particle.Enabled) {//will have already been removed in an earlier iteration if disabled
					leaf = originLeaf;
					while (!leaf.IsLeaf)
						leaf = leaf.Children[leaf.ChildIndex(particle)];
					//recursively consume merge chain
					if (!(particle.Mergers is null))
						while (particle.Mergers.TryDequeue(out TParticle other))
							if (other.Enabled) {
								other.Enabled = false;
								originLeaf.GetContainingLeaf(other).RemoveFromLeaf(other, false);//defer leaf pruning
								//ingest the other particle's information
								particle.Consume(other);
								if (!(other.Mergers is null))
									while (other.Mergers.TryDequeue(out TParticle tail))
										if (particle.Id != tail.Id)
											particle.Mergers.Enqueue(tail);
							}
					//update particle information and location in tree in isolation of anything else affecting the tree
					particle.ApplyTimeStep(center);
					if (particle.Enabled)
						leaf.MoveFromLeaf(particle, false);//defer leaf pruning
					else leaf.RemoveFromLeaf(particle, false);//defer leaf pruning
					//finally add any newborn particles
					if (!(particle.NewParticles is null))
						while (particle.NewParticles.TryDequeue(out TParticle other))
							originLeaf.Add(other);//closer than new leaf position?
				}
			}
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
					particle.Acceleration = particle.Drag = Vector<float>.Zero;
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
										particle.Acceleration = particle.Drag = Vector<float>.Zero;
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