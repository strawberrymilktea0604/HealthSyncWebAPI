namespace HealthSync.Application.DTOs.Nutrition;

/// <summary>
/// DTO tóm tắt các chỉ số dinh dưỡng trong một ngày
/// (dùng để display trên UI, hiển thị tổng kết)
/// </summary>
public class NutritionSummaryDto
{
    /// <summary>
    /// Ngày của nhật ký
    /// </summary>
    public DateTime LogDate { get; set; }

    /// <summary>
    /// Tổng calo trong ngày
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
    /// Số entry (món ăn) được ghi trong ngày
    /// </summary>
    public int EntryCount { get; set; }

    /// <summary>
    /// Chi tiết macros tính phần trăm (tùy chọn hiển thị)
    /// </summary>
    public MacroBreakdownDto MacroBreakdown { get; set; } = null!;
}
