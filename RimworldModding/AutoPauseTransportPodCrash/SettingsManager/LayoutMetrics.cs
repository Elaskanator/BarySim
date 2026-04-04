using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace BunkRimworldTweaks.SettingsManager
{
	internal static class LayoutMetrics
	{
		public const float ViewScrollbarWidth = 16f;

		public const float SectionGap = 12f;

		public const float IndentWidth = 24f;
		public const float CheckboxSize = 24f;
		public const float LabelRightPadding = 30f;

		public static float MeasureContentHeight(IEnumerable<ISimpleSettings> settingsSections)
		{
			var sections = settingsSections.ToList();

			var listing = new Listing_Standard(); // holds current position
			listing.Begin(new Rect(0f, 0f, 9999f, 999999f));

			for (int i = 0; i < sections.Count; i++)
			{
				AdvanceLayoutForSection(listing, sections[i]);

				if (i < sections.Count - 1)
					listing.Gap(SectionGap); // advance position
			}

			float height = listing.CurHeight;
			listing.End();

			return height;
		}

		private static void AdvanceLayoutForSection(Listing_Standard listing, ISimpleSettings section)
		{
			listing.GetRect(Text.LineHeight);

			if (!section.MasterEnabled)
				return;

			foreach (var child in section.ChildSections)
				AdvanceLayoutForSection(listing, child.settings);

			foreach (var _ in section.PropertiesEnabled)
				listing.GetRect(Text.LineHeight); // advance position
		}
	}
}