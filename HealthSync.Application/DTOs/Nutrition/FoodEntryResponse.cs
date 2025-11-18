namespace HealthSync.Application.DTOs.Nutrition;

/// <summary>
/// DTO trả về chi tiết một FoodEntry đã được tạo
/// </summary>
public class FoodEntryResponse
{
    /// <summary>
    /// ID của FoodEntry
    /// </summary>
    public int FoodEntryId { get; set; }

    /// <summary>
    /// ID của NutritionLog (ngày)
    /// </summary>
    public int NutritionLogId { get; set; }

    /// <summary>
    /// Thông tin FoodItem
    /// </summary>
    public FoodItemResponse FoodItem { get; set; } = null!;

    /// <summary>
    /// Loại bữa ăn
    /// </summary>
    public string MealType { get; set; } = string.Empty;

    /// <summary>
    /// Số lượng khẩu phần đã ăn
    /// </summary>
    public decimal Quantity { get; set; }

    /// <summary>
    /// Tổng calo tính toán = Quantity × FoodItem.CaloriesPerServing
    /// </summary>
    public decimal Calories { get; set; }

    /// <summary>
    /// Tổng protein = Quantity × FoodItem.ProteinG
    /// </summary>
    public decimal ProteinG { get; set; }

    /// <summary>
    /// Tổng carbs = Quantity × FoodItem.CarbsG
    /// </summary>
    public decimal CarbsG { get; set; }

    /// <summary>
    /// Tổng fat = Quantity × FoodItem.FatG
    /// </summary>
    public decimal FatG { get; set; }

    /// <summary>
    /// Ghi chú
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Thời điểm ăn
    /// </summary>
    public DateTime? ConsumedAt { get; set; }

    /// <summary>
    /// Thời điểm tạo entry này
    /// </summary>
    public DateTime CreatedAt { get; set; }
}
