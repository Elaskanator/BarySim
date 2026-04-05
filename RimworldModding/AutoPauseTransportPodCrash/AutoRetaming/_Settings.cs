using System;
using System.Collections.Generic;
using Verse;

namespace BunkRimworldTweaks.AutoRetaming
{
	public sealed class Settings : AFeatureSettingsBase
	{
		public override string FeatureLabel => "Tamed animal births should have colonists maintain tameness";

		readonly RootSettings _additionalSettings = new RootSettings();
		public override ISettingsBase AdditionalSettings => _additionalSettings;

		public override void EnsureDefaults()
		{
			foreach (var def in AnimalTypeExtensions.GetApplicableAnimalDefs())
			{
				var bucket = GetBucket(def.GetAnimalType());
				if (!bucket.PropertiesEnabled.ContainsKey(def.defName))
					bucket.PropertiesEnabled[def.defName] = true;
			}
		}

		protected override void ExposeAdditionalParameters()
		{
			_additionalSettings.ExposeData();
		}

		internal SettingsBucket GetBucket(AnimalType animalType) => _additionalSettings.GetBucket(animalType);

		sealed class RootSettings : ISettingsBase, IExposable
		{
			bool _enabled = true;
			public bool Enabled { get => _enabled; set => _enabled = value; }
			
			public string FriendlyName(string propertyName) => propertyName;

			static readonly Dictionary<string, bool> _emptyPropertiesEnabled = new Dictionary<string, bool>();

			readonly List<(string headerLabel, ISettingsBase settings)> _childSections =
				new List<(string headerLabel, ISettingsBase settings)>();

			Dictionary<AnimalType, SettingsBucket> _animalTypeSettings = new Dictionary<AnimalType, SettingsBucket>();

			public Dictionary<string, bool> PropertiesEnabled => _emptyPropertiesEnabled;

			public IReadOnlyList<(string headerLabel, ISettingsBase settings)> ChildSections => _childSections;

			public void ExposeData()
			{
				List<AnimalType> keys = null;
				List<SettingsBucket> values = null;
				Scribe_Collections.Look(ref _animalTypeSettings, "AnimalTypeSettings", LookMode.Value, LookMode.Deep, ref keys, ref values);

				if (_animalTypeSettings == null)
					_animalTypeSettings = new Dictionary<AnimalType, SettingsBucket>();

				EnsureBuckets();
				RebuildChildSections();
			}

			internal SettingsBucket GetBucket(AnimalType animalType)
			{
				if (!_animalTypeSettings.TryGetValue(animalType, out var bucket) || bucket == null)
				{
					bucket = new SettingsBucket();
					_animalTypeSettings[animalType] = bucket;
				}

				return bucket;
			}

			void EnsureBuckets()
			{
				foreach (AnimalType animalType in Enum.GetValues(typeof(AnimalType)))
					if (!_animalTypeSettings.ContainsKey(animalType) || _animalTypeSettings[animalType] == null)
						_animalTypeSettings[animalType] = new SettingsBucket();
			}

			void RebuildChildSections()
			{
				_childSections.Clear();

				foreach (AnimalType animalType in Enum.GetValues(typeof(AnimalType)))
					_childSections.Add((
						headerLabel: animalType.ToHeaderLabel(),
						settings: GetBucket(animalType)));
			}
		}
	}
}