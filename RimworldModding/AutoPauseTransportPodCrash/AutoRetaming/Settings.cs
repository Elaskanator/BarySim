using System.Collections.Generic;
using Verse;

namespace BunkRimworldTweaks.AutoRetaming
{
	public sealed class Settings : IExposable, ISimpleSettings
	{
		private bool _masterEnabled = true;
		public bool MasterEnabled { get => _masterEnabled; set => _masterEnabled = value; }

		private Dictionary<string, bool> _propertiesEnabled = new Dictionary<string, bool>();
		public Dictionary<string, bool> PropertiesEnabled { get => _propertiesEnabled; }

		public void ExposeData()
		{
			Scribe_Values.Look(ref _masterEnabled, "Enabled", true);
			Scribe_Collections.Look(ref _propertiesEnabled, "AutoRetameByType", LookMode.Value, LookMode.Value);

			if (_propertiesEnabled == null)
				_propertiesEnabled = new Dictionary<string, bool>();
		}

		public string FriendlyName(string propertyName)
		{
			var def = DefDatabase<ThingDef>.GetNamedSilentFail(propertyName);
			if (def != null && !string.IsNullOrWhiteSpace(def.label))
				return def.LabelCap;

			var label = propertyName;
			label = label.Replace('_', ' ');
			label = Shared.EnumStringSplitterRegex.Replace(label, " ");
			label = Shared.MultiSpaceRegex.Replace(label, " ");

			return label.Trim();
		}
	}
}