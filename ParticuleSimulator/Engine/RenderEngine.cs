using System;
using System.Collections.Generic;
using Generic.Extensions;
using System.Linq;
using Generic.Models;
using ParticleSimulator.Rendering;
using ParticleSimulator.Simulation;
using ParticleSimulator.Rendering.SystemConsole;

namespace ParticleSimulator.Engine {
	public class RenderEngine : IRunnable {
		private static int _globalId = 0;
		private readonly int _id = ++_globalId;

		~RenderEngine() => this.Dispose(false);

		public override string ToString() {
			return string.Format("{0}<{1}>[{2}]", nameof(RenderEngine),
				this.Evaluators.Length.Pluralize("step"),
				string.Join(", ", this.Evaluators.AsEnumerable()));//string.Join ambiguous without AsEnumerable() (C# you STOOOPID)
		}

		public int Id => this._id;
		public string Name => "Run Manager";
		public readonly Random Random = new();

		public bool IsOpen { get; private set; }
		public DateTime? StartTimeUtc { get; private set; }
		public DateTime? EndTimeUtc { get; private set; }
		
		internal ACalculationHandler[] Evaluators { get; private set; }

		public ISimulator Simulator { get; private set; }
		public ARenderer Renderer { get; private set; }
		
		public Autoscaler Scaling { get; private set; }
		public Rasterizer Rasterizer { get; private set; }

		internal ProcessThread StepEval_Simulate { get; private set; }
		internal ProcessThread StepEval_Autoscale { get; private set; }
		internal ProcessThread StepEval_Rasterize { get; private set; }
		internal ProcessThread StepEval_Render { get; private set; }
		internal ProcessThread StepEval_Export { get; private set; }

		public void Init() {
			ACalculationHandler[] steps;
			SynchronousBuffer<Queue<ParticleData>> particleResource = new("Locations", Parameters.PRECALCULATION_LIMIT);
			SynchronousBuffer<Pixel[]> rasterResource = new("Rasterization", Parameters.PRECALCULATION_LIMIT);
			SynchronousBuffer<float?[]> rawRankData = new("Ranks", 0);

			SynchronousBuffer<float[]> scaling = new("Scaling", 0);
			this.Scaling = new();
			scaling.Overwrite(this.Scaling.Values);

			this.Simulator = new BaryonSimulator();
			this.Rasterizer = new(Parameters.WINDOW_WIDTH, Parameters.WINDOW_HEIGHT * 2, this.Random, rawRankData);
			this.Renderer = new ConsoleRenderer(this);
			
			this.StepEval_Render = ProcessThread.New(new() {
				Name = "Draw",
				EvaluatorFn = this.Renderer.Draw,
				CallbackFn = this.Renderer.UpdateRenderTime,
				Synchronizer = Parameters.TARGET_FPS > 0f || Parameters.MAX_FPS > 0f
					? TimeSynchronizer.FromFps(Parameters.TARGET_FPS, Parameters.MAX_FPS)
					: null,
				DataLoadingTimeout = TimeSpan.FromMilliseconds(Parameters.PERF_WARN_MS),
				InputResourceUses = new IPrerequisite[] {
					new IPrerequisite<Pixel[]>() {
						Resource = rasterResource,
						DoConsume = true,
					},
					new IPrerequisite<float[]>() {
						Resource = scaling,
					},
				}});

			this.StepEval_Simulate = ProcessThread.New(new() {
				Name = "Simulate",
				GeneratorFn = this.Simulator.RefreshSimulation,
				CallbackFn = this.Renderer.UpdateRasterizationTime,
				OutputResource = particleResource,
				IsOutputOverwrite = !Parameters.SYNC_SIMULATION,
				OutputSkips = Parameters.SIMULATION_SKIPS,
			});

			this.StepEval_Rasterize = ProcessThread.New(new() {
				Name = "Rasterize",
				CalculatorFn = this.Rasterizer.Rasterize,
				OutputResource = rasterResource,
				InputResourceUses = new IPrerequisite[] {
					new IPrerequisite<Queue<ParticleData>>() {
						Resource = particleResource,
						DoConsume = true,
					},
					new IPrerequisite<float[]>() {
						Resource = scaling,
					}
				}
			});
			
			if (Parameters.AUTOSCALER_ENABLE)
				this.StepEval_Autoscale = ProcessThread.New(new() {
					Name = "Autoscale",
					CalculatorFn = this.Scaling.Update,
					Synchronizer = Parameters.AUTOSCALE_INTERVAL_MS > 0
						? new TimeSynchronizer(null, TimeSpan.FromMilliseconds(Parameters.AUTOSCALE_INTERVAL_MS))
						: null,
					OutputResource = scaling,
					IsOutputOverwrite = true,
					InputResourceUses = new IPrerequisite[] {
						new IPrerequisite<float?[]>() {
							Resource = rawRankData,
							DoConsume = true,
						}
					}
			});
			
			steps = new[] {
				this.StepEval_Simulate,
				this.StepEval_Rasterize,
				this.StepEval_Autoscale,
				this.StepEval_Render,
				this.StepEval_Export,
			};

			this.Evaluators = steps.Without(s => s is null).ToArray();

			this.Renderer.Init();
		}

		public void Start() {
			if (this.IsOpen) {
				throw new InvalidOperationException("Already open");
			} else {
				this.Init();
				this.IsOpen = true;
				this.StartTimeUtc = DateTime.UtcNow;
				for (int i = 0; i < this.Evaluators.Length; i++)
					this.Evaluators[i].Start();
			}
		}

		public void Pause() {
			if (this.IsOpen) {
				for (int i = 0; i < this.Evaluators.Length; i++)
					this.Evaluators[i].Pause();
			} else throw new InvalidOperationException("Not open");
		}

		public void Resume() {
			if (this.IsOpen) {
				for (int i = 0; i < this.Evaluators.Length; i++)
					this.Evaluators[i].Resume();
			} else throw new InvalidOperationException("Not open");
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
	}
}