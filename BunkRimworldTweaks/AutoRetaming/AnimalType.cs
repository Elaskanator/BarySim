namespace BunkRimworldTweaks.AutoRetaming {
	internal enum AnimalType
	{
		Nuzzleable,
		Livestock,
		Carnivore,
		Vermin,
		Bugs,
		Other
	}
	
	internal static partial class AnimalTypeExtensions
	{
		public static string ToHeaderLabel(this AnimalType type) => type.ToString();
	}
}
