using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace BunkRimworldTweaks.AutoRetaming
{
	internal static partial class AnimalTypeExtensions
	{
		public static IEnumerable<ThingDef> GetApplicableAnimalDefs() =>
			DefDatabase<ThingDef>.AllDefs
				.Where(def => def.IsApplicableAnimal())
				.OrderBy(def => def.LabelCap.RawText);

		static readonly Type RacePropertiesType = AccessTools.TypeByName("Verse.RaceProperties");

		static readonly PropertyInfo FenceBlockedProperty =
			RacePropertiesType?.GetProperty("FenceBlocked", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		static readonly FieldInfo NuzzleMtbHoursField =
			RacePropertiesType?.GetField("nuzzleMtbHours", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		static readonly PropertyInfo InsectProperty =
			RacePropertiesType?.GetProperty("Insect", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		static readonly FieldInfo InsectField =
			RacePropertiesType?.GetField("Insect", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		static readonly FieldInfo FoodTypeField =
			RacePropertiesType?.GetField("foodType", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

		static readonly Type TrainabilityDefOfType = AccessTools.TypeByName("RimWorld.TrainabilityDefOf");

		static readonly FieldInfo TrainabilityNoneField =
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

			if (def.IsBug())
				return AnimalType.Bugs;
			if (def.IsNuzzleable())
				return AnimalType.Nuzzleable;
			if (def.IsLivestock())
				return AnimalType.Livestock;
			if (def.IsCarnivore())
				return AnimalType.Carnivore;
			if (def.IsVermin())
				return AnimalType.Vermin;

			return AnimalType.Other;
		}

		public static bool IsBug(this ThingDef def)
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

		public static bool IsNuzzleable(this ThingDef def)
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

		public static bool IsLivestock(this ThingDef def)
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

		public static bool IsCarnivore(this ThingDef def)
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

		public static bool IsVermin(this ThingDef def)
		{
			if (!def.IsApplicableAnimal())
				return false;
			if (def.race == null)
				return false;
			return def.race.baseBodySize <= 0.25f;
		}
	}
}