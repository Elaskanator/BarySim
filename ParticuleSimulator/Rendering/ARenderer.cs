using System;
using System.Linq;
using Generic.Models;
using ParticleSimulator.Engine;

namespace ParticleSimulator.Rendering {
	public abstract class ARenderer {
		public ARenderer(RenderEngine engine) {
			this.Engine = engine;
		}
		
		public readonly RenderEngine Engine;

		public readonly AIncrementalAverage<TimeSpan> FrameTimings = new SimpleExponentialMovingTimeAverage(Parameters.PERF_SMA_ALPHA);
		public readonly AIncrementalAverage<TimeSpan> FpsTimings = new SimpleExponentialMovingTimeAverage(Parameters.PERF_SMA_ALPHA);
		public int FramesCompleted { get; private set; }

		public void Draw(bool wasPunctual, object[] parameters) {
			float[] scaling = this.Engine.Scaling.Values;
			object buffer = this.PrepareBuffer(scaling, (Pixel[])parameters[0]);
			this.DrawOverlays(wasPunctual, scaling, buffer);
			this.Flush(buffer);
		}

		public void UpdateRenderTime(bool wasPunctual) {
			if (wasPunctual) {
				TimeSpan currentFpsTime = this.Engine.StepEval_Render.FullTime.LastUpdate;
				this.FpsTimings.Update(currentFpsTime);
				this.UpdateMonitor(this.FramesCompleted, this.FrameTimings.LastUpdate, this.FpsTimings.LastUpdate);
				this.FramesCompleted++;
			}
		}

		public void UpdateRasterizationTime(bool wasPunctual = true) {
			TimeSpan currentFrameTime = new TimeSpan[] {
				this.Engine.StepEval_Simulate.ExclusiveTime.LastUpdate,
				this.Engine.StepEval_Rasterize.ExclusiveTime.LastUpdate,
			}.Max();

			this.FrameTimings.Update(currentFrameTime);
		}

		public abstract void Init();

		protected abstract object PrepareBuffer(float[] scaling, Pixel[] buffer);
		protected abstract void DrawOverlays(bool wasPunctual, float[] scaling, object buffer);
		protected abstract void Flush(object buffer);
		protected abstract void UpdateMonitor(int framesCompleted, TimeSpan frameTime, TimeSpan fpsTime);
	}
}