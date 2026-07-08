using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Generic.Extensions;
using ParticleSimulator.Engine.Threading;
using ParticleSimulator.Engine.Threading.Classes;
using ParticleSimulator.Engine.Threading.Configs;
using ParticleSimulator.Engine.Threading.Interface;
using ParticleSimulator.Rendering;
using ParticleSimulator.Rendering.ConsoleRendering;
using ParticleSimulator.Rendering.Exporter;
using ParticleSimulator.Rendering.Rasterization;
using ParticleSimulator.Simulation;
using ParticleSimulator.Simulation.Baryon;
using ParticleSimulator.Simulation.Particles;

namespace ParticleSimulator.Engine;

public class MainEngine : IRunnable {
	private static int _globalId = 0;
	private readonly int _id = ++_globalId;

	public MainEngine() {
		this.KeyListeners = [.. this.BuildKeyListeners()];

		this.Evaluators = [.. this.BuildEvaluators()];
		this._stepsStartingPaused = this.Evaluators.ToDictionary(e => e.Id, e => e.IsPaused);

		this.Simulator = new BaryonSimulator(Parameters.DIM);

		float zoomLevel = 1f / Parameters.VIEWPORT_WIDTH;
		this.Camera = new();
			
		this.Renderer = new ConsoleRenderer(this);

		this.Rasterizer = new(
			this.Camera,
			Parameters.WINDOW_WIDTH,
			Parameters.WINDOW_HEIGHT * 2,
			Program.Random,
			this._rankingsResource);

		this.Scaling = new(this._scalingResource);

		if (Parameters.EXPORT_FRAMES)
			this.Exporter = new BitmapGenerator(
				Parameters.WINDOW_WIDTH,
				Parameters.WINDOW_HEIGHT * 2,
				Parameters.EXPORT_DIR);

		this._keyReaderThread = this.NewKeyReaderThread();
		this._keyReaderThread.Start();
	}
	~MainEngine() => this.Dispose(false);
	public void Dispose() => this.Dispose(true);
	public void Dispose(bool fromDispose) {
		if (!fromDispose) return;

		this._alive = false;

		for (int i = 0; i < this.Evaluators.Length; i++)
			this.Evaluators[i].Dispose(fromDispose);
	}

	public override string ToString() {
		return string.Format("{0}<{1}>[{2}]", nameof(MainEngine),
			this.Evaluators.Length.Pluralize("step"),
			string.Join(", ", this.Evaluators.AsEnumerable()));//string.Join ambiguous without AsEnumerable() (C# you STOOOPID)
	}

	public int Id => this._id;
	public string Name => "Run Manager";
	public bool IsOpen { get; private set; }
	private readonly ManualResetEventSlim _active = new(false);
	public bool IsActive { get => this._active.IsSet; }
	public bool IsPaused { get => !this._active.IsSet; }
	public bool OverlaysEnabled { get; set; }
	private bool _alive = true;

	public DateTime? StartTimeUtc { get; private set; }
	public DateTime? EndTimeUtc { get; private set; }
	public KeyListener[] KeyListeners { get; private set; }

	public ISimulator Simulator { get; private set; }
	public ARenderer Renderer { get; private set; }
	public Autoscaler Scaling { get; private set; }
	public Rasterizer Rasterizer { get; private set; }
	public BitmapGenerator? Exporter { get; private set; }
	public Camera Camera { get; private set; }
		
	internal ACalculationHandler[] Evaluators { get; private set; }
	
	private readonly Thread _keyReaderThread;

	private ProcessThread _stepEval_Simulate = null!;
	private ProcessThread _stepEval_Autoscale = null!;
	private ProcessThread _stepEval_Rasterize = null!;
	private ProcessThread _stepEval_Render = null!;
	private ProcessThread _stepEval_Export = null!;
	private readonly Dictionary<int, bool> _stepsStartingPaused;
		
	private readonly SynchronousBuffer<List<ParticleData>> _particleResource = new("Locations", Parameters.SYNC_SIMULATION ? Parameters.PRECALCULATION_LIMIT : 0);
	private readonly ConsumptionType _particleResourceReadType = Parameters.SYNC_SIMULATION ? ConsumptionType.Consume : ConsumptionType.ConsumeReady;
	private IngestedResource<List<ParticleData>> _particleResourceUse = null!;
	private readonly SynchronousBuffer<float?[]> _rankingsResource = new("Ranks", 0);
	private readonly ConsumptionType _rasterResourceRenderReadType = Parameters.EXPORT_FRAMES ? ConsumptionType.ReadReady : ConsumptionType.Consume;
	private readonly SynchronousBuffer<PixelRank[]> _rasterResource = new("Rasterization", Parameters.SYNC_SIMULATION ? Parameters.PRECALCULATION_LIMIT : 0);
	private readonly SynchronousBuffer<float[]> _scalingResource = new("Scaling", 0);

