using System.Collections.Generic;
using System.Linq;
using Verse;

namespace BunkRimworldTweaks.AutoRetaming
{
	internal static class FeatureGate
	{
		public static bool IsEnabledFor(string defName)
		{
			var root = SettingsManager.ConfigUi.Settings?.AutoRetamingSettings;
			if (root == null)
				return true;
			if (!root.MasterEnabled)
				return false;

			var def = DefDatabase<ThingDef>.GetNamedSilentFail(defName);
			if (def == null)
				return true;
			if (!def.IsApplicableAnimal())
				return true;

			var bucket = root.GetBucket(def.GetAnimalType());
			if (bucket == null)
				return true;
			if (!bucket.MasterEnabled)
				return false;
			if (!bucket.PropertiesEnabled.TryGetValue(defName, out var enabled))
			{
				bucket.PropertiesEnabled[defName] = true;
				return true;
			}

			return enabled;
		}

		public static bool AllowAutoRetaming(Pawn animal, bool forced) // false = do NOT maintain taming
		{
			if (forced)
				return true;
			if (animal == null || animal.def == null || animal.Faction == null)
				return true;
			if (!animal.RaceProps.Animal)
				return true;

			return IsEnabledFor(animal.def.defName);
		}

		public static IEnumerable<ThingDef> GetApplicableAnimalDefs() =>
			DefDatabase<ThingDef>.AllDefs
				.Where(def => def.IsApplicableAnimal())
				.OrderBy(def => def.LabelCap.RawText);
	}
}