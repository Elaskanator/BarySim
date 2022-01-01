using System;
using Generic.Extensions;
using System.Linq;
using System.Threading;
using Generic.Models;
using ParticleSimulator.Rendering;
using ParticleSimulator.Rendering.Rasterization;
using ParticleSimulator.Rendering.SystemConsole;
using ParticleSimulator.Simulation;
using ParticleSimulator.Simulation.Baryon;
using ParticleSimulator.Rendering.Exporter;
using ParticleSimulator.Engine.Threading;
using System.Collections.Generic;

namespace ParticleSimulator.Engine {
	public class RenderEngine : IRunnable {
		private static int _globalId = 0;
		private readonly int _id = ++_globalId;

		public RenderEngine() {
			this.Random = Parameters.DETERMINISTIC_RANDOM_SEED == -1
				? new()
				: new(Parameters.DETERMINISTIC_RANDOM_SEED);
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

		public Random Random { get; private set; }
		public ISimulator Simulator { get; private set; }
		public ARenderer Renderer { get; private set; }
		public Autoscaler Scaling { get; private set; }
		public Rasterizer Rasterizer { get; private set; }
		public BitmapGenerator Exporter { get; private set; }
		
		internal ACalculationHandler[] Evaluators { get; private set; }

		private ProcessThread _stepEval_Simulate;
		private ProcessThread _stepEval_Autoscale;
		private ProcessThread _stepEval_Rasterize;
		private ProcessThread _stepEval_Render;
		private ProcessThread _stepEval_Export;
		private Dictionary<int, bool> _stepsStartingPaused;
		
		private SynchronousBuffer<ParticleData[]> _particleResource = new("Locations", Parameters.PRECALCULATION_LIMIT);
		private IngestedResource<ParticleData[]> _particleResourceUse;
		private SynchronousBuffer<float?[]> _rankingsResource = new("Ranks", 0);
		private SynchronousBuffer<Pixel[]> _rasterResource = new("Rasterization", Parameters.PRECALCULATION_LIMIT);
		private SynchronousBuffer<float[]> _scalingResource = new("Scaling", 0);

		public void Init() {
			this.Simulator = new BaryonSimulator();

			this.Rasterizer = new(
				Parameters.WINDOW_WIDTH,
				Parameters.WINDOW_HEIGHT * 2,
				this.Random,
				this._rankingsResource);

			this.Scaling = new();
			_scalingResource.Overwrite(this.Scaling.Values);

			this.Renderer = new ConsoleRenderer(this);

			this.Exporter = new BitmapGenerator(
				Parameters.WINDOW_WIDTH,
				Parameters.WINDOW_HEIGHT * 2,
				Parameters.EXPORT_DIR);

			this.Evaluators = this.BuildEvaluators().ToArray();

			this.KeyListeners = this.BuildKeyListeners().ToArray();

			this.Renderer.Init();
			this.Simulator.Init();

			this._stepsStartingPaused = this.Evaluators.ToDictionary(e => e.Id, e => e.IsPaused);
		}

		public void Start() {
			if (this.IsOpen) {
				throw new InvalidOperationException("Already open");
			} else {
				this.Init();

				this.IsOpen = true;
				this.StartTimeUtc = DateTime.UtcNow;

				this.Renderer.Startup();

				Thread keyReader = new(this.HandleInputs);
				keyReader.Start();

				for (int i = 0; i < this.Evaluators.Length; i++)
					this.Evaluators[i].Start();
			}
		}

		public void Pause() {
			if (this.IsOpen) {
				for (int i = 0; i < this.Evaluators.Length; i++) {
					this._stepsStartingPaused[this.Evaluators[i].Id] = this.Evaluators[i].IsPaused;
					if (this.Evaluators[i] != this._stepEval_Render)
						this.Evaluators[i].Pause();
				}
				this.IsPaused = true;
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

		public void SetRunningState(bool state) {
			if (state) this.Resume();
			else this.Pause();
		}

		public void Stop () {
			if (this.IsOpen) {
				this.EndTimeUtc = DateTime.UtcNow;
				for (int i = 0; i < this.Evaluators.Length; i++)
					this.Evaluators[i].Stop();
			} else throw new InvalidOperationException("Not open");
		}

		public void Restart() {
			if (this.IsOpen) {
				for (int i = 0; i < this.Evaluators.Length; i++)
					this.Evaluators[i].Restart();
			} else throw new InvalidOperationException("Not open");
		}

		public void Dispose() => this.Dispose(true);
		public void Dispose(bool fromDispose) {
			if (fromDispose)
				for (int i = 0; i < this.Evaluators.Length; i++)
					this.Evaluators[i].Dispose(fromDispose);
		}

		private void HandleInputs() {
			ConsoleKey key;
			while (this.IsOpen) {
				key = Console.ReadKey(true).Key;
				for (int i = 0; i < this.KeyListeners.Length; i++)
					if (key == this.KeyListeners[i].Key)
						this.KeyListeners[i].Toggle();
			}
		}

		private IEnumerable<ACalculationHandler> BuildEvaluators() {
			this._stepEval_Simulate = ProcessThread.New(new() {
				Name = "Simulate",
				GeneratorFn = this.Simulator.RefreshSimulation,
				CallbackFn = this.Renderer.UpdateSimTime,
				OutputResource = this._particleResource,
				IsOutputOverwrite = !Parameters.SYNC_SIMULATION,
				OutputSkips = Parameters.SIMULATION_SKIPS,
			});
			yield return this._stepEval_Simulate;

			this._particleResourceUse = new IngestedResource<ParticleData[]>(this._particleResource, ConsumptionType.Consume);
			this._stepEval_Rasterize = ProcessThread.New(new() {
				Name = "Rasterize",
				CalculatorFn = this.Rasterizer.Rasterize,
				OutputResource = this._rasterResource,
				InputResourceUses = new IIngestedResource[] {
					this._particleResourceUse,
					new IngestedResource<float[]>(this._scalingResource, ConsumptionType.ReadReady),
				}
			});
			yield return this._stepEval_Rasterize;
			
			this._stepEval_Render = ProcessThread.New(new() {
				Name = "Draw",
				EvaluatorFn = this.Renderer.Draw,
				CallbackFn = this.Renderer.UpdateFullTime,
				Synchronizer = Parameters.TARGET_FPS > 0f
					? new TimeSynchronizer(Parameters.TARGET_FPS, Parameters.VSYNC)
					: null,
				DataLoadingTimeout = TimeSpan.FromSeconds(1d),
				InputResourceUses = new IIngestedResource[] {
					new IngestedResource<Pixel[]>(this._rasterResource, Parameters.EXPORT_FRAMES ? ConsumptionType.ReadReady : ConsumptionType.Consume),
					new IngestedResource<float[]>(this._scalingResource, ConsumptionType.ReadReady),
				}});
			yield return this._stepEval_Render;
			
			if (Parameters.AUTOSCALER_ENABLE) {
				this._stepEval_Autoscale = ProcessThread.New(Parameters.COLOR_METHOD != ParticleColoringMethod.Overlap, new() {
					Name = "Autoscale",
					CalculatorFn = this.Scaling.Update,
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

			if (Parameters.EXPORT_FRAMES) {
				this._stepEval_Export = ProcessThread.New(new() {
					Name = "Exporter",
					EvaluatorFn = this.Exporter.RenderOut,
					InputResourceUses = new IIngestedResource[] {
						new IngestedResource<Pixel[]>(this._rasterResource, ConsumptionType.Consume),
						new IngestedResource<float[]>(this._scalingResource, ConsumptionType.ReadReady),
					}
				});
				yield return this._stepEval_Export;
			}
		}

		private IEnumerable<KeyListener> BuildKeyListeners() {
			KeyListener[] standardFunctions = new KeyListener[] {
				new(ConsoleKey.F1, "Stats",
				() => { return this.OverlaysEnabled; },
				s => { this.OverlaysEnabled = s; }) {
				},
				new(ConsoleKey.F2, "Main",
				() => { return !this.IsPaused; },
				s => { this.SetRunningState(s); }) {
				},
				new(ConsoleKey.F3, "Sim",
				() => { return !this._stepEval_Simulate.IsPaused; },
				s => { this.SetSimulationState(s); }) {
				},
			};
			KeyListener autoscale = new(ConsoleKey.F4, "Scale",
				() => { return !this._stepEval_Autoscale.IsPaused; },
				s => { this.SetAutoscaleState(s); }) {
			};
			KeyListener[] rotationFunctions = new KeyListener[] {
				new(ConsoleKey.F5, "Rotate",
				() => { return this.Rasterizer.Camera.IsAutoIncrementActive; },
				s => { this.Rasterizer.Camera.IsAutoIncrementActive = s; }) {
				},
				new(ConsoleKey.F6, "α",
				() => { return this.Rasterizer.Camera.IsRollRotationActive; },
				s => { this.Rasterizer.Camera.IsRollRotationActive = s; }) {
				},
				new(ConsoleKey.F7, "β",
				() => { return this.Rasterizer.Camera.IsPitchRotationActive; },
				s => { this.Rasterizer.Camera.IsPitchRotationActive = s; }) {
				},
				new(ConsoleKey.F8, "γ",
				() => { return this.Rasterizer.Camera.IsYawRotationActive; },
				s => { this.Rasterizer.Camera.IsYawRotationActive = s; }) {
				},
			};

			IEnumerable<KeyListener> result = standardFunctions;
			if (Parameters.AUTOSCALER_ENABLE)
				result = result.Append(autoscale);
			result = result.Concat(rotationFunctions);
			return result;
		}

		private void SetAutoscaleState(bool enable) {
			this._stepsStartingPaused[this._stepEval_Autoscale.Id] = !enable;
			if (!enable || !this.IsPaused)
				this._stepEval_Autoscale.SetRunningState(enable);
		}

		private void SetSimulationState(bool enable) {
			this._stepsStartingPaused[this._stepEval_Simulate.Id] = !enable;
			if (!enable || !this.IsPaused) {
				this._stepEval_Simulate.SetRunningState(enable);
				this._particleResourceUse.ReadType = enable
					? ConsumptionType.Consume
					: ConsumptionType.ReadImmediate;
			}
		}
	}
}