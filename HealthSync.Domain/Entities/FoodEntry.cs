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

    // Meal type
    public MealType MealType { get; set; }

    // Snapshot of nutrition at the time of logging (per quantity)
    public decimal Calories { get; set; }
    public decimal ProteinG { get; set; }
    public decimal CarbsG { get; set; }
    public decimal FatG { get; set; }

    // Optional details
    public DateTime? ConsumedAt { get; set; }
    public string? Notes { get; set; }
}
