namespace HealthSync.Application.DTOs.Nutrition;

/// <summary>
/// DTO trả về thông tin đầy đủ một NutritionLog (nhật ký dinh dưỡng cho một ngày)
/// </summary>
public class NutritionLogResponse
{
    /// <summary>
    /// ID của NutritionLog
    /// </summary>
    public int NutritionLogId { get; set; }

    /// <summary>
    /// ID của User sở hữu nhật ký này
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// Ngày của nhật ký
    /// </summary>
    public DateTime LogDate { get; set; }

    /// <summary>
    /// Tổng calo trong ngày (auto-calculated)
    /// </summary>
    public decimal TotalCalories { get; set; }

    /// <summary>
    /// Tổng protein (g) trong ngày
    /// </summary>
    public decimal TotalProteinG { get; set; }

    /// <summary>
    /// Tổng carbs (g) trong ngày
    /// </summary>
    public decimal TotalCarbsG { get; set; }

    /// <summary>
    /// Tổng fat (g) trong ngày
    /// </summary>
    public decimal TotalFatG { get; set; }

    /// <summary>
    /// Danh sách các FoodEntry theo bữa ăn
    /// Nhóm theo: Breakfast, Lunch, Dinner, Snack
    /// </summary>
    public Dictionary<string, List<FoodEntryResponse>> EntriesByMeal { get; set; } = 
        new Dictionary<string, List<FoodEntryResponse>>();

    /// <summary>
    /// Tóm tắt dinh dưỡng với phân tích macros
    /// </summary>
    public NutritionSummaryDto Summary { get; set; } = null!;

    /// <summary>
    /// Ghi chú chung
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Thời điểm tạo nhật ký
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Thời điểm cập nhật gần nhất
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}
