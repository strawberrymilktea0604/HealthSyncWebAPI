using HealthSync.Application.DTOs;
using HealthSync.Application.DTOs.Nutrition;
using HealthSync.Application.Interfaces;
using HealthSync.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HealthSync.Infrastructure.Services;

/// <summary>
/// Service implementation cho Nutrition Log management
/// </summary>
public class NutritionLogService : INutritionLogService
{
    private readonly INutritionLogRepository _nutritionLogRepository;
    private readonly IFoodItemRepository _foodItemRepository;

    public NutritionLogService(INutritionLogRepository nutritionLogRepository, IFoodItemRepository foodItemRepository)
    {
        _nutritionLogRepository = nutritionLogRepository;
        _foodItemRepository = foodItemRepository;
    }

    /// <summary>
    /// Tạo hoặc lấy NutritionLog cho một ngày, cùng với list FoodEntry
    /// </summary>
    public async Task<NutritionLogResponse> CreateNutritionLogAsync(int userId, CreateNutritionLogRequest request)
    {
        // Xác định ngày (nếu không có thì dùng hôm nay)
        var logDate = (request.LogDate ?? DateTime.UtcNow).Date;

        // Lấy hoặc tạo NutritionLog cho ngày này
        var existingLog = await _nutritionLogRepository.GetByUserAndDateAsync(userId, logDate);
        
        NutritionLog nutritionLog;
        
        if (existingLog != null)
        {
            nutritionLog = existingLog;
            // Cập nhật notes nếu có
            if (!string.IsNullOrEmpty(request.Notes))
            {
                nutritionLog.Notes = request.Notes;
            }
        }
        else
        {
            // Tạo NutritionLog mới
            nutritionLog = new NutritionLog
            {
                UserId = userId,
                LogDate = logDate,
                Notes = request.Notes,
                TotalCalories = 0,
                TotalProteinG = 0,
                TotalCarbsG = 0,
                TotalFatG = 0,
                CreatedAt = DateTime.UtcNow
            };

            nutritionLog = await _nutritionLogRepository.CreateAsync(nutritionLog);
        }

        // Verify all food items exist
        if (request.FoodEntries != null && request.FoodEntries.Any())
        {
            var foodItemIds = request.FoodEntries.Select(fe => fe.FoodItemId).Distinct().ToList();
            var foodItems = await _foodItemRepository.GetByIdsAsync(foodItemIds);
            var foodItemDict = foodItems.ToDictionary(f => f.FoodItemId);

            var missingIds = foodItemIds.Except(foodItemDict.Keys).ToList();
            if (missingIds.Any())
            {
                throw new ArgumentException($"Food Item IDs not found: {string.Join(", ", missingIds)}");
            }

            // Thêm FoodEntry vào NutritionLog
            foreach (var foodEntryRequest in request.FoodEntries)
            {
                var foodItem = foodItemDict[foodEntryRequest.FoodItemId];

                // Tính toán nutrition dựa trên quantity
                var calories = foodEntryRequest.Quantity * foodItem.CaloriesPerServing;
                var proteinG = foodEntryRequest.Quantity * foodItem.ProteinG;
                var carbsG = foodEntryRequest.Quantity * foodItem.CarbsG;
                var fatG = foodEntryRequest.Quantity * foodItem.FatG;

                var foodEntry = new FoodEntry
                {
                    NutritionLogId = nutritionLog.NutritionLogId,
                    FoodItemId = foodEntryRequest.FoodItemId,
                    MealType = Enum.Parse<MealType>(foodEntryRequest.MealType),
                    Quantity = foodEntryRequest.Quantity,
                    Calories = calories,
                    ProteinG = proteinG,
                    CarbsG = carbsG,
                    FatG = fatG,
                    ConsumedAt = foodEntryRequest.ConsumedAt,
                    Notes = foodEntryRequest.Notes
                };

                await _nutritionLogRepository.AddFoodEntryAsync(foodEntry);

                // Cập nhật tổng của NutritionLog
                nutritionLog.TotalCalories += calories;
                nutritionLog.TotalProteinG += proteinG;
                nutritionLog.TotalCarbsG += carbsG;
                nutritionLog.TotalFatG += fatG;
            }

            // Cập nhật NutritionLog với tổng mới
            await _nutritionLogRepository.UpdateAsync(nutritionLog);
        }

        // Fetch lại NutritionLog để có dữ liệu mới nhất
        nutritionLog = await _nutritionLogRepository.GetByUserAndDateAsync(userId, logDate) 
            ?? throw new InvalidOperationException("Failed to retrieve created nutrition log");

        // Mapping sang Response
        return MapToResponse(nutritionLog);
    }

    /// <summary>
    /// Lấy danh sách NutritionLog của user với phân trang
    /// </summary>
    public async Task<PaginatedResult<NutritionLogResponse>> GetNutritionLogsAsync(int userId, int pageNumber, int pageSize)
    {
        var (logs, totalCount) = await _nutritionLogRepository.GetByUserIdAsync(userId, pageNumber, pageSize);

        var items = logs.Select(MapToResponse).ToList();

        return new PaginatedResult<NutritionLogResponse>(items, totalCount, pageNumber, pageSize);
    }

    /// <summary>
    /// Lấy NutritionLog theo ID
    /// </summary>
    public async Task<NutritionLogResponse?> GetByIdAsync(int userId, int nutritionLogId)
    {
        var nutritionLog = await _nutritionLogRepository.GetByIdAsync(nutritionLogId);

        if (nutritionLog == null || nutritionLog.UserId != userId)
        {
            return null;
        }

        return MapToResponse(nutritionLog);
    }

