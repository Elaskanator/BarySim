using Verse;

namespace BunkRimworldTweaks
{
	public sealed class Settings : ModSettings
	{
		public int PatchedQuestNodeCount = 0;

		public PodCrash.Settings PodCrashSettings = new PodCrash.Settings();
		public AutoRetaming.Settings AutoRetamingSettings = new AutoRetaming.Settings();

		public override void ExposeData()
		{
			Scribe_Deep.Look(ref PodCrashSettings, "PodCrash");
			Scribe_Deep.Look(ref AutoRetamingSettings, "AutoRetaming");

			Scribe_Values.Look(ref PatchedQuestNodeCount, "PatchedQuestNodeCount", 0);

			// TODO factory pattern
			if (PodCrashSettings == null)
				PodCrashSettings = new PodCrash.Settings();
			if (AutoRetamingSettings == null)
				AutoRetamingSettings = new AutoRetaming.Settings();

			base.ExposeData();
		}
	}
}