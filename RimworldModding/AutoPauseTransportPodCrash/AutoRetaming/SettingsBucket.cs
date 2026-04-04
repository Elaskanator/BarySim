using System.Collections.Generic;
using Verse;

namespace BunkRimworldTweaks.AutoRetaming {
	internal sealed class SettingsBucket : IExposable, ISimpleSettings
	{
		private bool _masterEnabled = true;
		public bool MasterEnabled { get => _masterEnabled; set => _masterEnabled = value; }

		private Dictionary<string, bool> _propertiesEnabled = new Dictionary<string, bool>();
		public Dictionary<string, bool> PropertiesEnabled => _propertiesEnabled;

		private static readonly List<(string headerLabel, ISimpleSettings settings)> _childSections =
			new List<(string headerLabel, ISimpleSettings settings)>();

		public IReadOnlyList<(string headerLabel, ISimpleSettings settings)> ChildSections => _childSections;

		public void ExposeData()
		{
			Scribe_Values.Look(ref _masterEnabled, "Enabled", true);
			Scribe_Collections.Look(ref _propertiesEnabled, "PropertiesEnabled", LookMode.Value, LookMode.Value);

			if (_propertiesEnabled == null)
				_propertiesEnabled = new Dictionary<string, bool>();
		}

		public string FriendlyName(string propertyName)
		{
			var def = DefDatabase<ThingDef>.GetNamedSilentFail(propertyName);
			if (def != null && !string.IsNullOrWhiteSpace(def.label))
				return def.LabelCap;

			return propertyName;
		}
	}
}
