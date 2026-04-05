using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BunkRimworldTweaks.SettingsManager;
using HarmonyLib;
using Verse;

namespace BunkRimworldTweaks.PodCrash
{
	[StaticConstructorOnStartup]
	public static class Patcher
	{
		static Patcher()
		{
			var types = GetApplicableTypes().OrderBy(t => t.FullName).ToList();

			var root = ConfigUi.Settings;
			if (root != null)
			{
				root.PatchedQuestNodeCount = types.Count;

				var feature = root.PodCrashSettings;
				if (feature == null)
				{
					feature = new Settings();
					root.PodCrashSettings = feature;
				}

				feature.EnsureDefaults();
			}
			
			var harmony = new Harmony("bunk.rimworldtweaks");
			var postfix = new HarmonyMethod(typeof(Patcher).GetMethod(nameof(Postfix), BindingFlags.Static | BindingFlags.NonPublic));
			foreach (var type in types)
			{
				var method = type.GetMethod(
					"RunInt",
					BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);

				if (method == null || method.IsAbstract)
					continue;

				harmony.Patch(method, postfix: postfix);
			}
		}

		static void Postfix(MethodBase __originalMethod)
		{
			var typeName = __originalMethod?.DeclaringType?.FullName;
			if (typeName == null)
				return;

			var settings = ConfigUi.Settings?.PodCrashSettings;
			if (settings == null)
				return;

			if (!settings.IsEnabledFor(typeName))
				return;

			Find.TickManager?.Pause();
		}

		internal static IEnumerable<Type> GetApplicableTypes() =>
			typeof(Log).Assembly
				.GetTypes()
				.Where(t =>
					t != null &&
					t.Namespace == "RimWorld.QuestGen" &&
					t.Name.StartsWith("QuestNode_Root_", StringComparison.Ordinal) &&
					t.Name.IndexOf("PodCrash", StringComparison.Ordinal) >= 0);
	}
}