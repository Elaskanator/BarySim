﻿using System;
using System.Linq;
using System.Numerics;
using Generic.Vectors;

namespace ParticleSimulator.Rendering {
	public class Camera {
		public const float AUTO_CENTER_UPDATE_ALPHA = 0.2f;

		public Camera() {
			this.RotationMatrixColumns = VectorFunctions.IdentityMatrixColumns;
		}

		public bool AutoCentering;

		public bool IsAutoIncrementActive { get; set; }
		public bool IsPitchRotationActive { get; set; }
		public bool IsYawRotationActive { get; set; }
		public bool IsRollRotationActive { get; set; }

		private VectorSmoothedIncrementalAverage _center = new(AUTO_CENTER_UPDATE_ALPHA);
		public Vector<float> Center {
			get => this._center.Current;
			set { this._center.Reset(); this._center.Update(value); } }
		public Vector<float> DefaultCenter = Vector<float>.Zero;
		//public Vector<float> Size { get; private set; }
		public float Zoom = 1f;
		public float DeafultZoom = 1f;

		public Vector<float>[] RotationMatrixColumns { get; private set; }
		public bool IsRotationNonzero { get; private set; }

		public int RotationStepsPitch = 0;
		public int RotationStepsYaw = 0;
		public int RotationStepsRoll = 0;

		public void ResetRotation() {
			this.IsAutoIncrementActive = false;
			//this.IsPitchRotationActive = false;
			//this.IsYawRotationActive = false;
			//this.IsRollRotationActive = false;
			this.RotationStepsPitch = 0;
			this.RotationStepsYaw = 0;
			this.RotationStepsRoll = 0;
		}
		public void ResetFocus() {
			this.Zoom = this.DeafultZoom;
			this.Center = this.DefaultCenter;
			this.AutoCentering = false;
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
			offsetV *= this.Zoom;

			if (this.IsRotationNonzero) {
				Span<float> values = stackalloc float[Vector<float>.Count];
					values[0] = Vector.Dot(offsetV, this.RotationMatrixColumns[0]);
					values[1] = Vector.Dot(offsetV, this.RotationMatrixColumns[1]);
					values[2] = Vector.Dot(offsetV, this.RotationMatrixColumns[2]);
				//for (int i = 3; i < Vector<float>.Count; i++)
				//	values[i] = offsetV[i];
				return new Vector<float>(values);
			} else return offsetV;
		}

		public void Increment(Vector<float> position) {
			this.Set3DRotation(
				Parameters.WORLD_ROTATION_RAD_PER_FRAME * this.RotationStepsPitch,
				Parameters.WORLD_ROTATION_RAD_PER_FRAME * this.RotationStepsYaw,
				Parameters.WORLD_ROTATION_RAD_PER_FRAME * this.RotationStepsRoll);
			
			if (this.AutoCentering) {
				this._center.Update(position);
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