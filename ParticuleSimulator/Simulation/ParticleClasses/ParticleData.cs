using System.Numerics;

namespace ParticleSimulator {
	public struct ParticleData : IBaryonParticle {
		public ParticleData(IBaryonParticle particle) {
			this.ID = particle.ID;
			this.GroupID = particle.GroupID;
			this.Position = particle.Position;
			this.Radius = particle.Radius;
			this.Luminosity = particle.Luminosity;
		}

		public override string ToString() => string.Format("Particle[<{0}> ID {1}]", this.ID, string.Join("", this.Position));

		public int ID { get; private set; }
		public int GroupID { get; private set; }
		public Vector<float> Position { get; private set; }
		public float Radius { get; private set; }
		public float Luminosity { get; private set; }
	}
}