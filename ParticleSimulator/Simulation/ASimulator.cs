using System.Collections.Generic;
using System.Linq;
using Generic.Trees;
using ParticleSimulator.Simulation.Baryon;
using ParticleSimulator.Simulation.Particles;
using ParticleSimulator.Simulation.Particles.Groups;

namespace ParticleSimulator.Simulation;

public abstract class ASimulator<TParticle, TNode> : ISimulator
	where TParticle : AParticle<TParticle, TNode>
	where TNode : ABinaryTree<TNode, TParticle>{
	public ASimulator() {
		this.IterationCount = -1;
	}

	public int IterationCount { get; private set; }
	IEnumerable<IParticle> ISimulator.Particles => this.Particles;
	public int ParticleCount => this.Particles.Count;

	public abstract ICollection<TParticle> Particles { get; }
	public abstract BaryCenter Center { get; }

	protected abstract List<ParticleData> Refresh();
	protected abstract AParticleGroup<TParticle, TNode> NewParticleGroup();
		
	public abstract void Init();

	public List<ParticleData> Update() {
		++this.IterationCount;
		return this.IterationCount == 0//skip to show starting data on first result (TODO cleanup)
			? [.. this.Particles.Select(p => new ParticleData(p))]
			: this.Refresh();
	}
}