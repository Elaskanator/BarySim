﻿using System;
using Generic.Extensions;
using System.Linq;
using System.Threading;
using ParticleSimulator.Rendering;
using ParticleSimulator.Rendering.Rasterization;
using ParticleSimulator.Rendering.SystemConsole;
using ParticleSimulator.Simulation;
using ParticleSimulator.Simulation.Baryon;
using ParticleSimulator.Simulation.Particles;
//using ParticleSimulator.Rendering.Exporter;
using ParticleSimulator.Engine.Threading;
using System.Collections.Generic;

namespace ParticleSimulator.Engine {
	public class RenderEngine : IRunnable {
		private static int _globalId = 0;
		private readonly int _id = ++_globalId;

		public RenderEngine() {
			this.KeyListeners = this.BuildKeyListeners().ToArray();

			this.Evaluators = this.BuildEvaluators().ToArray();
			this._stepsStartingPaused = this.Evaluators.ToDictionary(e => e.Id, e => e.IsPaused);

			this.Simulator = new BaryonSimulator(Parameters.DIM);

			float zoomLevel = Parameters.ZOOM_SCALE / Parameters.WORLD_SCALE;
			this.Camera = new() {
				Zoom = zoomLevel,
				DeafultZoom = zoomLevel,
				AutoCentering = Parameters.AUTOFOCUS_DEFAULT,
			};

			this.Rasterizer = new(
				this.Camera,
				Parameters.WINDOW_WIDTH,
				Parameters.WINDOW_HEIGHT * 2,
				Program.Random,
				this._rankingsResource);

			this.Scaling = new(this._scalingResource);

			this.Renderer = new ConsoleRenderer(this);

			//this.Exporter = new BitmapGenerator(
			//	Parameters.WINDOW_WIDTH,
			//	Parameters.WINDOW_HEIGHT * 2,
			//	Parameters.EXPORT_DIR);
		}

		~RenderEngine() => this.Dispose(false);

		public override string ToString() {
			return string.Format("{0}<{1}>[{2}]", nameof(RenderEngine),
				this.Evaluators.Length.Pluralize("step"),
				string.Join(", ", this.Evaluators.AsEnumerable()));//string.Join ambiguous without AsEnumerable() (C# you STOOOPID)
		}

		public int Id => this._id;
		public string Name => "Run Manager";
		public bool IsOpen { get; private set; }
		public bool IsPaused { get; private set; }
		public bool OverlaysEnabled { get; set; }

		public DateTime? StartTimeUtc { get; private set; }
		public DateTime? EndTimeUtc { get; private set; }
		public KeyListener[] KeyListeners { get; private set; }

		public ISimulator Simulator { get; private set; }
		public ARenderer Renderer { get; private set; }
		public Autoscaler Scaling { get; private set; }
		public Rasterizer Rasterizer { get; private set; }
		//public BitmapGenerator Exporter { get; private set; }
		public Camera Camera { get; private set; }
		
		internal ACalculationHandler[] Evaluators { get; private set; }
		private Thread _keyReader;

		private ProcessThread _stepEval_Simulate;
		private ProcessThread _stepEval_Autoscale;
		private ProcessThread _stepEval_Rasterize;
		private ProcessThread _stepEval_Render;
		//private ProcessThread _stepEval_Export;
		private Dictionary<int, bool> _stepsStartingPaused;
		
		private readonly SynchronousBuffer<List<ParticleData>> _particleResource = new("Locations", Parameters.PRECALCULATION_LIMIT);
		private readonly ConsumptionType _particleResourceReadType = Parameters.SYNC_SIMULATION ? ConsumptionType.Consume : ConsumptionType.ConsumeReady;
		private IngestedResource<List<ParticleData>> _particleResourceUse;
		private readonly SynchronousBuffer<float?[]> _rankingsResource = new("Ranks", 0);
		private readonly SynchronousBuffer<Pixel[]> _rasterResource = new("Rasterization", Parameters.PRECALCULATION_LIMIT);
		private readonly SynchronousBuffer<float[]> _scalingResource = new("Scaling", 0);

		public void Start(bool enable = true) {
			if (this.IsOpen) {
				throw new InvalidOperationException("Already open");
			} else {
				this.IsOpen = true;
				this.IsPaused = !enable;
				this.StartTimeUtc = DateTime.UtcNow;
				
				this._particleResourceUse.ReadType = enable ? this._particleResourceReadType : ConsumptionType.ConsumeReady;
				this._keyReader = new(this.HandleInputs);
				this._keyReader.Start();
				bool startActive;

				for (int i = 0; i < this.Evaluators.Length; i++) {
					startActive = (enable || this.Evaluators[i] == this._stepEval_Render)
						&& (this.Evaluators[i] != this._stepEval_Autoscale
							|| (Parameters.COLORING != ParticleColoringMethod.Depth && Parameters.COLORING != ParticleColoringMethod.Overlap));
					this.Evaluators[i].Start(startActive);
					this._stepsStartingPaused[this.Evaluators[i].Id] = !startActive;
				}
			}
		}