    /// <summary>
    /// Cập nhật ghi chú của NutritionLog
    /// </summary>
    public async Task<NutritionLogResponse> UpdateNotesAsync(int userId, int nutritionLogId, string? notes)
    {
        var nutritionLog = await _nutritionLogRepository.GetByIdAsync(nutritionLogId);

        if (nutritionLog == null || nutritionLog.UserId != userId)
        {
            throw new KeyNotFoundException($"Nutrition log with ID {nutritionLogId} not found for this user");
        }

        nutritionLog.Notes = notes;
        await _nutritionLogRepository.UpdateAsync(nutritionLog);

        // Reload để có dữ liệu mới nhất
        nutritionLog = await _nutritionLogRepository.GetByIdAsync(nutritionLogId)
            ?? throw new InvalidOperationException("Failed to retrieve updated nutrition log");

        return MapToResponse(nutritionLog);
    }

    /// <summary>
    /// Xóa NutritionLog
    /// </summary>
    public async Task<bool> DeleteAsync(int userId, int nutritionLogId)
    {
        var nutritionLog = await _nutritionLogRepository.GetByIdAsync(nutritionLogId);

        if (nutritionLog == null || nutritionLog.UserId != userId)
        {
            return false;
        }

        await _nutritionLogRepository.DeleteAsync(nutritionLogId);
        return true;
    }

    /// <summary>
    /// Mapping NutritionLog entity sang Response DTO
    /// </summary>
    private NutritionLogResponse MapToResponse(NutritionLog nutritionLog)
    {
        var entriesByMeal = new Dictionary<string, List<FoodEntryResponse>>();

        // Nhóm FoodEntry theo MealType
        foreach (var entry in nutritionLog.FoodEntries)
        {
            var mealTypeStr = entry.MealType.ToString();
            
            if (!entriesByMeal.ContainsKey(mealTypeStr))
            {
                entriesByMeal[mealTypeStr] = new List<FoodEntryResponse>();
            }

            entriesByMeal[mealTypeStr].Add(new FoodEntryResponse
            {
                FoodEntryId = entry.FoodEntryId,
                NutritionLogId = entry.NutritionLogId,
                FoodItem = new FoodItemResponse
                {
                    FoodItemId = entry.FoodItem.FoodItemId,
                    Name = entry.FoodItem.Name,
                    Category = entry.FoodItem.Category.ToString(),
                    ServingSize = entry.FoodItem.ServingSize,
                    ServingUnit = entry.FoodItem.ServingUnit.ToString(),
                    CaloriesPerServing = entry.FoodItem.CaloriesPerServing,
                    ProteinG = entry.FoodItem.ProteinG,
                    CarbsG = entry.FoodItem.CarbsG,
                    FatG = entry.FoodItem.FatG,
                    FiberG = entry.FoodItem.FiberG,
                    SugarG = entry.FoodItem.SugarG,
                    Description = entry.FoodItem.Description,
                    ImageUrl = entry.FoodItem.ImageUrl
                },
                MealType = entry.MealType.ToString(),
                Quantity = entry.Quantity,
                Calories = entry.Calories,
                ProteinG = entry.ProteinG,
                CarbsG = entry.CarbsG,
                FatG = entry.FatG,
                Notes = entry.Notes,
                ConsumedAt = entry.ConsumedAt,
                CreatedAt = DateTime.UtcNow
            });
        }

        // Tính macro breakdown (%)
        var macroBreakdown = CalculateMacroBreakdown(nutritionLog);

        var summary = new NutritionSummaryDto
        {
            LogDate = nutritionLog.LogDate,
            TotalCalories = nutritionLog.TotalCalories,
            TotalProteinG = nutritionLog.TotalProteinG,
            TotalCarbsG = nutritionLog.TotalCarbsG,
            TotalFatG = nutritionLog.TotalFatG,
            EntryCount = nutritionLog.FoodEntries.Count,
            MacroBreakdown = macroBreakdown
        };

        return new NutritionLogResponse
        {
            NutritionLogId = nutritionLog.NutritionLogId,
            UserId = nutritionLog.UserId,
            LogDate = nutritionLog.LogDate,
            TotalCalories = nutritionLog.TotalCalories,
            TotalProteinG = nutritionLog.TotalProteinG,
            TotalCarbsG = nutritionLog.TotalCarbsG,
            TotalFatG = nutritionLog.TotalFatG,
            EntriesByMeal = entriesByMeal,
            Summary = summary,
            Notes = nutritionLog.Notes,
            CreatedAt = nutritionLog.CreatedAt,
            UpdatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Tính toán tỷ lệ phần trăm macros dựa trên calo
    /// </summary>
    private MacroBreakdownDto CalculateMacroBreakdown(NutritionLog nutritionLog)
    {
        const decimal proteinCaloriesPerGram = 4;
        const decimal carbsCaloriesPerGram = 4;
        const decimal fatCaloriesPerGram = 9;

        if (nutritionLog.TotalCalories == 0)
        {
            return new MacroBreakdownDto
            {
                ProteinPercentage = 0,
                CarbsPercentage = 0,
                FatPercentage = 0
            };
        }

        var proteinCalories = nutritionLog.TotalProteinG * proteinCaloriesPerGram;
        var carbsCalories = nutritionLog.TotalCarbsG * carbsCaloriesPerGram;
        var fatCalories = nutritionLog.TotalFatG * fatCaloriesPerGram;

        return new MacroBreakdownDto
        {
            ProteinPercentage = Math.Round((proteinCalories / nutritionLog.TotalCalories) * 100, 2),
            CarbsPercentage = Math.Round((carbsCalories / nutritionLog.TotalCalories) * 100, 2),
            FatPercentage = Math.Round((fatCalories / nutritionLog.TotalCalories) * 100, 2)
        };
    }
}
