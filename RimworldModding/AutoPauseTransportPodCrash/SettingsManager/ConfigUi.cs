using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace BunkRimworldTweaks.SettingsManager
{
	public sealed class ConfigUi : Mod
	{
		public static Settings Settings;

		public override string SettingsCategory() => "Bunk Rimworld Tweaks";

		private readonly List<(string headerLabel, ISimpleSettings settings)> _mySettings;
		private Vector2 _scrollPosition = Vector2.zero;

		public ConfigUi(ModContentPack content) : base(content)
		{
			Settings = GetSettings<Settings>();

			// TODO factory pattern
			_mySettings = new List<(string headerLabel, ISimpleSettings settings)>
			{
				("Pod crash auto-pause", Settings.PodCrashSettings),
				("Animal taming auto-maintenance", Settings.AutoRetamingSettings),
			};
		}

		public override void DoSettingsWindowContents(Rect inRect)
		{
			float contentHeight = LayoutMetrics.EstimateContentHeight(_mySettings.Select(x => x.settings));
			Rect viewRect = new Rect(0f, 0f, inRect.width - LayoutMetrics.ViewScrollbarWidth, contentHeight);

			Widgets.BeginScrollView(inRect, ref _scrollPosition, viewRect);

			var listing = new Listing_Standard();
			listing.Begin(viewRect);

			for (int i = 0; i < _mySettings.Count; i++)
			{
				var (headerLabel, settings) = _mySettings[i];
				InsertSection(listing, headerLabel, settings);

				if (i < _mySettings.Count - 1)
					listing.Gap(LayoutMetrics.SectionGap);
			}

			listing.End();
			Widgets.EndScrollView();

			Settings.Write();
		}

		static void InsertSection(Listing_Standard listing, string headerLabel, ISimpleSettings sectionSettings)
		{
			var master = sectionSettings.MasterEnabled;
			listing.CheckboxLabeled(headerLabel, ref master);
			sectionSettings.MasterEnabled = master;

			if (!sectionSettings.MasterEnabled)
				return;

			foreach (var key in sectionSettings.PropertiesEnabled.Keys.OrderBy(x => x).ToList())
			{
				var value = sectionSettings.PropertiesEnabled[key];
				var label = sectionSettings.FriendlyName(key);

				Rect row = listing.GetRect(Text.LineHeight);
				row.xMin += LayoutMetrics.IndentWidth;

				Rect labelRect = row;
				labelRect.xMax -= LayoutMetrics.LabelRightPadding;

				Rect checkboxRect = new Rect(
					row.xMax - LayoutMetrics.CheckboxSize,
					row.y,
					LayoutMetrics.CheckboxSize,
					row.height);

				Widgets.Label(labelRect, label);
				Widgets.Checkbox(checkboxRect.position, ref value);

				sectionSettings.PropertiesEnabled[key] = value;
			}
		}
	}
}