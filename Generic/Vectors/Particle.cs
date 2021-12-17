using System.Numerics;

namespace Generic.Vectors {
	public interface IParticle {
		public int ID { get; }

		public Vector<float> Position { get; }
	}
	public abstract class AParticle : IParticle {
		private static int _globalID = 0;
		private readonly int _id = ++_globalID;
		public int ID => this._id;

		public Vector<float> Position { get; set; }
	}
}