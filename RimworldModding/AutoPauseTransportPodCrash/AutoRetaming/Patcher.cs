using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace BunkRimworldTweaks.AutoRetaming
{
	[StaticConstructorOnStartup]
	public static class Patcher
	{
		static Patcher()
		{
			var settings = SettingsManager.ConfigUi.Settings?.AutoRetamingSettings;
			if (settings != null)
				foreach (var def in FeatureGate.GetApplicableAnimalDefs())
					if (!settings.PropertiesEnabled.ContainsKey(def.defName))
						settings.PropertiesEnabled[def.defName] = true;

			var harmony = new Harmony("bunk.rimworldtweaks");
			var target = typeof(WorkGiver_Tame).GetMethod(
				"HasJobOnThing",
				BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);

			var postfix = new HarmonyMethod(typeof(Patcher).GetMethod(nameof(Postfix), BindingFlags.Static | BindingFlags.NonPublic));

			if (target != null && !target.IsAbstract)
				harmony.Patch(target, postfix: postfix);
		}

		static void Postfix(ref bool __result, Pawn pawn, Thing t, bool forced)
		{
			if (!__result) return;
			if (forced) return;
			if (!(t is Pawn animal)) return;
			// Only suppress re-taming of already-owned animals.
			if (animal.Faction != pawn?.Faction) return;

			if (FeatureGate.ShouldBlockAutoRetaming(animal, forced))
				__result = false;
		}
	}
}