using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace BunkRimworldTweaks.PodCrash
{
	[StaticConstructorOnStartup]
	public static class Patcher
	{
		static Patcher()
		{
			var harmony = new Harmony("bunk.rimworldtweaks");
			var postfix = new HarmonyMethod(typeof(Patcher).GetMethod(nameof(PostfixPause), BindingFlags.Static | BindingFlags.NonPublic));

			var types = GetApplicableTypes().OrderBy(t => t.FullName).ToList();

			var settings = SettingsManager.ConfigUi.Settings;
			if (settings != null)
			{
				settings.PatchedQuestNodeCount = types.Count;

				if (settings.PodCrashSettings == null)
					settings.PodCrashSettings = new Settings();

				foreach (var type in types)
				{
					if (!settings.PodCrashSettings.PropertiesEnabled.ContainsKey(type.FullName))
						settings.PodCrashSettings.PropertiesEnabled[type.FullName] = true;
				}
			}

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

		static void PostfixPause(MethodBase __originalMethod)
		{
			var typeName = __originalMethod?.DeclaringType?.FullName;
			if (typeName == null)
				return;

			FeatureGate.PauseIfEnabled(typeName);
		}

		static IEnumerable<Type> GetApplicableTypes() =>
			typeof(Log).Assembly
				.GetTypes()
				.Where(t =>
					t != null &&
					t.Namespace == "RimWorld.QuestGen" &&
					t.Name.StartsWith("QuestNode_Root_", StringComparison.Ordinal) &&
					t.Name.IndexOf("PodCrash", StringComparison.Ordinal) >= 0);
	}
}