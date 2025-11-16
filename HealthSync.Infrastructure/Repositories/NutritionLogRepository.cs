using HealthSync.Application.Interfaces;
using HealthSync.Domain.Entities;
using HealthSync.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HealthSync.Infrastructure.Repositories;

/// <summary>
/// Repository implementation cho NutritionLog
/// </summary>
public class NutritionLogRepository : INutritionLogRepository
{
    private readonly ApplicationDbContext _context;

    public NutritionLogRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Lấy NutritionLog theo user và ngày (so sánh chỉ ngày, không giờ)
    /// </summary>
    public async Task<NutritionLog?> GetByUserAndDateAsync(int userId, DateTime logDate)
    {
        var logDateOnly = logDate.Date;

        return await _context.NutritionLogs
            .Include(nl => nl.FoodEntries)
            .ThenInclude(fe => fe.FoodItem)
            .FirstOrDefaultAsync(nl => nl.UserId == userId && nl.LogDate.Date == logDateOnly);
    }

    /// <summary>
    /// Tạo NutritionLog mới
    /// </summary>
    public async Task<NutritionLog> CreateAsync(NutritionLog nutritionLog)
    {
        _context.NutritionLogs.Add(nutritionLog);
        await _context.SaveChangesAsync();
        return nutritionLog;
    }

    /// <summary>
    /// Cập nhật NutritionLog
    /// </summary>
    public async Task UpdateAsync(NutritionLog nutritionLog)
    {
        _context.NutritionLogs.Update(nutritionLog);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Thêm FoodEntry vào NutritionLog
    /// </summary>
    public async Task AddFoodEntryAsync(FoodEntry foodEntry)
    {
        _context.FoodEntries.Add(foodEntry);
        await _context.SaveChangesAsync();
    }
}
