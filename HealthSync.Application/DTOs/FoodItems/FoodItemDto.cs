namespace HealthSync.Application.DTOs.FoodItems;

public class FoodItemDto
{
    public int FoodItemId { get; set; }
    public string Name { get; set; } = null!;
    public string? Category { get; set; }
    public decimal ServingSize { get; set; }
    public string ServingUnit { get; set; } = "g";
    public decimal CaloriesPerServing { get; set; }
    public decimal ProteinG { get; set; }
    public decimal CarbsG { get; set; }
    public decimal FatG { get; set; }
    public decimal? FiberG { get; set; }
    public decimal? SugarG { get; set; }
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public int? CreatedByAdminId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}