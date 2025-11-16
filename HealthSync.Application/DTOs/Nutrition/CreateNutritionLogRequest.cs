namespace HealthSync.Application.DTOs.Nutrition;

/// <summary>
/// DTO để tạo hoặc lấy NutritionLog cho một ngày cụ thể
/// </summary>
public class CreateNutritionLogRequest
{
    /// <summary>
    /// Ngày của nhật ký dinh dưỡng (format: yyyy-MM-dd hoặc datetime)
    /// Nếu không cung cấp, hệ thống sẽ dùng ngày hôm nay (today)
    /// </summary>
    public DateTime? LogDate { get; set; }

    /// <summary>
    /// Ghi chú chung cho ngày hôm nay (tùy chọn)
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Danh sách các FoodEntry (món ăn) để thêm vào NutritionLog
    /// </summary>
    public List<CreateFoodEntryRequest> FoodEntries { get; set; } = new List<CreateFoodEntryRequest>();
}
