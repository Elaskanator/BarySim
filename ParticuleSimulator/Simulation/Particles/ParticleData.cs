using System;
using System.Collections.Generic;
using System.Numerics;
using Generic.Vectors;

namespace ParticleSimulator.Simulation {
	public struct ParticleData : ISimulationParticle, IEquatable<ParticleData>, IEqualityComparer<ParticleData> {
		public int ID { get; private set; }
		public int GroupID { get; private set; }
		public Vector<float> Position { get; private set; }
		public float Radius { get; private set; }
		public float Density { get; private set; }
		public float Luminosity { get; private set; }
		public bool IsVisible { get; private set; }

		public ParticleData(ISimulationParticle particle) {
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

		public bool Equals(ParticleData other) { return this.ID == other.ID; }
		public override bool Equals(object other) { return (other is IParticle) && this.ID == (other as IParticle).ID; }
		public bool Equals(ParticleData x, ParticleData y) { return x.ID == y.ID; }
		public int GetHashCode(ParticleData obj) { return obj.ID.GetHashCode(); }
		public override int GetHashCode() { return this.ID.GetHashCode(); }
	}
}