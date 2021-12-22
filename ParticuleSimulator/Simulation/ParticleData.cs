using System.Numerics;
using ParticleSimulator.ConsoleRendering;

namespace ParticleSimulator {
	public struct ParticleData : IBaryonParticle {
		public ParticleData(IBaryonParticle particle) {
			this.ID = particle.ID;
			this.GroupID = particle.GroupID;
			this.Radius = particle.Radius;
			this.Density = particle.Density;
			this.Luminosity = particle.Luminosity;

			this.Position = ConsoleRenderer.RotateCoordinates(particle.Position);

			bool visible = true;
			//for (int d = 0; visible && d < Parameters.DIM && d < 3; d++)
			//	if (this.Position[d] + particle.Radius < 0f || this.Position[d] - particle.Radius >= Parameters.DOMAIN_SIZE[d])
			//		visible = false;
			this.IsVisible = visible;
		}

		public override string ToString() => string.Format("Particle[<{0}> ID {1}]", this.ID, string.Join("", this.Position));

		public int ID { get; private set; }
		public int GroupID { get; private set; }
		public Vector<float> Position { get; private set; }
		public float Radius { get; private set; }
		public float Density { get; private set; }
		public float Luminosity { get; private set; }

		public bool IsVisible { get; private set; }
	}
}