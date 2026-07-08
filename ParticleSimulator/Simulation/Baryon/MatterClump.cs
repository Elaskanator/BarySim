using System;
using System.Numerics;
using Generic.Vectors;
using ParticleSimulator.Simulation.Particles;

namespace ParticleSimulator.Simulation.Baryon;

public class MatterClump : AParticle<MatterClump, BarnesHutTree> {
	public MatterClump(Vector<float> position, Vector<float> velocity)
		: base(position, velocity) {
		this.SetMass(Parameters.MASS_SCALAR);
	}
		
	public float Mass;
	private void SetMass(float value) {
		this.Mass = value;
		if (this.IsCollapsed) {
			this._density = float.PositiveInfinity;
			this._radius = MathF.Pow(value, 1f / 3f) * Parameters.BLACKHOLE_RADIUS_SCALAR * Parameters.MASS_RADIUS_SCALAR;
		} else {
			this._density = MathF.Pow(value, Parameters.MASS_DENSITY_POW) * Parameters.MASS_DENSITY_SCALAR;
			var volume = value / this._density;
			this._radius = (float)VectorFunctions.HypersphereRadius(volume, Parameters.DIM) * Parameters.MASS_RADIUS_SCALAR;
		}
		this.Luminosity = this.IsCollapsed
			? -1f
			: Parameters.MASS_LUMINOSITY_SCALAR * MathF.Pow(value, Parameters.MASS_LUMINOSITY_POW);
	}
		
	private float _density = Parameters.MASS_DENSITY_SCALAR;
	public override float Density => this._density;
	public bool IsCollapsed { get; private set; }

	public override Vector<float> Momentum {
		get => this.Velocity * this.Mass;
		set { this.Velocity = value * (1f / this.Mass); } }

	public override Vector<float> Impulse {
		get => this.Acceleration * this.Mass;
		set { this.Acceleration = value * (1f / this.Mass); } }

	public override Vector<float> DragImpulse {
		get => this.DragAcceleration * this.Mass;
		set { this.DragAcceleration = value * (1f / this.Mass); } }

	public override Vector<float> ComputeInfluence(MatterClump other, Vector<float> toOther, float distance, float distance2) {
		float largerRadius = this._radius > other._radius ? this._radius : other._radius;
		distance = distance >= largerRadius ? distance : largerRadius;
		return toOther * (Parameters.GRAVITATIONAL_CONSTANT / (distance2 * distance));
	}

	public override Vector<float> ComputeCollisionImpulse(MatterClump other, float engulfRelativeDistance) {
		if (Parameters.DRAG_CONSTANT > 0f) {
			Vector<float> dV = other.Velocity - this.Velocity;
			float smallerMass = this.Mass > other.Mass ? other.Mass : this.Mass;
			return dV * ((1f - engulfRelativeDistance) * smallerMass * Parameters.DRAG_CONSTANT);
		} else return Vector<float>.Zero;
	}

	public override void Consume(MatterClump other) {
		float totalMass = this.Mass + other.Mass;
		float totalMassInv = 1f / totalMass;

		Vector<float> weightedPosition = ((this.Mass*this._position) + (other.Mass*other._position)) * totalMassInv;
		Vector<float> weightedAcceleration1 = ((this.Mass*this._acceleration1) + (other.Mass*other._acceleration1)) * totalMassInv;
		Vector<float> weightedAcceleration2 = ((this.Mass*this._acceleration2) + (other.Mass*other._acceleration2)) * totalMassInv;
		Vector<float> totalMomentum = this.Momentum + other.Momentum;
		Vector<float> totalImpulse = this.Impulse + other.Impulse;
		Vector<float> totalDragImpulse = this.DragImpulse + other.DragImpulse;

		this.SetMass(totalMass);

		this.IsCollapsed |= other.IsCollapsed;
		this._position = weightedPosition;
		this.Momentum = totalMomentum;
		this.Impulse = totalImpulse;
		this.DragImpulse = totalDragImpulse;
		this._acceleration1 = weightedAcceleration1;
		this._acceleration2 = weightedAcceleration2;
	}

	protected override void AfterMove() {
		if (!this.IsCollapsed && this.Mass >= Parameters.SUPERNOVA_CRITICAL_MASS) {
			if (Parameters.BLACKHOLE_ENABLE && this.Mass >= Parameters.BLACKHOLE_THRESHOLD * Parameters.SUPERNOVA_CRITICAL_MASS) {
				this.IsCollapsed = true;
				this.Luminosity = -1f;
			} else if (Parameters.SUPERNOVA_ENABLE)
				this.GoSupernova();
		}
	}
	private void GoSupernova() {
		int numParticles = (int)(Parameters.SUPERNOVA_EJECTA_MASS > 0
			? this.Mass / Parameters.SUPERNOVA_EJECTA_MASS
			: this.Mass);
		if (numParticles > 1) {
			float maxRadius = this._radius * Parameters.SUPERNOVA_RADIUS_SCALAR;
			float ratio = (1f / numParticles);
			float avgMass = ratio * this.Mass;
			Vector<float> avgImpulse = ratio * this.Impulse;

			this.NewParticles ??= new();
			this.SetMass(avgMass);
			this.Impulse = avgImpulse;

			Vector<float> direction;
			float rand, radius;
			MatterClump newParticle;
			for (int i = 1; i < numParticles; i++) {
				direction = VectorFunctions.RandomDirectionVector(Parameters.DIM, Program.Random);
				rand = (float)Program.Random.NextDouble();
				radius = maxRadius * MathF.Pow(rand, 1f / Parameters.DIM);

				newParticle = new(
					this._position + direction * radius,
					this.Velocity + direction * Parameters.SUPERNOVA_EJECTA_SPEED)
				{
					GroupId = this.GroupId,
				};
				newParticle.SetMass(avgMass);
				newParticle.Impulse += avgImpulse;

				this.NewParticles.Enqueue(newParticle);
			}
		}
	}

	protected override bool SurviveOutOfBounds(BaryCenter center, float distance2) =>
		Vector.Dot(this.Velocity, this.Velocity) < //below escape velocity
		2f * Parameters.GRAVITATIONAL_CONSTANT * center.Weight * MathF.ReciprocalSqrtEstimate(distance2);
}