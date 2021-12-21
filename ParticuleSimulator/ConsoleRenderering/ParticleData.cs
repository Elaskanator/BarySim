using System.Linq;
using System.Numerics;
using Generic.Vectors;

namespace ParticleSimulator.ConsoleRendering {
	public struct ParticleData : IBaryonParticle {
		public int ID { get; private set; }
		public int GroupID { get; private set; }
		public Vector<float> Position { get; private set; }
		public float Radius { get; private set; }
		public float Density { get; private set; }
		public float Luminosity { get; private set; }

		public bool IsVisible { get; private set; }
		public float Depth =>
			Parameters.DIM < 3
				? 0f
				: (this.Position * new Vector<float>(Enumerable.Range(0, Vector<float>.Count).Select(i => i < 2 ? 0f : 1f).ToArray())).Magnitude();
		//public static readonly float DOMAIN_HIDDEN_DIMENSIONAL_HEIGHT = DIM < 3 ? 0f : MathF.Sqrt(Enumerable.Range(2, DIM - 2).Select(d => DOMAIN_SIZE[d]).Sum(x => x * x));

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