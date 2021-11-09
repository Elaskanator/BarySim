using System;
using System.Diagnostics;
using System.Threading;

namespace Generic.Extensions {
	public static class ThreadExtensions {
		public const int MIN_SLEEP_DURATION_MS = 50;

		public static void SleepWithProgress(double waitAmount, bool clearWhenFinished = true) {
			Stopwatch stopwatch = new Stopwatch();
			stopwatch.Start();

			if (Console.CursorLeft > 0)
				Console.SetCursorPosition(0, Console.CursorTop + 1);
			int line = Console.CursorTop;
			double consoleWidth = Console.WindowWidth;
			double numSteps = Math.Min(consoleWidth, Math.Ceiling(waitAmount / (double)MIN_SLEEP_DURATION_MS));
			double sleepSize = waitAmount / numSteps;
		
			double totalSlept = 0;
			int totalProgress = 0;

			int progressIncrement;
			double offset;
			for (int i = 0; i < numSteps && stopwatch.ElapsedMilliseconds < waitAmount; i++) {
				progressIncrement = (int)Math.Round(Math.Min(consoleWidth, ((double)stopwatch.ElapsedMilliseconds / waitAmount * consoleWidth) - totalProgress));
				Console.Write(new string('.', progressIncrement));
				totalProgress += progressIncrement;
			
				offset = totalSlept - (double)stopwatch.ElapsedMilliseconds;//synchronize to stopwatch laps
				if (-offset < sleepSize) Thread.Sleep((int)(sleepSize + offset));
				totalSlept += sleepSize;
			}

			if (clearWhenFinished) ConsoleExtensions.ClearLine(line);

			stopwatch.Stop();
		}
	}
}