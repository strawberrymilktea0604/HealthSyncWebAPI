namespace HealthSync.Application.DTOs.Nutrition;

/// <summary>
/// DTO để tạo một FoodEntry mới (thêm món ăn vào NutritionLog)
/// </summary>
public class CreateFoodEntryRequest
{
    /// <summary>
    /// ID của loại thực phẩm từ thư viện
    /// </summary>
    public int FoodItemId { get; set; }

    /// <summary>
    /// Loại bữa ăn: Breakfast, Lunch, Dinner, Snack
    /// </summary>
    public string MealType { get; set; } = string.Empty;

    /// <summary>
    /// Số lượng khẩu phần đã ăn (ví dụ: 1.5 = 1.5 serving)
    /// </summary>
    public decimal Quantity { get; set; }

    /// <summary>
    /// Ghi chú về bữa ăn (tùy chọn)
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Thời điểm ăn (tùy chọn, nếu muốn ghi chép chính xác)
    /// </summary>
    public DateTime? ConsumedAt { get; set; }
}
