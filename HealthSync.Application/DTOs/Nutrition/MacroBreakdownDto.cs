namespace HealthSync.Application.DTOs.Nutrition;

/// <summary>
/// Phân tích chi tiết tỷ lệ phần trăm các macros
/// </summary>
public class MacroBreakdownDto
{
    /// <summary>
    /// Phần trăm calo từ protein
    /// </summary>
    public decimal ProteinPercentage { get; set; }

    /// <summary>
    /// Phần trăm calo từ carbs
    /// </summary>
    public decimal CarbsPercentage { get; set; }

    /// <summary>
    /// Phần trăm calo từ fat
    /// </summary>
    public decimal FatPercentage { get; set; }
}