	public void Start(bool enable = true) {
		if (this.IsOpen) {
			throw new InvalidOperationException("Already open");
		} else {
			this.IsOpen = true;
			if (enable) this._active.Set();
			else this._active.Reset();
			this.StartTimeUtc = DateTime.UtcNow;
				
			this._particleResourceUse.ReadType = enable ? this._particleResourceReadType : ConsumptionType.ConsumeReady;
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
			this._active.Reset();
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
			this._active.Set();
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

			if (Parameters.EXPORT_FRAMES)
				this.Exporter.Cleanup();
		} else throw new InvalidOperationException("Not open");
	}

	public void Restart(bool running) {
		if (this.IsOpen) {
			this.Stop();
				
			Program.ResetRandom();

			this._particleResource.Reset();
			this._rankingsResource.Reset();
			this._rasterResource.Reset();
			this._scalingResource.Reset();

			this.Scaling.Reset();
			this.Camera.ResetRotation();
			if (Parameters.EXPORT_FRAMES)
				this.Exporter.Reset();
				
			this.Start(running);
			this.Pause();
		} else throw new InvalidOperationException("Not open");
	}

	private IEnumerable<ACalculationHandler> BuildEvaluators() {
		this._stepEval_Simulate = ProcessThread.New(new() {
			Name = "Simulate",
			InitFn = () => { this.Simulator.Init(); },
			GeneratorFn = () => { return this.Simulator.Update(); },
			CallbackFn = (r) => { this.Renderer.UpdateSimTime(r); },
			OutputResource = this._particleResource,
			OutputSkips = Parameters.SIMULATION_SKIPS,
			IsOutputOverwrite = !Parameters.SYNC_SIMULATION,
		});
		yield return this._stepEval_Simulate;

		this._particleResourceUse = new(this._particleResource, this._particleResourceReadType);
		this._stepEval_Rasterize = ProcessThread.New(new() {
			Name = "Rasterize",
			CalculatorFn = (r, p) => { return this.Rasterizer.Rasterize(p); },
			OutputResource = this._rasterResource,
			InputResourceUses = [
				this._particleResourceUse,
				new IngestedResource<float[]>(this._scalingResource, ConsumptionType.ReadReady),
			]
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
			InputResourceUses = [
				new IngestedResource<PixelRank[]>(this._rasterResource, this._rasterResourceRenderReadType),
				new IngestedResource<float[]>(this._scalingResource, ConsumptionType.ReadReady),
			]});
		yield return this._stepEval_Render;
			
		if (Parameters.AUTOSCALER_ENABLE) {
			this._stepEval_Autoscale = ProcessThread.New(new() {
				Name = "Autoscale",
				CalculatorFn = (r, p) => { return this.Scaling.Update(p); },
				Synchronizer = Parameters.AUTOSCALE_INTERVAL_MS > 0
					? new TimeSynchronizer(TimeSpan.FromMilliseconds(Parameters.AUTOSCALE_INTERVAL_MS), false)
					: null,
				OutputResource = this._scalingResource,
				IsOutputOverwrite = true,
				InputResourceUses = [
					new IngestedResource<float?[]>(this._rankingsResource, ConsumptionType.Consume),
				]
			});
			yield return this._stepEval_Autoscale;
		}

		if (Parameters.EXPORT_FRAMES) {
			this._stepEval_Export = ProcessThread.New(new() {
				Name = "Exporter",
				EvaluatorFn = (r, p) => { this.Exporter.RenderOut(p); },
				InputResourceUses = [
					new IngestedResource<PixelRank[]>(this._rasterResource, ConsumptionType.Consume),
					new IngestedResource<float[]>(this._scalingResource, ConsumptionType.ReadReady),
				]
			});
			yield return this._stepEval_Export;
		}
	}

