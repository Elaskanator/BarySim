using Generic.Models;

namespace ParticleSimulator.Simulation.Boids {
	public class Boid: AParticle {
		public Boid(int groupID, double[] position, double[] velocity)
		: base(groupID, position, velocity) { }
		
		public override double Radius => 0d;

		public override void Interact(AParticle other) {
			double totalWeight = 0d;
			double[] result = new double[this.Dimensionality];

			double[] vectorTo = other.TrueCoordinates.Subtract(this.TrueCoordinates);
			double dist = vectorTo.Magnitude();
			if (dist > Parameters.SEPARATION && other.GroupID == this.GroupID) {
				if (Parameters.ENABLE_COHESION && Parameters.COHESION_WEIGHT > 0d) {
					totalWeight += Parameters.COHESION_WEIGHT;
					result = result.Add(vectorTo);
				}
				if (Parameters.ENABLE_ALIGNMENT && Parameters.ALIGNMENT_WEIGHT > 0d) {
					totalWeight += Parameters.ALIGNMENT_WEIGHT;
					result = result.Add(other.Velocity);
				}
			} else {
				if (Parameters.ENABLE_SEPARATION && Parameters.SEPARATION_WEIGHT > 0d) {
					totalWeight += Parameters.SEPARATION_WEIGHT;
					result = result.Add(vectorTo.Negate());
				}
			}

			if (totalWeight > 0)
				this.Acceleration = this.Acceleration.Add(result.Divide(totalWeight));
		}
	}
}