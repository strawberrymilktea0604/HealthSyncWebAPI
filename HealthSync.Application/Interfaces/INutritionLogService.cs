using HealthSync.Application.DTOs.Nutrition;
using HealthSync.Application.DTOs;

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

    /// <summary>
    /// Lấy danh sách NutritionLog của user với phân trang
    /// </summary>
    /// <param name="userId">ID của user</param>
    /// <param name="pageNumber">Số trang</param>
    /// <param name="pageSize">Kích thước trang</param>
    /// <returns>Kết quả phân trang với danh sách NutritionLog</returns>
    Task<PaginatedResult<NutritionLogResponse>> GetNutritionLogsAsync(int userId, int pageNumber, int pageSize);

    /// <summary>
    /// Lấy NutritionLog theo ID
    /// </summary>
    /// <param name="userId">ID của user</param>
    /// <param name="nutritionLogId">ID của NutritionLog</param>
    /// <returns>Chi tiết NutritionLog hoặc null nếu không tìm thấy</returns>
    Task<NutritionLogResponse?> GetByIdAsync(int userId, int nutritionLogId);

    /// <summary>
    /// Cập nhật ghi chú của NutritionLog
    /// </summary>
    /// <param name="userId">ID của user</param>
    /// <param name="nutritionLogId">ID của NutritionLog</param>
    /// <param name="notes">Ghi chú mới</param>
    /// <returns>NutritionLogResponse đã được cập nhật</returns>
    Task<NutritionLogResponse> UpdateNotesAsync(int userId, int nutritionLogId, string? notes);

    /// <summary>
    /// Xóa NutritionLog
    /// </summary>
    /// <param name="userId">ID của user</param>
    /// <param name="nutritionLogId">ID của NutritionLog</param>
    /// <returns>True nếu xóa thành công, False nếu không tìm thấy</returns>
    Task<bool> DeleteAsync(int userId, int nutritionLogId);
}
