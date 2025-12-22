using System.ComponentModel.DataAnnotations;

namespace HealthSync.Application.DTOs.Nutrition;

/// <summary>
/// DTO để tạo một FoodEntry mới (thêm món ăn vào NutritionLog)
/// </summary>
public class CreateFoodEntryRequest
{
    /// <summary>
    /// ID của loại thực phẩm từ thư viện
    /// </summary>
    [Required(ErrorMessage = "FoodItemId is required")]
    [Range(1, int.MaxValue, ErrorMessage = "FoodItemId must be greater than 0")]
    public int FoodItemId { get; set; }

    /// <summary>
    /// Loại bữa ăn: Breakfast, Lunch, Dinner, Snack
    /// </summary>
    [Required(ErrorMessage = "MealType is required")]
    [RegularExpression("^(Breakfast|Lunch|Dinner|Snack)$", ErrorMessage = "MealType must be Breakfast, Lunch, Dinner, or Snack")]
    public string MealType { get; set; } = string.Empty;

    /// <summary>
    /// Số lượng khẩu phần đã ăn (ví dụ: 1.5 = 1.5 serving)
    /// </summary>
    [Required(ErrorMessage = "Quantity is required")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Quantity must be greater than 0")]
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
