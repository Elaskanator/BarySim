using System;
using System.Diagnostics;

namespace Generic {
	public static class DebugExtensions {
		public static void DebugWriteline(string message) {
			Debug.WriteLine("{0} - {1}",
				DateTime.Now.Subtract(Singleton.Instance.Start).ToString(@"hh\:mm\:ss\.ffff"),
				message);
		}
		private sealed class Singleton {
			public readonly DateTime Start = DateTime.Now;
			public static Singleton Instance { get { return _instance.Value; } }
			private static readonly Lazy<Singleton> _instance = new(() => new Singleton());
		}
	}
}