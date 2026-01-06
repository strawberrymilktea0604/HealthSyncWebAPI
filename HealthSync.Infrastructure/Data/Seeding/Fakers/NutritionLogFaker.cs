using Bogus;
using HealthSync.Domain.Entities;

namespace HealthSync.Infrastructure.Data.Seeding.Fakers;

/// <summary>
/// Bogus faker for NutritionLog and FoodEntry entities.
/// Generates realistic nutrition data with proper macro calculations.
/// </summary>
public sealed class NutritionLogFaker
{
    // Private constructor to prevent instantiation
    private NutritionLogFaker() { }

    private static readonly Dictionary<MealType, (int minEntries, int maxEntries)> MealEntryRanges = new()
    {
        { MealType.Breakfast, (1, 3) },
        { MealType.Lunch, (2, 4) },
        { MealType.Dinner, (2, 4) },
        { MealType.Snack, (1, 2) }
    };

    public static NutritionLog GenerateWithEntries(
        int userId,
        DateTime logDate,
        IReadOnlyList<FoodItem> foodItems,
        Faker faker)
    {
        var nutritionLog = new NutritionLog
        {
            UserId = userId,
            LogDate = logDate.Date,
            Notes = faker.Random.Bool(0.1f) ? faker.Lorem.Sentence() : null,
            CreatedAt = logDate
        };

        // Generate entries for each meal type
        var mealTypes = new[] { MealType.Breakfast, MealType.Lunch, MealType.Dinner };
        
        foreach (var mealType in mealTypes)
        {
            var range = MealEntryRanges[mealType];
            var entryCount = faker.Random.Int(range.minEntries, range.maxEntries);
            var selectedFoods = SelectFoodsForMeal(mealType, foodItems, entryCount, faker);

            foreach (var food in selectedFoods)
            {
                var entry = GenerateFoodEntry(food, mealType, logDate, faker);
                nutritionLog.FoodEntries.Add(entry);
            }
        }

        // 50% chance to add snacks
        if (faker.Random.Bool(0.5f))
        {
            var snackCount = faker.Random.Int(1, 2);
            var snackFoods = SelectFoodsForMeal(MealType.Snack, foodItems, snackCount, faker);

            foreach (var food in snackFoods)
            {
                var entry = GenerateFoodEntry(food, MealType.Snack, logDate, faker);
                nutritionLog.FoodEntries.Add(entry);
            }
        }

        // Calculate totals
        CalculateTotals(nutritionLog);

        return nutritionLog;
    }

    private static IEnumerable<FoodItem> SelectFoodsForMeal(
        MealType mealType,
        IReadOnlyList<FoodItem> allFoods,
        int count,
        Faker faker)
    {
        // Prefer certain categories for certain meals
        var preferredCategories = mealType switch
        {
            MealType.Breakfast => new[] { "Carbs", "Dairy", "Fruits", "Protein" },
            MealType.Lunch => new[] { "Protein", "Carbs", "Vegetables" },
            MealType.Dinner => new[] { "Protein", "Vegetables", "Carbs" },
            MealType.Snack => new[] { "Snacks", "Fruits", "Dairy" },
            _ => Array.Empty<string>()
        };

        var preferredFoods = allFoods
            .Where(f => preferredCategories.Contains(f.Category))
            .ToList();

        if (preferredFoods.Count < count)
        {
            preferredFoods = allFoods.ToList();
        }

        return faker.PickRandom(preferredFoods, Math.Min(count, preferredFoods.Count));
    }

    private static FoodEntry GenerateFoodEntry(
        FoodItem food,
        MealType mealType,
        DateTime logDate,
        Faker faker)
    {
        // Quantity varies by serving unit
        var quantity = food.ServingUnit switch
        {
            ServingUnit.Piece => faker.Random.Decimal(1, 2),
            ServingUnit.Tablespoon or ServingUnit.Teaspoon => faker.Random.Decimal(1, 3),
            ServingUnit.Cup => faker.Random.Decimal(0.5m, 1.5m),
            _ => faker.Random.Decimal(0.5m, 2.0m) // Gram, Milliliter
        };

        quantity = Math.Round(quantity, 1);

        // Calculate nutrition based on quantity
        var calories = Math.Round(quantity * food.CaloriesPerServing, 1);
        var protein = Math.Round(quantity * food.ProteinG, 1);
        var carbs = Math.Round(quantity * food.CarbsG, 1);
        var fat = Math.Round(quantity * food.FatG, 1);

        // Generate consumed time based on meal type
        var consumedAt = mealType switch
        {
            MealType.Breakfast => logDate.Date.AddHours(faker.Random.Int(6, 9)),
            MealType.Lunch => logDate.Date.AddHours(faker.Random.Int(11, 13)),
            MealType.Dinner => logDate.Date.AddHours(faker.Random.Int(18, 20)),
            MealType.Snack => logDate.Date.AddHours(faker.Random.Int(14, 17)),
            _ => logDate
        };

        return new FoodEntry
        {
            FoodItemId = food.FoodItemId,
            MealType = mealType,
            Quantity = quantity,
            Calories = calories,
            ProteinG = protein,
            CarbsG = carbs,
            FatG = fat,
            ConsumedAt = consumedAt,
            CreatedAt = logDate
        };
    }

    private static void CalculateTotals(NutritionLog log)
    {
        log.TotalCalories = log.FoodEntries.Sum(e => e.Calories);
        log.TotalProteinG = log.FoodEntries.Sum(e => e.ProteinG);
        log.TotalCarbsG = log.FoodEntries.Sum(e => e.CarbsG);
        log.TotalFatG = log.FoodEntries.Sum(e => e.FatG);
    }
}
