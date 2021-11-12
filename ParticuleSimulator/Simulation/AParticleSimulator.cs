using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Generic.Extensions;
using Generic.Models;
using ParticleSimulator.Rendering;

namespace ParticleSimulator.Simulation {
	public interface IParticleSimulator : IEnumerable<AParticle> {
		public bool IsDiscrete { get; }
		public AParticle[] AllParticles { get; }
		public SampleSMA[] DensityScale { get; }
		public int InteractionLimit { get; }
		public int? NeighborhoodFilteringDepth { get; }

		public ITree RebuildTree();
		public ConsoleColor ChooseGroupColor(AParticle[] others);
		public Tuple<double[], AParticle>[] RefreshSimulation(object[] parameters);
		public Tuple<char, AParticle[]>[] Resample(object[] parameters);
		public void AutoscaleUpdate(object[] parameters);
	}

	public abstract class AParticleSimulator<P, G> : IParticleSimulator
	where P : AParticle
	where G : AParticleGroup<P> {
		public AParticleSimulator(Random rand = null) {
			this._rand = rand ?? new Random();
			this.ParticleGroups = Enumerable.Range(0, Parameters.NUM_PARTICLE_GROUPS).Select(i => this.NewParticleGroup(rand)).ToArray();
			this.AllParticles = this.ParticleGroups.SelectMany(g => g.Particles).ToArray();
			this.DensityScale = Enumerable
				.Range(1, Parameters.DENSITY_COLORS.Length - 1)
				.Select(x => new SampleSMA(Parameters.AUTOSCALING_SMA_ALPHA, x))
				.ToArray();
		}
		public virtual int InteractionLimit => Parameters.DESIRED_INTERACTION_NEIGHBORS;
		public virtual int? NeighborhoodFilteringDepth => null;
		
		public abstract bool IsDiscrete { get; }
		public G[] ParticleGroups { get; private set; }
		public P[] AllParticles { get; private set; }
		AParticle[] IParticleSimulator.AllParticles => this.AllParticles;
		public SampleSMA[] DensityScale { get; private set; }
		protected readonly Random _rand;

		public abstract G NewParticleGroup(Random rand);

		public ParticleTree<P> RebuildTree() {
			foreach (P p in this.AllParticles)
				p.Coordinates = (double[])p.TrueCoordinates.Clone();//tree reuse means we don't care about race conditions with dirty access
			return new ParticleTree<P>(this.AllParticles);
		}
		ITree IParticleSimulator.RebuildTree() { return this.RebuildTree(); }

		public Tuple<double[], AParticle>[] RefreshSimulation(ParticleTree<P> tree) {
			this.InteractTree(tree);
			foreach (P p in this.AllParticles)
				p.ApplyTimeStep();

			Tuple<double[], AParticle>[] result = this.AllParticles.Select(p => new Tuple<double[], AParticle>(p.TrueCoordinates, p)).ToArray();

			return result;
		}
		public Tuple<double[], AParticle>[] RefreshSimulation(object[] parameters) { return this.RefreshSimulation((ParticleTree<P>)parameters[0]); }

		protected virtual void InteractTree(ParticleTree<P> tree) {
			Parallel.ForEach(tree.Leaves, leaf => {
				foreach (P p in leaf.NodeElements) {
					p.Acceleration = new double[p.DIMENSIONALITY];
					p.Interact(leaf.GetNeighbors().Except(p2 => p2.ID == p.ID));
				}
			});
		}

		public virtual ConsoleColor ChooseGroupColor(AParticle[] others) {//treat like a static method
			int dominantGroupID = others.GroupBy(p => p.GroupID).MaxBy(g => g.Count()).Key;
			return (ConsoleColor)(dominantGroupID < 1 ? 1 : dominantGroupID > 14 ? 14 :
				dominantGroupID == (int)Parameters.BOIDS_PREDATOR_COLOR
					? ((dominantGroupID + 1) % 16) + 1
					: dominantGroupID);
		}

		public virtual Tuple<char, AParticle[]>[] Resample(object[] parameters) {
			Tuple<double[], AParticle>[] particleData = (Tuple<double[], AParticle>[])parameters[0];
			Tuple<char, AParticle[]>[] results = new Tuple<char, AParticle[]>[Parameters.WINDOW_WIDTH * Parameters.WINDOW_HEIGHT];

			bool top, bottom;
			char pixelChar;
			foreach (IGrouping<int, dynamic> bin in this.DiscreteParticleBin(particleData)) {
				top = bin.Any(t => t.Y % 1d < 0.5d);
				bottom = bin.Any(t => t.Y % 1d >= 0.5d);

				if (top && bottom)
					pixelChar = Parameters.CHAR_BOTH;
				else if (top)
					pixelChar = Parameters.CHAR_TOP;
				else pixelChar = Parameters.CHAR_LOW;

				results[bin.Key] =
					new Tuple<char, AParticle[]>(
						pixelChar,
						bin.Select(b => (AParticle)b.Particle).ToArray());
			}
			return results;
		}

		public IEnumerable<IGrouping<int, dynamic>> DiscreteParticleBin(Tuple<double[], AParticle>[] particleData) { 
			return particleData
				.Select(d => new {
					X = d.Item1[0] * Renderer.RenderWidth / Parameters.DOMAIN[0],
					Y = d.Item1[1] * Renderer.RenderHeight / Parameters.DOMAIN[1] / 2d,
					Particle = d.Item2})
				.GroupBy(d => (int)d.X + Renderer.RenderWidthOffset + Parameters.WINDOW_WIDTH*((int)d.Y + Renderer.RenderHeightOffset));
/*
			foreach (IGrouping<int, Tuple<double[], AParticle>> xGroup
			in particleData.GroupBy(c => (int)(c.Item1[0] * Renderer.RenderWidth / Parameters.DOMAIN[0])))
				foreach (IGrouping<int, Tuple<AParticle, int, double>> yGroup
				in xGroup.Select(c => new Tuple<AParticle, int, double>(c.Item2, xGroup.Key,
					Parameters.DOMAIN.Length < 2 ? 0 : (int)(c.Item1[1] * Renderer.RenderHeight / Parameters.DOMAIN[1] / 2d)))
					.GroupBy(t => (int)t.Item3)) 
						yield return yGroup;
*/
		}

		

		public void AutoscaleUpdate(Tuple<char, double>[] sampling) {
			double[] orderedCounts = ((Tuple<char, double>[])sampling.Clone()).Except(c => c is null).Select(c => c.Item2).Order().ToArray();//TODO use selection sort?
			if (orderedCounts.Length > 0) {
				int totalBands = Parameters.DENSITY_COLORS.Length - 1;

				double curVal = 0d, curValRounded = 0d;
				double newVal = 0d, newValRounded = 0d;
				int percentilIdx;
				for (int band = 1; band <= totalBands; band++) {
					percentilIdx = (int)(((double)orderedCounts.Length * band / (totalBands + 1d)) - 1d);

					if (orderedCounts.Length > percentilIdx) {
						curVal = curValRounded = this.DensityScale[band - 1].Current;
						newVal = newValRounded = orderedCounts[percentilIdx];
						if (Program.Simulator.IsDiscrete) {
							curValRounded = Math.Floor(curVal);
							newValRounded = Math.Ceiling(newVal);
						}
					} else {
						newVal = newValRounded = curVal = ++curValRounded;
					}

					if (newValRounded > curVal) {
						this.DensityScale[band - 1].Update(newValRounded, Program.StepEval_Resample.NumCompleted <= 1 ? 1d : null);
					} else if (band > 1 && newValRounded <= this.DensityScale[band - 2].Current) {
						this.DensityScale[band - 1].Update(this.DensityScale[band - 2].Current + 1, 1d);
					} else {
						this.DensityScale[band - 1].Update(newValRounded, 1d);
					}
				}
			}
		}
		public void AutoscaleUpdate(object[] parameters) { this.AutoscaleUpdate((Tuple<char, double>[])parameters[0]); }

		public IEnumerator<AParticle> GetEnumerator() { return this.AllParticles.AsEnumerable().GetEnumerator(); }
		IEnumerator IEnumerable.GetEnumerator() {return this.AllParticles.GetEnumerator(); }
	}
}