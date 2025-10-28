using System;
using System.Collections.Generic;

namespace HealthSync.Domain.Entities;

public class NutritionLog
{
    public int NutritionLogId { get; set; }

    // Owner
    public int UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;

    // Date for the daily log
    public DateTime LogDate { get; set; }

    // Cached totals (calculated from FoodEntries)
    public decimal TotalCalories { get; set; }

    // Navigation
    public ICollection<FoodEntry> FoodEntries { get; set; } = new List<FoodEntry>();
}
