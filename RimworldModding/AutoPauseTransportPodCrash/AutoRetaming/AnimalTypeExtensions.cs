using System;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace BunkRimworldTweaks.AutoRetaming
{
	internal static partial class AnimalTypeExtensions
	{
		private static readonly Type RacePropertiesType = AccessTools.TypeByName("Verse.RaceProperties");

		private static readonly PropertyInfo FenceBlockedProperty =
			RacePropertiesType?.GetProperty("FenceBlocked", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

		private static readonly FieldInfo NuzzleMtbHoursField =
			RacePropertiesType?.GetField("nuzzleMtbHours", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

		private static readonly PropertyInfo InsectProperty =
			RacePropertiesType?.GetProperty("Insect", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

		private static readonly FieldInfo InsectField =
			RacePropertiesType?.GetField("Insect", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

		private static readonly FieldInfo FoodTypeField =
			RacePropertiesType?.GetField("foodType", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

		private static readonly Type TrainabilityDefOfType = AccessTools.TypeByName("RimWorld.TrainabilityDefOf");

		private static readonly FieldInfo TrainabilityNoneField =
			TrainabilityDefOfType?.GetField("None", BindingFlags.Static | BindingFlags.Public);

		public static bool IsApplicableAnimal(this ThingDef def)
		{
			if (def == null)
				return false;
			if (def.category != ThingCategory.Pawn)
				return false;
			if (def.race == null)
				return false;
			if (!def.race.Animal)
				return false;

			return true;
		}

		public static AnimalType GetAnimalType(this ThingDef def)
		{
			if (!def.IsApplicableAnimal())
				throw new ArgumentException("ThingDef must be an applicable animal.", nameof(def));

			if (def.IsBugAnimal())
				return AnimalType.Bugs;
			if (def.IsNuzzleableAnimal())
				return AnimalType.Nuzzleable;
			if (def.IsPenAnimal())
				return AnimalType.Penned;
			if (def.IsCarnivoreAnimal())
				return AnimalType.Carnivore;

			return AnimalType.Other;
		}

		public static bool IsBugAnimal(this ThingDef def)
		{
			if (!def.IsApplicableAnimal())
				return false;

			var race = def.race;
			if (race == null)
				return false;

			if (InsectProperty != null)
			{
				var value = InsectProperty.GetValue(race, null);
				if (value is bool insectFromProperty)
					return insectFromProperty;
			}
			if (InsectField != null)
			{
				var value = InsectField.GetValue(race);
				if (value is bool insectFromField)
					return insectFromField;
			}

			return false;
		}

		public static bool IsNuzzleableAnimal(this ThingDef def)
		{
			if (!def.IsApplicableAnimal())
				return false;

			var race = def.race;
			if (race == null || NuzzleMtbHoursField == null)
				return false;

			var value = NuzzleMtbHoursField.GetValue(race);
			if (value is float f)
				return f > 0f;
			if (value is double d)
				return d > 0d;

			return false;
		}

		public static bool IsPenAnimal(this ThingDef def)
		{
			if (!def.IsApplicableAnimal())
				return false;

			var race = def.race;
			if (race != null && FenceBlockedProperty != null)
			{
				var value = FenceBlockedProperty.GetValue(race, null);
				if (value is bool fenceBlocked)
					return fenceBlocked;
			}

			var trainability = race?.trainability;
			var none = TrainabilityNoneField?.GetValue(null);

			return trainability != null && Equals(trainability, none);
		}

		public static bool IsCarnivoreAnimal(this ThingDef def)
		{
			if (!def.IsApplicableAnimal())
				return false;

			var race = def.race;
			if (race == null || FoodTypeField == null)
				return false;

			var value = FoodTypeField.GetValue(race);
			if (value == null)
				return false;

			string text = value.ToString();
			if (string.IsNullOrWhiteSpace(text))
				return false;

			return text.IndexOf("Carnivore", StringComparison.OrdinalIgnoreCase) >= 0
				|| text.IndexOf("Meat", StringComparison.OrdinalIgnoreCase) >= 0;
		}
	}
}