using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Generic.Extensions {
	public static class FileExtensions {
		public static string IncrementFilename(string path) {
			string baseName = Path.GetFileNameWithoutExtension(path);
			string extension = Path.GetExtension(path);
			string directory = Path.GetDirectoryName(path);
			
			int counter = 1;
			string testName = string.Format("{0}\\{1} ({2:00}){3}",
				directory,
				baseName,
				counter,
				extension);
			while (File.Exists(testName)) {
				counter++;
				testName = string.Format("{0}\\{1} ({2:00}){3}",
					directory,
					baseName,
					counter,
				extension);
			}
			return testName;
		}
	}
}
