using System.Collections.Generic;

namespace HealthSync.Domain.Entities;

public class FoodItem
{
    public int FoodItemId { get; set; }

    // Basic info
    public string Name { get; set; } = null!;

    // Serving information (e.g. 100, and unit = "g")
    public decimal ServingSize { get; set; }
    public string? ServingUnit { get; set; }

    // Nutrition per serving
    public decimal CaloriesKcal { get; set; }
    public decimal ProteinGrams { get; set; }
    public decimal CarbsGrams { get; set; }
    public decimal FatGrams { get; set; }

    // Navigation
    public ICollection<FoodEntry> FoodEntries { get; set; } = new List<FoodEntry>();
}
