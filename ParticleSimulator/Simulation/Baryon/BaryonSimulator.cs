using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading;
using ParticleSimulator.Simulation.Particles.Groups;

namespace ParticleSimulator.Simulation.Baryon;

public class BaryonSimulator(int dim) : ABinaryTreeSimulator<MatterClump, BarnesHutTree> {
	private readonly Lock _lock = new();//needed to avoid camera autocenter from spazzing upon pruning
	private BarnesHutTree _tree = new(dim);
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

	protected override AParticleGroup<MatterClump, BarnesHutTree> NewParticleGroup() =>
		//new PlummerGalaxy((p, v) => new(p, v), Parameters.GALAXY_PLUMMER_RADIUS);
		new SpinningDisk((p, v) => new(p, v));

	protected override void AccumulateLeafNode(NodeParticles leafBin) =>
		leafBin.Node.InitBaryCenter(leafBin.Particles);
	protected override void AccumulateInnerNode(BarnesHutTree node) =>
		node.UpdateBaryCenter();

	protected override void PruneTreeTop() {
		BaryCenter center = this._tree.MassBaryCenter;
		lock (this._lock) {//prevents camera autofollowing from tweaking out if the tree shrinks
			this._tree = (BarnesHutTree)this._tree.PruneTop();
			this._tree.MassBaryCenter = center;
		}
	}

	protected override void ComputeInteractions(NodeParticles leafParticles) {
		List<MatterClump> nearField = [];
		Vector<float> farFieldAcceleration = DetermineNeighbors(leafParticles.Node, nearField);

		Vector<float> influence;
		MatterClump particle1, particle2;
		for (int i = 0; i < leafParticles.Particles.Length; i++) {
			particle1 = leafParticles.Particles[i];
			//add weaker forces first to reduce floating point errors
			for (int n = 0; n < nearField.Count; n++) {
				influence = particle1.ComputeInteractionInfluence(nearField[n]);
				particle1.Acceleration += influence * nearField[n].Mass;
			}
			for (int j = 0; j < i; j++) {
				particle2 = leafParticles.Particles[j];
				influence = particle1.ComputeInteractionInfluence(particle2);
				particle1.Acceleration += influence * particle2.Mass;
				particle2.Acceleration -= influence * particle1.Mass;
			}
			//add last to reduce floating point errors
			particle1.Acceleration += farFieldAcceleration;//cheeky optimization to skip impulse/mass conversion
		}
	}

	public static Vector<float> DetermineNeighbors(BarnesHutTree leaf, List<MatterClump> nearField) {
		//apply the Barnes Hut proximity criterion to partition the tree into nearby leaves and distance approximations
		Vector<float> farFieldAcceleration = Vector<float>.Zero;

		Stack<int> pathDown = new();
		BarnesHutTree parent = leaf, child = null;//STFU compiler

		//evaluate from top nodes down to compute furthest (and weakest) interactions first, to reduce floating point errors when aggregating
		Stack<BarnesHutTree> remaining = new();
		BarnesHutTree neighbor, tail;
		Vector<float> subTotal1, subTotal2, toOther;
		float distanceSquared, invSqRt, invR2, invR3;
		while (pathDown.TryPop(out int idx)) {
			subTotal1 = Vector<float>.Zero;
			for (int i = 0; i < parent.Children.Length; i++) {
				if (i == idx) {
					child = parent.Children[i];
				} else if (parent.Children[i].ItemCount > 0) {
					neighbor = parent.Children[i];
					subTotal2 = Vector<float>.Zero;
					do {//recursively test depth-first for nodes that can be approximated as point masses
						if (neighbor.IsLeaf) {
							nearField.AddRange(neighbor.Bin);
						} else {
							toOther = neighbor.MassBaryCenter.Position - leaf.MassBaryCenter.Position;
							distanceSquared = Vector.Dot(toOther, toOther);
							if (distanceSquared > Parameters.NODE_APPROX_CUTOFF2
							    && distanceSquared * Parameters.INACCURACY2 > neighbor.SizeSquared) {//Barnes-Hut condition
								invSqRt = MathF.ReciprocalSqrtEstimate(distanceSquared);
								invR2 = 1f / distanceSquared;
								invR3 = invSqRt * invR2;
								subTotal2 += toOther * neighbor.MassBaryCenter.Weight * invR3;
							} else {//recurse down
								for (int j = 0; j < neighbor.Children.Length; j++) {
									tail = neighbor.Children[j];
									if (tail.ItemCount > 0)
										remaining.Push(tail);
								}
							}
						}
					} while (remaining.TryPop(out neighbor));
					subTotal1 += subTotal2;
				}
			}
			//reduce floating point error with subtotalling before adding to running total
			farFieldAcceleration += subTotal1;
			parent = child;
		}
		//finally apply G
		return farFieldAcceleration * Parameters.GRAVITATIONAL_CONSTANT;
	}
}