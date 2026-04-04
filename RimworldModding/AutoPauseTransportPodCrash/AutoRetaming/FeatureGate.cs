using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace BunkRimworldTweaks.AutoRetaming
{
	public static class FeatureGate
	{
		static readonly Type RacePropertiesType = AccessTools.TypeByName("Verse.RaceProperties");
		static readonly PropertyInfo FenceBlockedProperty =
			RacePropertiesType?.GetProperty("FenceBlocked", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

		static readonly PropertyInfo AnimalProperty =
			RacePropertiesType?.GetProperty("Animal", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

		static readonly Type TrainabilityDefOfType = AccessTools.TypeByName("RimWorld.TrainabilityDefOf");
		static readonly FieldInfo TrainabilityNoneField =
			TrainabilityDefOfType?.GetField("None", BindingFlags.Static | BindingFlags.Public);

		public static bool IsEnabledFor(string defName)
		{
			var settings = SettingsManager.ConfigUi.Settings?.AutoRetamingSettings;
			if (settings == null)
				return true;

			if (!settings.MasterEnabled)
				return false;

			if (!settings.PropertiesEnabled.TryGetValue(defName, out var enabled))
			{
				settings.PropertiesEnabled[defName] = true;
				return true;
			}

			return enabled;
		}

		public static bool ShouldBlockAutoRetaming(Pawn animal, bool forced)
		{
			if (forced)
				return false;

			if (animal == null || animal.def == null || animal.Faction == null)
				return false;

			if (!animal.RaceProps.Animal)
				return false;

			return IsEnabledFor(animal.def.defName);
		}

		public static IEnumerable<ThingDef> GetApplicableAnimalDefs() =>
			DefDatabase<ThingDef>.AllDefs
				.Where(IsApplicableAnimalDef)
				.OrderBy(def => def.LabelCap.RawText);

		public static IEnumerable<ThingDef> GetPenAnimalDefs() =>
			GetApplicableAnimalDefs().Where(IsPenAnimalDef);

		public static IEnumerable<ThingDef> GetNonPenAnimalDefs() =>
			GetApplicableAnimalDefs().Where(def => !IsPenAnimalDef(def));

		public static bool IsPenAnimalDef(ThingDef def)
		{
			if (!IsApplicableAnimalDef(def))
				return false;

			// Prefer the game's runtime concept directly if present.
			var race = def.race;
			if (race != null && FenceBlockedProperty != null)
			{
				var value = FenceBlockedProperty.GetValue(race, null);
				if (value is bool b)
					return b;
			}

			// Conservative fallback:
			// Pen animals are the fence/pen-managed, non-trainable farm animals.
			var trainability = race?.trainability;
			var none = TrainabilityNoneField?.GetValue(null);
			return trainability != null && Equals(trainability, none);
		}

		static bool IsApplicableAnimalDef(ThingDef def)
		{
			if (def == null) return false;
			if (def.race == null) return false;
			if (def.category != ThingCategory.Pawn) return false; // any autonomous actor, not just colonists

			return def.race.Animal;
		}
	}
}