	private IEnumerable<KeyListener> BuildKeyListeners() {
		KeyListener[] standardFunctions = [
			new(ConsoleKey.F1, "Stats",
				() => { return this.OverlaysEnabled; },
				s => { this.OverlaysEnabled = s; }),
			new(ConsoleKey.F2, "Main",
				() => { return this.IsActive; },
				s => { this.SetRunningState(s); },
				() => { this.Restart(false); }),
			new(ConsoleKey.F3, "Sim",
				() => { return !this._stepEval_Simulate.IsPaused; },
				s => { this.SetSimulationState(s); },
				() => { this.ResetSimulation(); },
				() => { return !this._stepsStartingPaused[this._stepEval_Simulate.Id]; }),
		];
		KeyListener autoscale = new(ConsoleKey.F4, "Scale",
			() => { return !this._stepEval_Autoscale.IsPaused; },
			s => { this.SetAutoscaleState(s); },
			() => { this._stepEval_Autoscale.Pause(); this.Scaling.Reset(); },
			() => { return !this._stepsStartingPaused[this._stepEval_Autoscale.Id]; });
		KeyListener[] rotationFunctions = [
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
		];
		KeyListener[] positionFunctions = [
			new(ConsoleKey.F9, "Focus",
				() => { return this.Camera.AutoCentering; },
				s => { this.Camera.AutoCentering = s; },
				() => { this.Camera.ResetFocus(); }),

			new(ConsoleKey.A, "←",
					() => { return this.Camera.PanningX == false; },
					s => {
						if (s) this.Camera.PanningX = false;
						else if (this.Camera.PanningX == false) this.Camera.PanningX = null;
					},
					() => { this.Camera.ResetPosition(0); })
				{ IsToggle = false },
			new(ConsoleKey.D, "→",
					() => { return this.Camera.PanningX == true; },
					s => {
						if (s) this.Camera.PanningX = true;
						else if (this.Camera.PanningX == true) this.Camera.PanningX = null;
					},
					() => { this.Camera.ResetPosition(0); })
				{ IsToggle = false },

			new(ConsoleKey.S, "↓",
					() => { return this.Camera.PanningY == false; },
					s => {
						if (s) this.Camera.PanningY = false;
						else if (this.Camera.PanningY == false) this.Camera.PanningY = null;
					},
					() => { this.Camera.ResetPosition(1); })
				{ IsToggle = false },
			new(ConsoleKey.W, "↑",
					() => { return this.Camera.PanningY == true; },
					s => {
						if (s) this.Camera.PanningY = true;
						else if (this.Camera.PanningY == true) this.Camera.PanningY = null;
					},
					() => { this.Camera.ResetPosition(1); })
				{ IsToggle = false },

			new(ConsoleKey.Q, "-",
					() => { return this.Camera.Zooming == false; },
					s => {
						if (s) this.Camera.Zooming = false;
						else if (this.Camera.Zooming == false) this.Camera.Zooming = null;
					},
					() => { this.Camera.Zoom = Parameters.STARTING_ZOOM; })
				{ IsToggle = false },
			new(ConsoleKey.E, "+",
					() => { return this.Camera.Zooming == true; },
					s => {
						if (s) this.Camera.Zooming = true;
						else if (this.Camera.Zooming == true) this.Camera.Zooming = null;
					},
					() => { this.Camera.Zoom = Parameters.STARTING_ZOOM; })
				{ IsToggle = false },
		];

		IEnumerable<KeyListener> result = standardFunctions;
		if (Parameters.AUTOSCALER_ENABLE)
			result = result.Append(autoscale);
		result = result.Concat(rotationFunctions).Concat(positionFunctions);
		return result;
	}
	
	private Thread NewKeyReaderThread() {
		return new Thread(() => {
			while (this._alive) {
				KeyListener.HandleConsoleInputs(this.KeyListeners);
				Thread.Sleep(10);
			}
		}) { IsBackground = true };
	}

	private void SetAutoscaleState(bool enable) {
		this._stepsStartingPaused[this._stepEval_Autoscale.Id] |= !enable;
		if (!enable || this.IsActive)
			this._stepEval_Autoscale.SetRunningState(enable);
	}

	private void SetSimulationState(bool enable) {
		this._stepsStartingPaused[this._stepEval_Simulate.Id] |= !enable;
		if (!enable || this.IsActive) {
			this._stepEval_Simulate.SetRunningState(enable);
			this._particleResourceUse.ReadType = enable ? this._particleResourceReadType : ConsumptionType.ConsumeReady;
		}
	}

	private void ResetSimulation() {
		bool paused = this._stepEval_Simulate.IsPaused;
		Program.ResetRandom();
		this._stepEval_Simulate.Restart(false);
		this._stepsStartingPaused[this._stepEval_Simulate.Id] = !paused;
		if (!paused)
			this._stepEval_Simulate.Resume();
	}
}