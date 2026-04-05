using Verse;

namespace BunkRimworldTweaks {
	public interface IFeatureSettings : IExposable
	{
		bool Enabled { get; set; }
		string FeatureLabel { get; }

		/// <summary>
		/// OPTIONAL extra settings (will be NULL when none)
		/// </summary>
		ISettingsBase AdditionalSettings { get; }
	}

	public abstract class AFeatureSettingsBase : IFeatureSettings
	{
		bool _enabled = true;
		public bool Enabled { get => _enabled; set => _enabled = value; }

		public abstract string FeatureLabel { get; }

		public virtual ISettingsBase AdditionalSettings => null;

		public void ExposeData()
		{
			Scribe_Values.Look(ref _enabled, "Enabled", true);
			ExposeAdditionalParameters();
		}

		protected virtual void ExposeAdditionalParameters() { }

		public virtual bool IsEnabledFor(string key)
		{
			if (!Enabled)
				return false;

			var settings = AdditionalSettings;
			if (settings == null)
				return true;

			if (!settings.PropertiesEnabled.TryGetValue(key, out var enabled))
			{
				settings.PropertiesEnabled[key] = true;
				return true;
			}

			return enabled;
		}

		public virtual void EnsureDefaults() { }
	}
}