		public void Pause() {
			if (this.IsOpen) {
				this.IsPaused = true;
				for (int i = 0; i < this.Evaluators.Length; i++) {
					this._stepsStartingPaused[this.Evaluators[i].Id] = this.Evaluators[i].IsPaused;
					if (this.Evaluators[i] != this._stepEval_Render)
						this.Evaluators[i].Pause();
				}
			} else throw new InvalidOperationException("Not open");
		}

		public void Resume() {
			if (this.IsOpen) {
				for (int i = 0; i < this.Evaluators.Length; i++)
					if (!this._stepsStartingPaused[this.Evaluators[i].Id])
						this.Evaluators[i].Resume();
				this.IsPaused = false;
			} else throw new InvalidOperationException("Not open");
		}

		public void SetRunningState(bool running) {
			if (running) this.Resume();
			else this.Pause();
		}

		public void Stop () {
			if (this.IsOpen) {
				//this._keyReader.Interrupt();//why does this break stuff???!??!!
				for (int i = 0; i < this.Evaluators.Length; i++)
					this.Evaluators[i].Stop();
				this.EndTimeUtc = DateTime.UtcNow;
				this.IsOpen = false;
			} else throw new InvalidOperationException("Not open");
		}

		public void Restart(bool running) {
			if (this.IsOpen) {
				this.Stop();
				
				Program.ResetRandon();

				this._particleResource.Reset();
				this._rankingsResource.Reset();
				this._rasterResource.Reset();
				this._scalingResource.Reset();

				this.Scaling.Reset();
				this.Camera.ResetRotation();
				//this.Exporter.Reset();
				
				this.Start(running);
				this.Pause();
			} else throw new InvalidOperationException("Not open");
		}

		public void Dispose() => this.Dispose(true);
		public void Dispose(bool fromDispose) {
			if (fromDispose)
				for (int i = 0; i < this.Evaluators.Length; i++)
					this.Evaluators[i].Dispose(fromDispose);
		}

		private IEnumerable<ACalculationHandler> BuildEvaluators() {
			this._stepEval_Simulate = ProcessThread.New(new() {
				Name = "Simulate",
				InitFn = () => { this.Simulator.Init(); },
				GeneratorFn = () => { return this.Simulator.Update(); },
				CallbackFn = (r) => { this.Renderer.UpdateSimTime(r); },
				OutputResource = this._particleResource,
				OutputSkips = Parameters.SIMULATION_SKIPS,
			});
			yield return this._stepEval_Simulate;

			this._particleResourceUse = new(this._particleResource, this._particleResourceReadType);
			this._stepEval_Rasterize = ProcessThread.New(new() {
				Name = "Rasterize",
				CalculatorFn = (r, p) => { return this.Rasterizer.Rasterize(r, p); },
				OutputResource = this._rasterResource,
				InputResourceUses = new IIngestedResource[] {
					this._particleResourceUse,
					new IngestedResource<float[]>(this._scalingResource, ConsumptionType.ReadReady),
				}
			});
			yield return this._stepEval_Rasterize;
			
			this._stepEval_Render = ProcessThread.New(new() {
				Name = "Draw",
				InitFn = () => { this.Renderer.Init(); },
				EvaluatorFn = (r, p) => { this.Renderer.Draw(r, p); },
				CallbackFn = (r) => { this.Renderer.UpdateFullTime(r); },
				Synchronizer = Parameters.TARGET_FPS > 0f
					? new TimeSynchronizer(Parameters.TARGET_FPS, Parameters.VSYNC)
					: null,
				DataLoadingTimeout = TimeSpan.FromMilliseconds(Parameters.MON_WARN_MS),
				InputResourceUses = new IIngestedResource[] {
					new IngestedResource<Pixel[]>(this._rasterResource, /*Parameters.EXPORT_FRAMES ? ConsumptionType.ReadReady : */ConsumptionType.Consume),
					new IngestedResource<float[]>(this._scalingResource, ConsumptionType.ReadReady),
				}});
			yield return this._stepEval_Render;
			
			if (Parameters.AUTOSCALER_ENABLE) {
				this._stepEval_Autoscale = ProcessThread.New(new() {
					Name = "Autoscale",
					CalculatorFn = (r, p) => { return this.Scaling.Update(r, p); },
					Synchronizer = Parameters.AUTOSCALE_INTERVAL_MS > 0
						? new TimeSynchronizer(TimeSpan.FromMilliseconds(Parameters.AUTOSCALE_INTERVAL_MS), false)
						: null,
					OutputResource = this._scalingResource,
					IsOutputOverwrite = true,
					InputResourceUses = new IIngestedResource[] {
						new IngestedResource<float?[]>(this._rankingsResource, ConsumptionType.Consume),
					}
				});
				yield return this._stepEval_Autoscale;
			}

			//if (Parameters.EXPORT_FRAMES) {
			//	this._stepEval_Export = ProcessThread.New(new() {
			//		Name = "Exporter",
			//		EvaluatorFn = (r, p) => { this.Exporter.RenderOut(r, p); },
			//		InputResourceUses = new IIngestedResource[] {
			//			new IngestedResource<Pixel[]>(this._rasterResource, ConsumptionType.Consume),
			//			new IngestedResource<float[]>(this._scalingResource, ConsumptionType.ReadReady),
			//		}
			//	});
			//	yield return this._stepEval_Export;
			//}
		}

