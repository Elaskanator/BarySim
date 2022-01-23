using System.Collections.Generic;
using System.Numerics;
using ParticleSimulator.Simulation.Particles;

namespace ParticleSimulator.Simulation.Baryon {
	public class BaryonSimulator : ABinaryTreeSimulator<MatterClump, BarnesHutTree> {
		public BaryonSimulator(int dim) {
			this._tree = new(dim);
		}
		
		private readonly object _lock = new();
		private BarnesHutTree _tree;
		public override BarnesHutTree Tree {
			get {
				lock (this._lock)
					return this._tree;
			} protected set {
				lock (this._lock)
					this._tree = value;
			}}
		public override BaryCenter Center => this.Tree.MassBaryCenter;
		protected override bool AccumulateTreeNodeData => true;

		protected override AParticleGroup<MatterClump> NewParticleGroup() =>
			new SpinningDisk<MatterClump>((p, v) => new(p, v), Parameters.GALAXY_RADIUS);
		protected override void AccumulateLeafNode(BarnesHutTree node, MatterClump[] particles) =>
			node.InitBaryCenter(particles);
		protected override void AccumulateInnerNode(BarnesHutTree node) =>
			node.UpdateBaryCenter();
		protected override void PruneTreeTop() {
			BaryCenter center = this._tree.MassBaryCenter;
			lock (this._lock) {//prevents camera autofollowing from tweaking out if the tree shrinks
				this._tree = (BarnesHutTree)this._tree.PruneTop();
				this._tree.MassBaryCenter = center;
			}
		}

		protected override void ComputeInteractions(BarnesHutTree leaf, MatterClump[] particles) {
			List<MatterClump> nearField = new();
			Vector<float> farFieldAcceleration = leaf.DetermineNeighbors(nearField);
			Vector<float> impulse;

			for (int i = 0; i < particles.Length; i++) {
				//add weaker forces first to reduce floating point errors
				for (int n = 0; n < nearField.Count; n++) {
					impulse = particles[i].ComputeInteractionImpulse(nearField[n]);
					particles[i].Impulse += impulse;
				}
				for (int j = 0; j < i; j++) {
					impulse = particles[i].ComputeInteractionImpulse(particles[j]);
					particles[i].Impulse += impulse;
					particles[j].Impulse -= impulse;
				}
				//add last to reduce floating point errors
				particles[i].Acceleration += farFieldAcceleration;//cheeky optimization to skip impulse/mass conversion
			}
		}
	}
}