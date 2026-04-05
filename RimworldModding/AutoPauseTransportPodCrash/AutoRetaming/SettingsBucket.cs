using System.Collections.Generic;
using Verse;

namespace BunkRimworldTweaks.AutoRetaming
{
	internal sealed class SettingsBucket : AFlatSimpleSettingsBase
	{
		protected override void ExposeAdditionalParameters()
		{
			Scribe_Collections.Look(ref _propertiesEnabled, "PropertiesEnabled", LookMode.Value, LookMode.Value);

			if (_propertiesEnabled == null)
				_propertiesEnabled = new Dictionary<string, bool>();
		}

		public override string FriendlyName(string propertyName)
		{
			var def = DefDatabase<ThingDef>.GetNamedSilentFail(propertyName);
			if (def != null && !string.IsNullOrWhiteSpace(def.label))
				return def.LabelCap;

			return propertyName;
		}
	}
}