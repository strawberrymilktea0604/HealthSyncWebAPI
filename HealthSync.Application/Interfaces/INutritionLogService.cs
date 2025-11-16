using HealthSync.Application.DTOs.Nutrition;

namespace HealthSync.Application.Interfaces;

/// <summary>
/// Service interface cho Nutrition Log management
/// </summary>
public interface INutritionLogService
{
    /// <summary>
    /// Tạo hoặc lấy NutritionLog cho một ngày, cùng với list FoodEntry
    /// </summary>
    /// <param name="userId">ID của user</param>
    /// <param name="request">Request chứa logDate và list FoodEntry</param>
    /// <returns>NutritionLogResponse với tất cả chi tiết</returns>
    Task<NutritionLogResponse> CreateNutritionLogAsync(int userId, CreateNutritionLogRequest request);
}
