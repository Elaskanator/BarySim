using System;
using System.Linq;
using System.Numerics;
using Generic.Vectors;

namespace ParticleSimulator.Rendering.Rasterization {
	public class Camera {
		public Camera(float scaling = 1f) {
			this.Scaling = scaling;

			this.RotationMatrixColumns = VectorFunctions.IdentityMatrixColumns;
			this.SetRange(
				-Vector<float>.One,
				Vector<float>.One);
		}

		public Vector<float> Left { get; private set; }
		public Vector<float> Center { get; private set; }
		public Vector<float> Right { get; private set; }
		public Vector<float> Size { get; private set; }
		public float Scaling { get; set; }

		public Vector<float>[] RotationMatrixColumns { get; private set; }
		public bool IsRotationNonzero { get; private set; }

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

		public Vector<float> OffsetAndRotate(Vector<float> v) {
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
	}
}