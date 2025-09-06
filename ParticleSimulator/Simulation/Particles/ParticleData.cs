using System.Numerics;

namespace ParticleSimulator.Simulation.Particles {
	public struct ParticleData : IParticle {
		public ParticleData(IParticle particle) {
			this.Id = particle.Id;
			this.GroupId = particle.GroupId;
			this.Position = particle.Position;
			this.Radius = particle.Radius;
			this.Density = particle.Density;
			this.Luminosity = particle.Luminosity;
		}

		public override string ToString() => string.Format("Particle[<{0}> ID {1}]", this.Id, string.Join("", this.Position));

		public int Id { get; private set; }
		public int GroupId { get; private set; }
		public Vector<float> Position { get; private set; }
		public float Radius { get; private set; }
		public float Density { get; private set; }
		public float Luminosity { get; private set; }
	}
}