		private IEnumerable<KeyListener> BuildKeyListeners() {
			KeyListener[] standardFunctions = new KeyListener[] {
				new(ConsoleKey.F1, "Stats",
					() => { return this.OverlaysEnabled; },
					s => { this.OverlaysEnabled = s; }),
				new(ConsoleKey.F2, "Main",
					() => { return !this.IsPaused; },
					s => { this.SetRunningState(s); },
					() => { this.Restart(false); }),
				new(ConsoleKey.F3, "Sim",
					() => { return !this._stepEval_Simulate.IsPaused; },
					s => { this.SetSimulationState(s); },
					() => { this.ResetSimulation(); },
					() => { return !this._stepsStartingPaused[this._stepEval_Simulate.Id]; }),
			};
			KeyListener autoscale = new(ConsoleKey.F4, "Scale",
				() => { return !this._stepEval_Autoscale.IsPaused; },
				s => { this.SetAutoscaleState(s); },
				() => { this._stepEval_Autoscale.Pause(); this.Scaling.Reset(); },
				() => { return !this._stepsStartingPaused[this._stepEval_Autoscale.Id]; });
			KeyListener[] rotationFunctions = new KeyListener[] {
				new(ConsoleKey.F5, "Rotate",
					() => { return this.Camera.IsAutoIncrementActive; },
					s => { this.Camera.IsAutoIncrementActive = s; },
					() => { this.Camera.ResetRotation(); }) ,
				new(ConsoleKey.F6, "α",
					() => { return this.Camera.IsPitchRotationActive; },
					s => { this.Camera.IsPitchRotationActive = s; },
					() => { this.Camera.IsPitchRotationActive = false; this.Camera.RotationStepsPitch = 0; }),
				new(ConsoleKey.F7, "β",
					() => { return this.Camera.IsYawRotationActive; },
					s => { this.Camera.IsYawRotationActive = s; },
					() => { this.Camera.IsYawRotationActive = false; this.Camera.RotationStepsYaw = 0; }),
				new(ConsoleKey.F8, "γ",
					() => { return this.Camera.IsRollRotationActive; },
					s => { this.Camera.IsRollRotationActive = s; },
					() => { this.Camera.IsRollRotationActive = false; this.Camera.RotationStepsRoll = 0; }),
			};
			KeyListener[] positionFunctions = new KeyListener[] {
				new(ConsoleKey.F9, "Focus",
					() => { return this.Camera.AutoCentering; },
					s => { this.Camera.AutoCentering = s; },
					() => { this.Camera.ResetFocus(); }),
			};

			IEnumerable<KeyListener> result = standardFunctions;
			if (Parameters.AUTOSCALER_ENABLE)
				result = result.Append(autoscale);
			result = result.Concat(rotationFunctions).Concat(positionFunctions);
			return result;
		}

		private void SetAutoscaleState(bool enable) {
			this._stepsStartingPaused[this._stepEval_Autoscale.Id] |= !enable;
			if (!enable || !this.IsPaused)
				this._stepEval_Autoscale.SetRunningState(enable);
		}

		private void SetSimulationState(bool enable) {
			this._stepsStartingPaused[this._stepEval_Simulate.Id] |= !enable;
			if (!enable || !this.IsPaused) {
				this._stepEval_Simulate.SetRunningState(enable);
				this._particleResourceUse.ReadType = enable ? this._particleResourceReadType : ConsumptionType.ConsumeReady;
			}
		}

		private void ResetSimulation() {
			bool paused = this._stepEval_Simulate.IsPaused;
			Program.ResetRandon();
			this._stepEval_Simulate.Restart(false);
			this._stepsStartingPaused[this._stepEval_Simulate.Id] = !paused;
			if (!paused)
				this._stepEval_Simulate.Resume();
		}

		private void HandleInputs() {
			ConsoleKeyInfo keyInfo;
			try {
				while (this.IsOpen) {
					keyInfo = Console.ReadKey(true);
					for (int i = 0; i < this.KeyListeners.Length; i++)
						if (keyInfo.Key == this.KeyListeners[i].Key)
							if ((this.KeyListeners[i].Resetter is null) || (keyInfo.Modifiers & ConsoleModifiers.Control) == 0)
								this.KeyListeners[i].Toggle();
							else this.KeyListeners[i].Resetter();
				}
			} catch (ThreadInterruptedException) { }//die
		}
	}
}