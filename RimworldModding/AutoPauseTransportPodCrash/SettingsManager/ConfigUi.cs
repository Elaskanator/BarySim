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

		private readonly List<IFeatureSettings> _features;
		private Vector2 _scrollPosition = Vector2.zero;

		public ConfigUi(ModContentPack content) : base(content)
		{
			Settings = GetSettings<Settings>();

			// TODO factory pattern
			_features = new List<IFeatureSettings>
			{
				Settings.PodCrashSettings,
				Settings.AutoRetamingSettings,
			};
		}

		public override void DoSettingsWindowContents(Rect inRect)
		{
			float contentHeight = LayoutMetrics.MeasureContentHeight(_features.Select(x => x.AdditionalSettings));
			Rect viewRect = new Rect(0f, 0f, inRect.width - LayoutMetrics.ViewScrollbarWidth, contentHeight);

			Widgets.BeginScrollView(inRect, ref _scrollPosition, viewRect);

			var listing = new Listing_Standard();
			listing.Begin(viewRect);

			for (int i = 0; i < _features.Count; i++)
			{
				var feature = _features[i];
				InsertSection(listing, feature.AdditionalSettings, feature.FeatureLabel);

				if (i < _features.Count - 1)
					listing.Gap(LayoutMetrics.SectionGap);
			}

			listing.End();
			Widgets.EndScrollView();

			Settings.Write();
		}

		static void InsertSection(Listing_Standard listing, ISettingsBase sectionSettings, string headerLabel, int nestLevel = 0)
		{
			DrawCheckboxRow(
				listing,
				headerLabel,
				sectionSettings.Enabled,
				nestLevel,
				value => sectionSettings.Enabled = value);

			if (!sectionSettings.Enabled)
				return;

			foreach (var child in sectionSettings.ChildSections)
				InsertSection(listing, child.settings, child.headerLabel, nestLevel + 1);

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