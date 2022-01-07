using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Generic.Extensions;
using Generic.Models.Trees;
using ParticleSimulator.Simulation.Particles;

namespace ParticleSimulator.Simulation.Baryon {
	public class BaryonSimulator : ISimulator {//modified Barnes-Hut Algorithm
		public BaryonSimulator() { }

		public int IterationCount { get; private set; }
		public Galaxy[] InitialParticleGroups { get; private set; }
		public BarnesHutTree ParticleTree { get; private set; }
		ITree ISimulator.ParticleTree => this.ParticleTree;
		IEnumerable<IParticle> ISimulator.Particles => this.ParticleTree.AsEnumerable();
		public int ParticleCount => this.ParticleTree is null ? 0 : this.ParticleTree.Count;//this.ParticleTree is null ? 0 : this.ParticleTree.Count;

		public void Init() {
			this.IterationCount = -1;
			this.InitialParticleGroups = Enumerable
				.Range(0, Parameters.PARTICLES_GROUP_COUNT)
				.Select(i => new Galaxy())
				.ToArray();

			ATree<MatterClump> node = new BarnesHutTree(Parameters.DIM);
			node.Add(this.InitialParticleGroups.SelectMany(g => g.InitialParticles));
			this.ParticleTree = (BarnesHutTree)node.Root;
		}

		public ParticleData[] RefreshSimulation() {
			this.IterationCount++;
			if (this.ParticleTree.Count > 0) {
				if (this.IterationCount > 0) {//show starting data on first result
					this.ComputeAcceleration(Parameters.TIME_SCALE);
					if (Parameters.MERGE_ENABLE)
						this.HandleMergers();
					//trim top of tree
					ATree<MatterClump> root = this.ParticleTree.Root;
					int count, idx;
					while (!root.IsLeaf) {
						count = idx = 0;
						for (int i = 0; i < root.Children.Length; i++)
							if (root.Children[i].Count > 0)
								if (++count > 1) break;
								else idx = i;
						if (count == 1)
							root = root.Children[idx];
						else break;
					}
					root.Parent = null;
					this.ParticleTree = (BarnesHutTree)root;
				}
				return this.ParticleTree.Select(p => new ParticleData(p)).ToArray();
			} else {
				Program.CancelAction(null, null);
				return Array.Empty<ParticleData>();
			}
		}

		private void ComputeAcceleration(float timeStep) {
			Queue<ATree<MatterClump>> pendingNodes = new(), testNodes = new();
			Stack<BarnesHutTree[]> levelStack = new();

			pendingNodes.Enqueue(this.ParticleTree);
			levelStack.Push(new BarnesHutTree[] { this.ParticleTree });

			BarnesHutTree[] levelNodes;
			ATree<MatterClump> node;
			bool any = true;
			while (any) {
				any = false;
				while (pendingNodes.TryDequeue(out node))
					if (node.Count > 0 && !node.IsLeaf)
						for (int cIdx = 0; cIdx < node.Children.Length; cIdx++)
							if (node.Children[cIdx].Count > 0) {
								any = true;
								testNodes.Enqueue(node.Children[cIdx]);
							}
				if (any) {
					levelNodes = new BarnesHutTree[testNodes.Count];
					testNodes.CopyTo(levelNodes, 0);//casting magic?
					levelStack.Push(levelNodes);

					(pendingNodes, testNodes) = (testNodes, pendingNodes);
				}
			}

			Tuple<BarnesHutTree, MatterClump[]>[] leaves = new Tuple<BarnesHutTree, MatterClump[]>[this.ParticleTree.Count];
			int lIdx = 0;
			while (levelStack.TryPop(out levelNodes))
				for (int i = 0; i < levelNodes.Length; i++) {
					levelNodes[i].UpdateBarycenter();
					if (levelNodes[i].IsLeaf)
						leaves[lIdx++] = new(levelNodes[i], levelNodes[i].Bin.ToArray());
				}

			for (int i = 0; i < lIdx; i++)
				this.ProcessLeaf(leaves[i]);

			//var crap1 =
			//	this.ParticleTree
			//	.Where(p => p.NearfieldInteractionCounts.Count > 0)
			//	.Select(p =>
			//		new { Id = p.Id,
			//			Groups = 
			//				p.NearfieldInteractionCounts
			//				.GroupBy(kvp => kvp.Value)
			//				.OrderByDescending(g => g.Key) })
			//	.OrderByDescending(pg => pg.Groups.First().Key)
			//	.ThenBy(pg => pg.Id)
			//	.Select(pg => string.Format("{0} => {1}",
			//		pg.Id,
			//		string.Join(" ", 
			//			pg.Groups
			//				.Select(g => string.Format("[{0}:{1}]",
			//					g.Key,
			//					g.Count()))
			//				.ToArray())))
			//	.ToArray();

			for (int i = 0; i < lIdx; i++)
				for (int p = 0; p < leaves[i].Item2.Length; p++) {
					if (leaves[i].Item2[p].Enabled) {
						node = leaves[i].Item1.GetContainingLeaf(leaves[i].Item2[p]);
						leaves[i].Item2[p].ApplyTimeStep(timeStep, this.ParticleTree);

						//leaves[i].Item2[p].NearfieldInteractionCounts.Clear();///////////////

						node.MoveFromLeaf(leaves[i].Item2[p]);
					}
				}
		}

