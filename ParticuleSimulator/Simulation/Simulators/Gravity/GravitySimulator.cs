namespace ParticleSimulator.Simulation.Gravity {
	public class GravitySimulator : AParticleSimulator {
		public GravitySimulator()
		: base(new GravitationalForce(), new ElectrostaticForce()) { }

		protected override AParticleGroup NewParticleGroup() { return new Galaxy(); }
	}
}