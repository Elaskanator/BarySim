using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Generic.Extensions;

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

	public static bool IsEmpty(this DirectoryInfo dirInfo) {
		using (IEnumerator<FileSystemInfo> enumerator = dirInfo.EnumerateFileSystemInfos().GetEnumerator())
			return !enumerator.MoveNext();
	}
	public static bool IsEmpty(string directory) {
		return IsEmpty(new DirectoryInfo(directory));
	}

	//see https://stackoverflow.com/questions/309485/c-sharp-sanitize-file-name
	public static string Sanitize(string path, string replacement = null) {
		throw new Exception("untested");

		string invalidChars = Regex.Escape(new string(Path.GetInvalidFileNameChars()));
		string invalidRegStr = string.Format(@"([{0}]*\.+$)|([{0}]+)", invalidChars);

		return Regex.Replace(path, invalidRegStr, replacement ?? string.Empty);
	}

	public static void EmptyDirectory(this DirectoryInfo dirInfo) {
		foreach (FileInfo file in dirInfo.GetFiles())
			file.Delete();
		foreach (DirectoryInfo subdir in dirInfo.GetDirectories())
			subdir.Delete(true);
	}
	public static void EmptyDirectory(string directory) {
		EmptyDirectory(new DirectoryInfo(directory));
	}
}