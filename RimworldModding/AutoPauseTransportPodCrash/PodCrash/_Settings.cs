using System.Collections.Generic;
using Verse;

namespace BunkRimworldTweaks.PodCrash
{
	public sealed class Settings : IExposable, ISimpleSettings {
		private bool _masterEnabled = true;
		public bool MasterEnabled { get => _masterEnabled; set => _masterEnabled = value; }

		private Dictionary<string, bool> _propertiesEnabled = new Dictionary<string, bool>();
		public Dictionary<string, bool> PropertiesEnabled { get => _propertiesEnabled; }

		private static readonly List<(string headerLabel, ISimpleSettings settings)> _childSections = new List<(string headerLabel, ISimpleSettings settings)>();
		public IReadOnlyList<(string headerLabel, ISimpleSettings settings)> ChildSections => _childSections;

		public void ExposeData()
		{
			Scribe_Values.Look(ref _masterEnabled, "Enabled", true);
			Scribe_Collections.Look(ref _propertiesEnabled, "PauseByType", LookMode.Value, LookMode.Value);
		}

		public string FriendlyName(string propertyName)
		{
			var label = propertyName; // e.g. RimWorld.QuestGen.QuestNode_Root_RefugeePodCrash[_Ghoul]

			// strip namespace
			var lastDotIdx = label.LastIndexOf('.');
			if (lastDotIdx >= 0)
				label = label.Substring(lastDotIdx + 1);
			label = label.Replace("QuestNode_Root_", "");

			// strip shared label
			label = label.Replace("RefugeePodCrash", "");
			if (string.IsNullOrWhiteSpace(label))
				label = "Normal";

			// format remainder
			label = label.Replace('_', ' ');
			label = Shared.EnumStringSplitterRegex.Replace(label, " ");
			label = Shared.MultiSpaceRegex.Replace(label, " ");

			return label.Trim();
		}
	}
}
