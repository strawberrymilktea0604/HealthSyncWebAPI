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

    /// <summary>
    /// Lấy NutritionLog theo id (kèm FoodEntries + FoodItem)
    /// </summary>
    public async Task<NutritionLog?> GetByIdAsync(int nutritionLogId)
    {
        return await _context.NutritionLogs
            .Include(nl => nl.FoodEntries)
            .ThenInclude(fe => fe.FoodItem)
            .FirstOrDefaultAsync(nl => nl.NutritionLogId == nutritionLogId);
    }

    /// <summary>
    /// Lấy danh sách NutritionLog của user theo trang
    /// Trả về items + tổng số items
    /// Sắp xếp theo LogDate desc, CreatedAt desc
    /// </summary>
    public async Task<(List<NutritionLog> logs, int totalCount)> GetByUserIdAsync(int userId, int pageNumber, int pageSize)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100; // Max page size

        var query = _context.NutritionLogs
            .AsNoTracking()
            .Where(nl => nl.UserId == userId);

        var total = await query.CountAsync();

        var items = await query
            .Include(nl => nl.FoodEntries)
            .ThenInclude(fe => fe.FoodItem)
            .OrderByDescending(nl => nl.LogDate)
            .ThenByDescending(nl => nl.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, total);
    }

    /// <summary>
    /// Xóa NutritionLog theo id
    /// </summary>
    public async Task DeleteAsync(int nutritionLogId)
    {
        var nutritionLog = await _context.NutritionLogs.FindAsync(nutritionLogId);
        if (nutritionLog != null)
        {
            _context.NutritionLogs.Remove(nutritionLog);
            await _context.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Xóa FoodEntry theo id
    /// </summary>
    public async Task DeleteFoodEntryAsync(int foodEntryId)
    {
        var foodEntry = await _context.FoodEntries.FindAsync(foodEntryId);
        if (foodEntry != null)
        {
            _context.FoodEntries.Remove(foodEntry);
            await _context.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Lấy FoodEntry theo id với NutritionLog để check ownership
    /// </summary>
    public async Task<FoodEntry?> GetFoodEntryByIdAsync(int foodEntryId)
    {
        return await _context.FoodEntries
            .Include(fe => fe.NutritionLog)
            .FirstOrDefaultAsync(fe => fe.FoodEntryId == foodEntryId);
    }

    /// <summary>
    /// Get all nutrition logs
    /// </summary>
    public async Task<IEnumerable<NutritionLog>> GetAllAsync()
    {
        return await _context.NutritionLogs.ToListAsync();
    }
}
