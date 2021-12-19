using System.Numerics;

namespace ParticleSimulator.Rendering {
	public struct ParticleData : IBaryonParticle {
		public int ID { get; private set; }
		public int GroupID { get; private set; }
		public Vector<float> Position { get; private set; }
		public float Radius { get; private set; }
		public float Density { get; private set; }
		public float Luminosity { get; private set; }
		public bool IsVisible { get; private set; }

		public ParticleData(IBaryonParticle particle) {
			this.ID = particle.ID;
			this.GroupID = particle.GroupID;
			this.Radius = particle.Radius;
			this.Density = particle.Density;
			this.Luminosity = particle.Luminosity;

			this.Position = Renderer.RotateCoordinates(particle.Position);
			bool visible = true;
			for (int d = 0; d < Parameters.DIM && visible; d++)
				if (particle.Position[d] + particle.Radius < 0f || particle.Position[d] - particle.Radius >= Parameters.DOMAIN_SIZE[d])
					visible = false;
			this.IsVisible = visible;
		}
	}
}