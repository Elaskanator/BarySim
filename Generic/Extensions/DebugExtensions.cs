using System;
using System.Diagnostics;

namespace Generic.Extensions {
	public static class DebugExtensions {
		public static void DebugWriteline(this string message) {//is there really no built-in functionality to show the elapsed time when debug writing?!
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