using HarmonyLib;
using Verse;

namespace BunkRimWorldTweaks
{
	[StaticConstructorOnStartup]
	public static class Bootstrap
	{
		static Bootstrap()
		{
			var harmony = new Harmony("bunk.rimworldtweaks");
			harmony.PatchAll();
		}
	}
}