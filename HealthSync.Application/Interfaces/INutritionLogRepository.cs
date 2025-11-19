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
    /// Lấy NutritionLog theo ID với tất cả quan hệ
    /// </summary>
    Task<NutritionLog?> GetByIdAsync(int nutritionLogId);

    /// <summary>
    /// Lấy danh sách NutritionLog của user với phân trang
    /// </summary>
    Task<(List<NutritionLog> logs, int totalCount)> GetByUserIdAsync(int userId, int pageNumber, int pageSize);

    /// <summary>
    /// Tạo NutritionLog mới
    /// </summary>
    Task<NutritionLog> CreateAsync(NutritionLog nutritionLog);

    /// <summary>
    /// Cập nhật NutritionLog
    /// </summary>
    Task UpdateAsync(NutritionLog nutritionLog);

    /// <summary>
    /// Xóa NutritionLog
    /// </summary>
    Task DeleteAsync(int nutritionLogId);

    /// <summary>
    /// Thêm FoodEntry vào NutritionLog
    /// </summary>
    Task AddFoodEntryAsync(FoodEntry foodEntry);

    /// <summary>
    /// Xóa FoodEntry
    /// </summary>
    Task DeleteFoodEntryAsync(int foodEntryId);

    /// <summary>
    /// Lấy FoodEntry theo id với NutritionLog để check ownership
    /// </summary>
    Task<FoodEntry?> GetFoodEntryByIdAsync(int foodEntryId);
}
