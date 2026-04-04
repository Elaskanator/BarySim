using System;
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
			float contentHeight = LayoutMetrics.MeasureContentHeight(_mySettings.Select(x => x.settings));
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

		static void InsertSection(Listing_Standard listing, string headerLabel, ISimpleSettings sectionSettings, int nestLevel = 0)
		{
			DrawCheckboxRow(
				listing,
				headerLabel,
				sectionSettings.MasterEnabled,
				nestLevel,
				value => sectionSettings.MasterEnabled = value);

			if (!sectionSettings.MasterEnabled)
				return;

			foreach (var child in sectionSettings.ChildSections)
				InsertSection(listing, child.headerLabel, child.settings, nestLevel + 1);

			foreach (var key in sectionSettings.PropertiesEnabled.Keys.OrderBy(x => x).ToList())
			{
				var propertyKey = key;

				DrawCheckboxRow(
					listing,
					sectionSettings.FriendlyName(propertyKey),
					sectionSettings.PropertiesEnabled[propertyKey],
					nestLevel + 1,
					value => sectionSettings.PropertiesEnabled[propertyKey] = value);
			}
		}

		static void DrawCheckboxRow(Listing_Standard listing, string label, bool currentValue, int nestLevel, Action<bool> onChanged)
		{
			Rect row = listing.GetRect(Text.LineHeight);

			float indent = LayoutMetrics.IndentWidth * nestLevel;

			Rect checkboxRect = new Rect(
				row.xMin + indent,
				row.y,
				LayoutMetrics.CheckboxSize,
				row.height);

			Rect labelRect = new Rect(
				checkboxRect.xMax + 6f,
				row.y,
				row.width - indent - LayoutMetrics.CheckboxSize - 6f,
				row.height);

			Widgets.Label(labelRect, label);

			var value = currentValue;
			Widgets.Checkbox(checkboxRect.position, ref value);

			onChanged(value);
		}
	}
}