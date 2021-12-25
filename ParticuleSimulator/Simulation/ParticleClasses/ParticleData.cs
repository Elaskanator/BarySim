using System.Numerics;

namespace ParticleSimulator {
	public struct ParticleData : IBaryonParticle {
		public ParticleData(IBaryonParticle particle) {
			this.ID = particle.ID;
			this.GroupID = particle.GroupID;
			this.Radius = particle.Radius;
			this.Luminosity = particle.Luminosity;

			this.Position = RotateCoordinates(particle.Position);
		}

		public override string ToString() => string.Format("Particle[<{0}> ID {1}]", this.ID, string.Join("", this.Position));

		public int ID { get; private set; }
		public int GroupID { get; private set; }
		public Vector<float> Position { get; private set; }
		public float Radius { get; private set; }
		public float Luminosity { get; private set; }

		public bool IsVisible => Vector.LessThanOrEqualAll(new Vector<float>(-this.Radius), this.Position)
			&& Vector.LessThanAll(this.Position, Parameters.DOMAIN_SIZE + new Vector<float>(this.Radius));

		public static Vector<float> RotateCoordinates(Vector<float> coordinates) {//TODODO
			return coordinates;
		}
	}
}