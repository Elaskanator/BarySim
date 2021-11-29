using System;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace Generic.Extensions {
	public static class DebugExtensions {
		public static void DebugWriteline(this string message) {//is there really no built-in functionality to show the elapsed time when debug writing?!
			Debug.WriteLine("{0} - {1}",
				DateTime.UtcNow.Subtract(Singleton.Instance.StartUtc).ToString(@"hh\:mm\:ss\.ffff"),
				message);
		}
		public static void DebugWriteline_Interval(this string message, int trackId = 0) {//is there really no built-in functionality to show the elapsed time when debug writing?!
			Debug.WriteLine("{0} - {1}",
				DateTime.UtcNow.Subtract(Singleton.Instance.GetLastWriteUtc(trackId)).ToString(@"hh\:mm\:ss\.ffff"),
				message);
		}

		private sealed class Singleton {
			public readonly DateTime StartUtc = DateTime.UtcNow;
			private readonly ConcurrentDictionary<int, DateTime> _lastWritesUtc = new();

			public DateTime GetLastWriteUtc(int trackId = 0) {
				if (!this._lastWritesUtc.ContainsKey(trackId))
					this._lastWritesUtc[trackId] = this.StartUtc;

				DateTime result = this._lastWritesUtc[trackId];
				this._lastWritesUtc[trackId] = DateTime.UtcNow;
				return result;
			}

			public static Singleton Instance { get { return _instance.Value; } }
			private static readonly Lazy<Singleton> _instance = new(() => new Singleton());
		}
	}
}