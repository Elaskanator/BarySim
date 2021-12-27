using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParticleSimulator.Simulation {
	public interface ISimulator {
		int ParticleCount { get; }

		ParticleData[] RefreshSimulation();
	}
}