		private void ProcessLeaf(Tuple<BarnesHutTree, MatterClump[]> leafData) {
			Queue<BarnesHutTree> remaining = new(),
				farField = new();
			List<MatterClump> nearField = new();

			ATree<MatterClump> node = leafData.Item1, lastNode;
			BarnesHutTree other, child;
			while (!node.IsRoot) {
				lastNode = node;
				node = node.Parent;
				for (int i = 0; i < node.Children.Length; i++) {
					if (node.Children[i].Count > 0)
						if (!ReferenceEquals(lastNode, node.Children[i])) {
							child = (BarnesHutTree)node.Children[i];
							if (child.MassBaryCenter.Weight > 0f) {
								if (leafData.Item1.CanApproximate(child))
									farField.Enqueue(child);
								else remaining.Enqueue(child);
							}
						}
				}

				//nearField.AddRange(remaining.SelectMany(f => f));
				
				while (remaining.TryDequeue(out other))
					if (other.IsLeaf) {
						if (leafData.Item1.CanApproximate(other))
							farField.Enqueue(other);
						else foreach (MatterClump p in other.Bin)
							nearField.Add(p);
					} else for (int i = 0; i < other.Children.Length; i++)
						if (other.Children[i].Count > 0) {
							child = (BarnesHutTree)other.Children[i];
							if (leafData.Item1.CanApproximate(child))
								farField.Enqueue(child);
							else remaining.Enqueue(child);
						}
				
			}

			Vector<float> farFieldContribution = Vector<float>.Zero;

			float distSq;
			Vector<float> toOther;
			while (farField.TryDequeue(out other)) {
				toOther = other.MassBaryCenter.Position - leafData.Item1.MassBaryCenter.Position;
				distSq = Vector.Dot(toOther, toOther);
				if (distSq > Parameters.WORLD_EPSILON)
					farFieldContribution += toOther * (other.MassBaryCenter.Weight / distSq);
			}
			farFieldContribution *= Parameters.GRAVITATIONAL_CONSTANT;

			Tuple<Vector<float>, Vector<float>> influence;
			for (int i = 0; i < leafData.Item2.Length; i++) {
				leafData.Item2[i].Acceleration = farFieldContribution;
				for (int j = i + 1; j < leafData.Item2.Length; j++) {
					if (leafData.Item2[j].Mass > 0f) {
						influence = leafData.Item2[i].ComputeInfluence(leafData.Item2[j]);
						leafData.Item2[i].Acceleration += leafData.Item2[j].Mass*influence.Item1 + influence.Item2*(1f/leafData.Item2[i].Mass);
						leafData.Item2[j].Acceleration -= leafData.Item2[i].Mass*influence.Item1 + influence.Item2*(1f/leafData.Item2[j].Mass);
					}
				}
				for (int n = 0; n < nearField.Count; n++) {
					//leafData.Item2[i].NearfieldInteractionCounts.TryAdd(nearField[n].Id, 0);////////////
					//leafData.Item2[i].NearfieldInteractionCounts[nearField[n].Id]++;////////////////
					//nearField[n].NearfieldInteractionCounts.TryAdd(leafData.Item2[i].Id, 0);////////////
					//nearField[n].NearfieldInteractionCounts[leafData.Item2[i].Id]++;////////////////

					influence = leafData.Item2[i].ComputeInfluence(nearField[n]);
					leafData.Item2[i].Acceleration += nearField[n].Mass*influence.Item1 + influence.Item2*(1f/leafData.Item2[i].Mass);
				}
			}
		}

		private void HandleMergers() {
			MatterClump other, tail;

			Queue<MatterClump> pending = new();
			HashSet<MatterClump> evaluated = new();
			MatterClump[] originalParticles = this.ParticleTree.AsArray();
			ATree<MatterClump> leaf;
			for (int i = 0; i < originalParticles.Length; i++) {
				if (originalParticles[i].IsInRange) {
					if (originalParticles[i].Enabled && evaluated.Add(originalParticles[i])) {
						pending.Clear();
						while (originalParticles[i].Mergers.TryDequeue(out other))
							if (other.Enabled)
								pending.Enqueue(other);

						while (pending.TryDequeue(out other))
							if (evaluated.Add(other)) {
								leaf = this.ParticleTree.GetContainingLeaf(originalParticles[i]);
								originalParticles[i].Incorporate(other);
								leaf.MoveFromLeaf(originalParticles[i]);
								this.ParticleTree.Remove(other);
								while (other.Mergers.TryDequeue(out tail))
									if (tail.Enabled)
										pending.Enqueue(tail);
							}
					}
				} else this.ParticleTree.Remove(originalParticles[i]);

				if (originalParticles[i].NewParticles.Count > 0) {
					while (originalParticles[i].NewParticles.TryDequeue(out other))
						this.ParticleTree.Add(other);
					this.ParticleTree = (BarnesHutTree)this.ParticleTree.Root;
				}
			}
		}
	}
}