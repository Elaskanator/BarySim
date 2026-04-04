namespace BunkRimworldTweaks.AutoRetaming {
	internal enum AnimalType
	{
		Nuzzleable,
		Penned,
		Carnivore,
		Bugs,
		Other
	}
	
	internal static partial class AnimalTypeExtensions
	{
		public static string ToHeaderLabel(this AnimalType type) => type.ToString();
	}
}
