using System;
using System.Linq;
using System.Numerics;
using Generic.Vectors;
using ParticleSimulator.Simulation.Baryon;

namespace ParticleSimulator.Rendering.Rasterization {
	public class Camera {
		public Camera(float scaling = 1f) {
			this.InitialScaling = this.Scaling = scaling * Parameters.WORLD_SCALE * 2f;

			this.RotationMatrixColumns = VectorFunctions.IdentityMatrixColumns;
			this.SetRange(
				-Vector<float>.One * (1f / scaling),
				Vector<float>.One * (1f / scaling));
			this.InitialCenter = this.Center;

			this.AutoCentering = true;
		}

		public bool AutoCentering { get; set; }

		public bool IsAutoIncrementActive { get; set; }
		public bool IsPitchRotationActive { get; set; }
		public bool IsYawRotationActive { get; set; }
		public bool IsRollRotationActive { get; set; }

		public Vector<float> Left { get; private set; }
		public Vector<float> Center { get; private set; }
		public Vector<float> InitialCenter { get; private set; }
		public Vector<float> Right { get; private set; }
		public Vector<float> Size { get; private set; }
		public float Scaling { get; set; }
		public readonly float InitialScaling;

		public Vector<float>[] RotationMatrixColumns { get; private set; }
		public bool IsRotationNonzero { get; private set; }

		public int RotationStepsPitch = 0;
		public int RotationStepsYaw = 0;
		public int RotationStepsRoll = 0;

		public void Reset() {
			this.IsAutoIncrementActive = false;
			this.AutoCentering = false;
			//this.IsPitchRotationActive = false;
			//this.IsYawRotationActive = false;
			//this.IsRollRotationActive = false;
			this.RotationStepsPitch = 0;
			this.RotationStepsYaw = 0;
			this.RotationStepsRoll = 0;

			this.ResetZoom();
		}
		public void ResetZoom() {
			this.Scaling = this.InitialScaling;
			this.Center = this.InitialCenter;
			this.AutoCentering = false;
		}

		public void SetRange(Vector<float> left, Vector<float> right) {
			this.Left = left;
			this.Right = right;
			this.Center = left + 0.5f*(right - left);
			this.Size = right - left;
		}

		public void Set3DRotation(float yaw, float pitch, float roll) {
			if (yaw == 0f && pitch == 0f && roll == 0f) {
				this.RotationMatrixColumns = VectorFunctions.IdentityMatrixColumns;
				this.IsRotationNonzero = false;
			} else {
				this.IsRotationNonzero = true;
				this.RotationMatrixColumns = new Vector<float>[] {
					VectorFunctions.New(
						MathF.Cos(yaw) * MathF.Cos(pitch),
						MathF.Sin(yaw) * MathF.Cos(pitch),
						-MathF.Sin(pitch)),
					VectorFunctions.New(
						MathF.Cos(yaw) * MathF.Sin(pitch) * MathF.Sin(roll) - MathF.Sin(yaw) * MathF.Cos(roll),
						MathF.Sin(yaw) * MathF.Sin(pitch) * MathF.Sin(roll) + MathF.Cos(yaw) * MathF.Cos(roll),
						MathF.Cos(pitch) * MathF.Sin(roll)),
					VectorFunctions.New(
						MathF.Cos(yaw) * MathF.Sin(pitch) * MathF.Cos(roll) + MathF.Sin(yaw) * MathF.Sin(roll),
						MathF.Sin(yaw) * MathF.Sin(pitch) * MathF.Cos(roll) - MathF.Cos(yaw) * MathF.Sin(roll),
						MathF.Cos(pitch) * MathF.Cos(roll))
				}.Concat(VectorFunctions.IdentityMatrixColumns.Skip(3))
				.ToArray();
			}
		}

		public Vector<float> Rotate(Vector<float> v) {
			Vector<float> offsetV = v - this.Center;
			offsetV *= this.Scaling;

			if (this.IsRotationNonzero) {
				Span<float> values = stackalloc float[Vector<float>.Count];
					values[0] = Vector.Dot(offsetV, this.RotationMatrixColumns[0]);
					values[1] = Vector.Dot(offsetV, this.RotationMatrixColumns[1]);
					values[2] = Vector.Dot(offsetV, this.RotationMatrixColumns[2]);
				//for (int i = 3; i < Vector<float>.Count; i++)
				//	values[i] = offsetV[i];
				return new Vector<float>(values) + this.Center;
			} else return offsetV + this.Center;
		}

		public void Increment() {
			this.Set3DRotation(
				Parameters.WORLD_ROTATION_RADS_PER_STEP * this.RotationStepsPitch,
				Parameters.WORLD_ROTATION_RADS_PER_STEP * this.RotationStepsYaw,
				Parameters.WORLD_ROTATION_RADS_PER_STEP * this.RotationStepsRoll);
			
			if (this.AutoCentering && Program.Engine.Simulator.ParticleCount > 0 && !(Program.Engine.Simulator.ParticleTree is null)) {
				BarnesHutTree tree = (BarnesHutTree)Program.Engine.Simulator.ParticleTree;
				if (tree.MassBaryCenter.Weight > 0f) {
					this.Center = 2f*tree.MassBaryCenter.Position;
					float maxLength = 1f;//TODODO
					this.Scaling = maxLength * Parameters.WORLD_SCALE * 2f;
				}
			}

			if (this.IsAutoIncrementActive) {
				if (this.IsPitchRotationActive)
					this.RotationStepsPitch++;
				if (this.IsYawRotationActive)
					this.RotationStepsYaw++;
				if (this.IsRollRotationActive)
					this.RotationStepsRoll++;
			} 
		}
	}
}