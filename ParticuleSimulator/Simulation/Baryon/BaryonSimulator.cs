using System;
using System.Collections.Generic;
using System.Numerics;
using Generic.Trees;
using ParticleSimulator.Simulation.Particles;

namespace ParticleSimulator.Simulation.Baryon {
	public class BaryonSimulator : ABinaryTreeSimulator<MatterClump, BarnesHutTree> {
		public BaryonSimulator() : base(new(Parameters.DIM)) { }

		public override Vector<float> Center {
			get { lock (this._lock)
				return this.ParticleTree.MassBaryCenter.Position; } }

		protected override bool AccumulateTreeNodeData => true;

		private readonly object _lock = new();

		protected override AParticleGroup<MatterClump> NewParticleGroup() =>
			new SpinningDisk<MatterClump>((p, v) => new(p, v), Parameters.GALAXY_RADIUS);

		protected override void ComputeLeafNode(BarnesHutTree child, MatterClump[] particles) =>
			child.InitBaryCenter(particles);
		protected override void ComputeInnerNode(BarnesHutTree node) =>
			node.UpdateBaryCenter();

		protected override BarnesHutTree PruneTree() {
			BaryCenter center = this.ParticleTree.MassBaryCenter;
			BarnesHutTree result;
			lock (this._lock) {
				result = (BarnesHutTree)this.ParticleTree.Prune();
				result.MassBaryCenter = center;
			}
			return result;
		}

		protected override void ProcessLeaf(BarnesHutTree leaf, MatterClump[] particles) {
			Vector<float> farFieldContribution = Vector<float>.Zero;
			List<MatterClump> nearField = new();
			Queue<BarnesHutTree> farField = new();
			this.DetermineNeighbors(leaf, nearField, farField);

			float distanceSquared;
			Vector<float> toOther;
			BarnesHutTree otherNode;
			while (farField.TryDequeue(out otherNode)) {
				toOther = otherNode.MassBaryCenter.Position - leaf.MassBaryCenter.Position;
				distanceSquared = Vector.Dot(toOther, toOther);
				if (distanceSquared > Parameters.WORLD_EPSILON)
					farFieldContribution += toOther * (otherNode.MassBaryCenter.Weight / distanceSquared);
			}
			farFieldContribution *= Parameters.GRAVITATIONAL_CONSTANT;

			Tuple<Vector<float>, Vector<float>> influence;
			for (int i = 0; i < particles.Length; i++) {
				particles[i].Acceleration = Vector<float>.Zero;
				for (int j = 0; j < i; j++) {
					influence = particles[i].ComputeInfluence(particles[j]);
					particles[i].Acceleration += particles[j].Mass*influence.Item1 + influence.Item2*(1f/particles[i].Mass);
					particles[j].Acceleration -= particles[i].Mass*influence.Item1 + influence.Item2*(1f/particles[j].Mass);
				}
				for (int n = 0; n < nearField.Count; n++) {
					influence = particles[i].ComputeInfluence(nearField[n]);
					particles[i].Acceleration += nearField[n].Mass*influence.Item1 + influence.Item2*(1f/particles[i].Mass);
				}
				particles[i].Acceleration += farFieldContribution;//add after to reduce floating point errors
			}
		}

		private void DetermineNeighbors(BarnesHutTree leaf, List<MatterClump> nearField, Queue<BarnesHutTree> farField) {
			BarnesHutTree other;

			/*
			//top down approach
			Queue<BarnesHutTree> remaining = new();
			remaining.Enqueue(this.ParticleTree);

			BarnesHutTree node;
			while (remaining.TryDequeue(out node))
				if (node.IsLeaf) {
					if (!ReferenceEquals(evalNode, node))
						nearField.AddRange(node.Bin);
				} else for (int c = 0; c < node.Children.Length; c++)
					if (node.Children[c].ItemCount > 0) {
						other = (BarnesHutTree)node.Children[c];
						if (evalNode.CanApproximate(other))//how are we guaranteed to not approximate a parent node? I don't like this
							farField.Enqueue(other);
						else remaining.Enqueue(other);
					}
			*/
			
			//bottom up approach
			Queue<BarnesHutTree> remaining = new();
			ATree<MatterClump> node = leaf, lastNode;
			BarnesHutTree child;
			while (!node.IsRoot) {
				lastNode = node;
				node = node.Parent;
				for (int i = 0; i < node.Children.Length; i++)
					if (node.Children[i].ItemCount > 0 && !ReferenceEquals(lastNode, node.Children[i])) {
						child = (BarnesHutTree)node.Children[i];
						if (leaf.CanApproximate(child))
							farField.Enqueue(child);
						else remaining.Enqueue(child);
					}

				while (remaining.TryDequeue(out other))
					if (other.IsLeaf)
						if (leaf.CanApproximate(other))
							farField.Enqueue(other);
						else nearField.AddRange(other.Bin);
					else for (int i = 0; i < other.Children.Length; i++)
						if (other.Children[i].ItemCount > 0) {
							child = (BarnesHutTree)other.Children[i];
							if (leaf.CanApproximate(child))
								farField.Enqueue(child);
							else remaining.Enqueue(child);
						}
			}
		}
	}
}