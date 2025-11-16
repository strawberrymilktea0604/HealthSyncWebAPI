using HealthSync.Domain.Entities;

namespace HealthSync.Application.Interfaces;

/// <summary>
/// Repository interface cho NutritionLog
/// </summary>
public interface INutritionLogRepository
{
    /// <summary>
    /// Lấy NutritionLog theo user và ngày
    /// </summary>
    Task<NutritionLog?> GetByUserAndDateAsync(int userId, DateTime logDate);

    /// <summary>
    /// Tạo NutritionLog mới
    /// </summary>
    Task<NutritionLog> CreateAsync(NutritionLog nutritionLog);

    /// <summary>
    /// Cập nhật NutritionLog
    /// </summary>
    Task UpdateAsync(NutritionLog nutritionLog);

    /// <summary>
    /// Thêm FoodEntry vào NutritionLog
    /// </summary>
    Task AddFoodEntryAsync(FoodEntry foodEntry);
}
