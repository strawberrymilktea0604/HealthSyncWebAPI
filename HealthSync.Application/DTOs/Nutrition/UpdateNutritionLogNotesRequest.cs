using System.ComponentModel.DataAnnotations;

namespace HealthSync.Application.DTOs.Nutrition;

/// <summary>
/// DTO để cập nhật ghi chú của NutritionLog
/// </summary>
public class UpdateNutritionLogNotesRequest
{
    /// <summary>
    /// Ghi chú mới cho NutritionLog
    /// </summary>
    [Required(ErrorMessage = "Notes is required")]
    public string? Notes { get; set; }
}
