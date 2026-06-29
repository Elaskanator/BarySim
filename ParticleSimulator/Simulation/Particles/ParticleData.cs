using System.Numerics;

namespace ParticleSimulator.Simulation.Particles;

public struct ParticleData {
	public ParticleData(IParticle particle) {
		this.Id = particle.Id;
		this.GroupId = particle.GroupId;
		this.Position = particle.Position;
		this.Radius = particle.Radius;
		this.Density = particle.Density;
		this.Luminosity = particle.Luminosity;
	}

	public override string ToString() => string.Format("Particle[<{0}> ID {1}]", this.Id, string.Join("", this.Position));

	public int Id;
	public int GroupId;
	public Vector<float> Position;
	public float Radius;
	public float Density;
	public float Luminosity;
}