using System.Numerics;

namespace ParticleSimulator.Simulation.Baryon {
	public struct BaryCenter {
		public readonly Vector<float> Position;
		public readonly float Weight;

		public BaryCenter(Vector<float> position, float weight) {
			this.Position = position;
			this.Weight = weight;
		}

		public override string ToString() =>
			string.Format("BaryCenter{0}[{1}]", string.Join("", this.Position), this.Weight);
	}
}