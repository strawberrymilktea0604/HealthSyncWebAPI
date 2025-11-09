using HealthSync.Domain.Entities;

namespace HealthSync.Application.DTOs.FoodItems;

public class UpdateFoodItemRequest
{
    public string Name { get; set; } = string.Empty;
    public FoodCategory Category { get; set; }
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public decimal ServingSize { get; set; }
    public ServingUnit ServingUnit { get; set; }
    public decimal CaloriesPerServing { get; set; }
    public decimal ProteinG { get; set; }
    public decimal CarbsG { get; set; }
    public decimal FatG { get; set; }
    public decimal? FiberG { get; set; }
    public decimal? SugarG { get; set; }
}
