namespace HealthSync.Application.DTOs.Nutrition;

/// <summary>
/// DTO để cập nhật ghi chú của NutritionLog
/// </summary>
public class UpdateNutritionLogNotesRequest
{
    /// <summary>
    /// Ghi chú mới cho NutritionLog
    /// </summary>
    public string? Notes { get; set; }
}
