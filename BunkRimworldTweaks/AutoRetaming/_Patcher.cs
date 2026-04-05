using System.Reflection;
using BunkRimworldTweaks.SettingsManager;
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
			var feature = ConfigUi.Settings?.AutoRetamingSettings;
			feature?.EnsureDefaults();

			var harmony = new Harmony("bunk.rimworldtweaks");
			var target = AccessTools.Method(typeof(PawnUtility), "TrySpawnHatchedOrBornPawn");
			var postfix = new HarmonyMethod(typeof(Patcher).GetMethod(nameof(Postfix), BindingFlags.Static | BindingFlags.NonPublic));

			if (target != null && !target.IsAbstract)
				harmony.Patch(target, postfix: postfix);
		}

		#pragma warning disable IDE0060 // motherOrEgg parameter unused but required by method signature
		static void Postfix(bool __result, Pawn pawn, Thing motherOrEgg)
		{
			if (!__result) return;
			if (pawn == null) return;
			
			//Log.Message($"[AutoRetaming] newborn={pawn.LabelShortCap} def={pawn.def.defName} type={pawn.def.GetAnimalType()}");

			if (!pawn.RaceProps.Animal) return;
			if (pawn.training == null) return;
			if (pawn.Faction != Faction.OfPlayer) return;

			var allowRetaming = FeatureGate.AllowAutoRetaming(pawn, false);
			//Log.Message($"[AutoRetaming] AllowRetaming={allowRetaming}");

			if (!allowRetaming)
				pawn.training.SetWantedRecursive(TrainableDefOf.Tameness, false);
		}
		#pragma warning restore IDE0060
	}
}