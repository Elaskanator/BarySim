using System;
using System.Collections.Generic;
using Verse;

namespace BunkRimworldTweaks.AutoRetaming
{
	public sealed class Settings : IExposable, ISimpleSettings
	{
		private bool _masterEnabled = true;
		public bool MasterEnabled { get => _masterEnabled; set => _masterEnabled = value; }

		private static readonly Dictionary<string, bool> _emptyPropertiesEnabled = new Dictionary<string, bool>();
		public Dictionary<string, bool> PropertiesEnabled => _emptyPropertiesEnabled;

		private Dictionary<AnimalType, SettingsBucket> _animalTypeSettings = new Dictionary<AnimalType, SettingsBucket>();

		private readonly List<(string headerLabel, ISimpleSettings settings)> _childSections;
		public IReadOnlyList<(string headerLabel, ISimpleSettings settings)> ChildSections => _childSections;

		public Settings()
		{
			_childSections = new List<(string headerLabel, ISimpleSettings settings)>();
			RebuildChildSections();
		}

		public void ExposeData()
		{
			Scribe_Values.Look(ref _masterEnabled, "Enabled", true);

			List<AnimalType> keys = null;
			List<SettingsBucket> values = null;
			Scribe_Collections.Look(ref _animalTypeSettings, "AnimalTypeSettings", LookMode.Value, LookMode.Deep, ref keys, ref values);

			if (_animalTypeSettings == null)
				_animalTypeSettings = new Dictionary<AnimalType, SettingsBucket>();

			foreach (AnimalType animalType in Enum.GetValues(typeof(AnimalType)))
				if (!_animalTypeSettings.ContainsKey(animalType) || _animalTypeSettings[animalType] == null)
					_animalTypeSettings[animalType] = new SettingsBucket();

			RebuildChildSections();
		}

		public string FriendlyName(string propertyName) => propertyName;

		internal SettingsBucket GetBucket(AnimalType animalType)
		{
			if (!_animalTypeSettings.TryGetValue(animalType, out var bucket) || bucket == null)
			{
				bucket = new SettingsBucket();
				_animalTypeSettings[animalType] = bucket;
			}

			return bucket;
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