using Verse;

namespace BunkRimworldTweaks.PodCrash
{
	public sealed class Settings : AFeatureSettingsBase
	{
		public override string FeatureLabel => "Pod crash auto-pause";

		readonly SubSettings _additionalSettings = new SubSettings();
		public override ISettingsBase AdditionalSettings => _additionalSettings;

		public override void EnsureDefaults()
		{
			var settings = AdditionalSettings;
			if (settings == null)
				return;

			foreach (var type in Patcher.GetApplicableTypes())
				if (!settings.PropertiesEnabled.ContainsKey(type.FullName))
					settings.PropertiesEnabled[type.FullName] = true;
		}

		protected override void ExposeAdditionalParameters()
		{
			_additionalSettings.ExposeData();
		}

		sealed class SubSettings : AFlatSimpleSettingsBase
		{
			protected override void ExposeAdditionalParameters()
			{
				Scribe_Collections.Look(ref _propertiesEnabled, "PauseByType", LookMode.Value, LookMode.Value);
			}

			public override string FriendlyName(string propertyName)
			{
				var label = propertyName;

				var lastDotIdx = label.LastIndexOf('.');
				if (lastDotIdx >= 0)
					label = label.Substring(lastDotIdx + 1);
				label = label.Replace("QuestNode_Root_", "");

				label = label.Replace("RefugeePodCrash", "");
				if (string.IsNullOrWhiteSpace(label))
					label = "Normal";

				label = label.Replace('_', ' ');
				label = Shared.EnumStringSplitterRegex.Replace(label, " ");
				label = Shared.MultiSpaceRegex.Replace(label, " ");

				return label.Trim();
			}
		}
	}
}