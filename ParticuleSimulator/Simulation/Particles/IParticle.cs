using System;
using System.Numerics;
using Generic.Vectors;

namespace ParticleSimulator.Simulation.Particles {
	public interface IParticle : IPosition<Vector<float>>, IEquatable<IParticle> {
		int Id { get; }
		int GroupId { get; }
		float Radius { get; }
		float Luminosity { get; }
		
		bool Equals(object other) => (other is IParticle data) && this.Id == data.Id;
		bool IEquatable<IParticle>.Equals(IParticle other) => this.Id == other.Id;
		int GetHashCode() => this.Id;
	}
}