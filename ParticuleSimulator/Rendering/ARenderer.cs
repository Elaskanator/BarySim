using System;
using Generic.Classes;
using ParticleSimulator.Engine;
using ParticleSimulator.Engine.Threading;
using ParticleSimulator.Rendering.Rasterization;

namespace ParticleSimulator.Rendering {
	public abstract class ARenderer {
		public ARenderer(RenderEngine engine) {
			this.Engine = engine;
		}
		
		public readonly RenderEngine Engine;

		public readonly AIncrementalAverage<TimeSpan> SimTimings = new SimpleExponentialMovingTimeAverage(Parameters.PERF_SMA_ALPHA);
		public readonly AIncrementalAverage<TimeSpan> FpsTimings = new SimpleExponentialMovingTimeAverage(Parameters.PERF_SMA_ALPHA);
		public int FramesCompleted { get; private set; }

		private float[] _scaling = null;

		public void Draw(EvalResult prepResults, object[] parameters) {
			if (prepResults.PrepPunctual || this._scaling is null)
				this._scaling = (float[])parameters[1];
			object buffer = this.PrepareBuffer(this._scaling, (Pixel[])parameters[0]);
			this.DrawOverlays(prepResults, this._scaling, buffer);
			this.Flush(buffer);
		}

		public void UpdateSimTime(EvalResult prepResults) {
			this.SimTimings.Update(prepResults.ExclusiveTime);
		}

		public void UpdateFullTime(EvalResult prepResults) {
			if (prepResults.PrepPunctual && !this.Engine.IsPaused) {
				this.FpsTimings.Update(prepResults.TotalTimePunctual.Value);
				this.UpdateMonitor(
					this.FramesCompleted,
					this.SimTimings.LastUpdate,
					this.FpsTimings.LastUpdate);

				if (Parameters.FRAME_LIMIT > 0 && this.FramesCompleted >= Parameters.FRAME_LIMIT)
					Program.CancelAction(null, null);
				else this.FramesCompleted++;
			}
		}

		public virtual void Init() { }

		protected abstract object PrepareBuffer(float[] scaling, Pixel[] buffer);
		protected abstract void DrawOverlays(EvalResult prepResults, float[] scaling, object buffer);
		protected abstract void Flush(object buffer);
		protected abstract void UpdateMonitor(int framesCompleted, TimeSpan frameTime, TimeSpan fpsTime);
	}
}