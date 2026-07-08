using System;
using System.IO;
using Generic.Extensions;
using ParticleSimulator.Engine.Threading;
using ParticleSimulator.Rendering.Rasterization;

namespace ParticleSimulator.Rendering.Exporter;

public class BitmapGenerator {
	public BitmapGenerator(int width, int height, string? rootDir = null) {
		this.Width = width;
		this.Height = height;
		this.RootDir = new(
			Path.Join(rootDir ??
			          //Directory.GetCurrentDirectory()
			          Environment.GetFolderPath(Environment.SpecialFolder.Desktop)));

		this.Reset();
	}

	public int Width { get; private set; }
	public int Height { get; private set; }
	public int FrameNumber { get; private set; }

	public readonly DirectoryInfo RootDir;
	public DirectoryInfo DirectoryInfo { get; private set; } = null!;
	public int NumSaved { get; private set; }

	public void RenderOut(object[] parameters) {
		Bitmap bitmap = new((PixelRank[])parameters[0]);
		string path = Path.Join(
			this.DirectoryInfo.ToString(),
			string.Join("frame{0}.bmp", ++this.FrameNumber));
		bitmap.Write(path);
	}

	public void Reset() {
		this.Cleanup();

		this.DirectoryInfo = new(
			Path.Join(
				this.RootDir.ToString(),
				string.Format("{0}_{1:yyyy-MM-ddTHH-mm-ss}Z",
					"Baryon Simulator",
					DateTime.UtcNow)));
		if (this.DirectoryInfo.Exists)
			if (ConsoleExtensions.Confirm("Time duplicate path exists, replace (Y/N)? "))
				FileExtensions.EmptyDirectory(this.DirectoryInfo);
			else throw new Exception("Aborted");
		else this.DirectoryInfo.Create();

		this.FrameNumber = 0;
	}

	public void Cleanup() {
		if (this.DirectoryInfo.IsEmpty())
			this.DirectoryInfo.Delete();
	}
}