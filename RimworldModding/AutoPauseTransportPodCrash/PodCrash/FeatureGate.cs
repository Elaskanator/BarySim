using Verse;

namespace BunkRimworldTweaks.PodCrash
{
	public static class FeatureGate
	{
		public static bool IsEnabledFor(string fullTypeName)
		{
			var settings = SettingsManager.ConfigUi.Settings;
			if (settings == null || settings.PodCrashSettings == null)
				return true;

			if (!settings.PodCrashSettings.MasterEnabled)
				return false;

			if (!settings.PodCrashSettings.PropertiesEnabled.TryGetValue(fullTypeName, out var enabled))
			{
				settings.PodCrashSettings.PropertiesEnabled[fullTypeName] = true;
				return true;
			}

			return enabled;
		}

		public static void PauseIfEnabled(string fullTypeName)
		{
			if (!IsEnabledFor(fullTypeName))
				return;

			Find.TickManager?.Pause();
		}
	}
}