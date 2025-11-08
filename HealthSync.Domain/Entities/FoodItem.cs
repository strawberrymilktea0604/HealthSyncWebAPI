using System.Collections.Generic;

namespace HealthSync.Domain.Entities;

public class FoodItem
{
    public int FoodItemId { get; set; }

    // Basic info
    public string Name { get; set; } = null!;
    public string Category { get; set; }
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }

    // Serving information (e.g. 100, and unit = "g")
    public decimal ServingSize { get; set; }
    public ServingUnit ServingUnit { get; set; }

    // Nutrition per serving
    public decimal CaloriesPerServing { get; set; }
    public decimal ProteinG { get; set; }
    public decimal CarbsG { get; set; }
    public decimal FatG { get; set; }
    public decimal? FiberG { get; set; }
    public decimal? SugarG { get; set; }

    // Admin tracking
    public int CreatedByAdminId { get; set; }
    public ApplicationUser CreatedByAdmin { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation
    public ICollection<FoodEntry> FoodEntries { get; set; } = new List<FoodEntry>();
}
