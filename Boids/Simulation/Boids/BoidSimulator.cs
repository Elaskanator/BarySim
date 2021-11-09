using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Generic.Extensions;
using Generic.Models;
using ParticleSimulator.Rendering;

namespace ParticleSimulator.Simulation.Boids {
	public class BoidSimulator : AParticleSimulator<Boid, QuadTree<Boid>> {
		public BoidSimulator() {
			Flocks = Enumerable.Range(0, Parameters.NUM_PARTICLE_GROUPS).Select(i => new Flock(this._rand)).ToArray();
		}

		public override bool IsDiscrete => true;
		public Flock[] Flocks { get; private set; }
		public override IEnumerable<Boid> AllParticles => Flocks.SelectMany(f => f.Particles);
		public override QuadTree<Boid> NewTree => new QuadTree<Boid>(new double[Parameters.DOMAIN.Length], Parameters.DOMAIN);

		protected override void ComputeUpdate(QuadTree<Boid> tree) {
			Parallel.ForEach(tree.Leaves, leaf => {
				foreach (Boid b in leaf.AllElements)
					b.UpdateDeltas(leaf.GetNeighbors()); });
		}

		protected override Tuple<char, double>[] Resample(Tuple<double[], double>[] particles, double width, double height) {
			Tuple<char, double>[] results = new Tuple<char, double>[Parameters.WINDOW_WIDTH * Parameters.WINDOW_HEIGHT];

			int topCount, bottomCount;
			double colorCount;
			char pixelChar;
			foreach (IGrouping<int, double[]> xGroup
			in particles.Select(p => p.Item1).GroupBy(c => (int)(Renderer.RenderWidth * c[0] / Parameters.DOMAIN[0]))) {
				foreach (IGrouping<int, double> yGroup
				in xGroup//subdivide each pixel into two vertical components
					.Select(c => Parameters.DOMAIN.Length < 2 ? 0 : Renderer.RenderHeight * c[1] / Parameters.DOMAIN[1] / 2d)
					.GroupBy(y => (int)y))//preserve floating point value of normalized Y for subdivision
				{
					topCount = yGroup.Count(y => y % 1d < 0.5d);
					bottomCount = yGroup.Count() - topCount;

					if (topCount > 0 && bottomCount > 0) {
						pixelChar = Parameters.CHAR_BOTH;
						colorCount = ((double)topCount + bottomCount) / 2d;
					} else if (topCount > 0) {
						pixelChar = Parameters.CHAR_TOP;
						colorCount = topCount;
					} else {
						pixelChar = Parameters.CHAR_BOTTOM;
						colorCount = bottomCount;
					}

					results[xGroup.Key + Renderer.RenderWidthOffset + Parameters.WINDOW_WIDTH*(yGroup.Key + Renderer.RenderHeightOffset)] =
						new Tuple<char, double>(
							pixelChar,
							colorCount);
				}
			}

			return results;
		}
	}
}