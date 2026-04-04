using System.Collections.Generic;
using Verse;

namespace BunkRimworldTweaks.SettingsManager {
	internal static class LayoutMetrics
	{
		public const float ViewScrollbarWidth = 16f;

		public const float ContentTopPadding = 12f;
		public const float ContentBottomPadding = 0f;

		public const float SectionGap = 12f;

		public const float IndentWidth = 24f;
		public const float CheckboxSize = 24f;
		public const float LabelRightPadding = 30f;

		public static float EstimateContentHeight(IEnumerable<ISimpleSettings> settingsSections)
		{
			float line = Text.LineHeight;
			float total = ContentTopPadding;

			int i = 0;
			foreach (var section in settingsSections)
			{
				if (i > 0)
					total += SectionGap;

				total += line;

				if (section.MasterEnabled)
					total += section.PropertiesEnabled.Count * line;

				i++;
			}

			total += ContentBottomPadding;

			return total;
		}
	}
}
