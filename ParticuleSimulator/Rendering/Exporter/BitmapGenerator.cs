using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ParticleSimulator.Engine.Threading;
using ParticleSimulator.Rendering.Rasterization;

namespace ParticleSimulator.Rendering.Exporter {
	public class BitmapGenerator {
		public BitmapGenerator(int width, int height, string directory = null) {
			this.ContainingFolder = directory ?? Directory.GetCurrentDirectory();
		}

		public int Width { get; private set; }
		public int Height { get; private set; }

		public string ContainingFolder { get; private set; }
		public int NumSaved { get; private set; }

		public void RenderOut(EvalResult prepResults, object[] parameters) {
			Pixel[] pixels = (Pixel[])parameters[0];
			//throw null;
		}

		private void Save(Bitmap render) {
			throw null;
		}
	}
}