using System;

namespace HealthSync.Domain.Entities;

public class FoodEntry
{
    public int FoodEntryId { get; set; }

    // Parent daily log
    public int NutritionLogId { get; set; }
    public NutritionLog NutritionLog { get; set; } = null!;

    // Reference to library item
    public int FoodItemId { get; set; }
    public FoodItem FoodItem { get; set; } = null!;

    // Quantity consumed (in units of the FoodItem.ServingSize)
    public decimal Quantity { get; set; }

    // Meal type: Breakfast, Lunch, Dinner, Snack
    public string MealType { get; set; } = null!;

    // Snapshot of nutrition at the time of logging (per quantity)
    public decimal CaloriesKcal { get; set; }
    public decimal ProteinGrams { get; set; }
    public decimal CarbsGrams { get; set; }
    public decimal FatGrams { get; set; }
}
