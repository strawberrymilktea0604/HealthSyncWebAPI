using HealthSync.Application.DTOs.Nutrition;
using HealthSync.Application.DTOs;
using HealthSync.Application.Interfaces;
using HealthSync.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace HealthSync.Application.Services;

/// <summary>
/// Service implementation cho Nutrition Log management
/// </summary>
public class NutritionLogService : INutritionLogService
{
    private readonly INutritionLogRepository _nutritionLogRepository;
    private readonly IFoodItemRepository _foodItemRepository;
    private readonly ILogger<NutritionLogService> _logger;

    public NutritionLogService(
        INutritionLogRepository nutritionLogRepository,
        IFoodItemRepository foodItemRepository,
        ILogger<NutritionLogService> logger)
    {
        _nutritionLogRepository = nutritionLogRepository;
        _foodItemRepository = foodItemRepository;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<NutritionLogResponse> CreateNutritionLogAsync(int userId, CreateNutritionLogRequest request)
    {
        _logger.LogInformation("Creating nutrition log for user {UserId} on date {Date}", userId, request.LogDate);

        // Check if log already exists for this date
        var logDate = request.LogDate ?? DateTime.UtcNow.Date;
        var existingLog = await _nutritionLogRepository.GetByUserAndDateAsync(userId, logDate);
        if (existingLog != null)
        {
            throw new InvalidOperationException($"Nutrition log already exists for date {request.LogDate:yyyy-MM-dd}");
        }

        // Create new nutrition log
        var nutritionLog = new NutritionLog
        {
            UserId = userId,
            LogDate = logDate,
            Notes = request.Notes,
            CreatedAt = DateTime.UtcNow
        };

        // Add food entries
        foreach (var entryRequest in request.FoodEntries)
        {
            var foodEntry = new FoodEntry
            {
                FoodItemId = entryRequest.FoodItemId,
                MealType = Enum.Parse<MealType>(entryRequest.MealType),
                Quantity = entryRequest.Quantity,
                ConsumedAt = entryRequest.ConsumedAt,
                Notes = entryRequest.Notes,
                CreatedAt = DateTime.UtcNow
            };

            nutritionLog.FoodEntries.Add(foodEntry);
        }

        // Calculate totals
        await CalculateNutritionTotalsAsync(nutritionLog);

        var createdLog = await _nutritionLogRepository.CreateAsync(nutritionLog);

        _logger.LogInformation("Created nutrition log {LogId} for user {UserId}", createdLog.NutritionLogId, userId);

        return MapToResponse(createdLog);
    }

    /// <inheritdoc />
    public async Task<PaginatedResult<NutritionLogResponse>> GetNutritionLogsAsync(int userId, int pageNumber, int pageSize)
    {
        _logger.LogInformation("Getting nutrition logs for user {UserId}, page {Page}, size {Size}", userId, pageNumber, pageSize);

        var (logs, totalCount) = await _nutritionLogRepository.GetByUserIdAsync(userId, pageNumber, pageSize);

        var responses = logs.Select(MapToResponse).ToList();

        return new PaginatedResult<NutritionLogResponse>
        {
            Items = responses,
            CurrentPage = pageNumber,
            PageSize = pageSize,
            TotalItems = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
            HasNext = pageNumber < (int)Math.Ceiling(totalCount / (double)pageSize),
            HasPrevious = pageNumber > 1
        };
    }

    /// <inheritdoc />
    public async Task<NutritionLogResponse?> GetByIdAsync(int userId, int nutritionLogId)
    {
        _logger.LogInformation("Getting nutrition log {LogId} for user {UserId}", nutritionLogId, userId);

        var log = await _nutritionLogRepository.GetByIdAsync(nutritionLogId);
        if (log == null || log.UserId != userId)
        {
            return null;
        }

        return MapToResponse(log);
    }

    /// <inheritdoc />
    public async Task<NutritionLogResponse> UpdateNotesAsync(int userId, int nutritionLogId, string? notes)
    {
        _logger.LogInformation("Updating notes for nutrition log {LogId}, user {UserId}", nutritionLogId, userId);

        var log = await _nutritionLogRepository.GetByIdAsync(nutritionLogId);
        if (log == null || log.UserId != userId)
        {
            throw new KeyNotFoundException($"Nutrition log {nutritionLogId} not found for user {userId}");
        }

        log.Notes = notes;
        await _nutritionLogRepository.UpdateAsync(log);

        return MapToResponse(log);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(int userId, int nutritionLogId)
    {
        _logger.LogInformation("Deleting nutrition log {LogId} for user {UserId}", nutritionLogId, userId);

        var log = await _nutritionLogRepository.GetByIdAsync(nutritionLogId);
        if (log == null || log.UserId != userId)
        {
            return false;
        }

        await _nutritionLogRepository.DeleteAsync(nutritionLogId);
        return true;
    }

    /// <inheritdoc />
    public async Task<NutritionLogResponse> GetOrCreateDailyLogAsync(int userId, DateTime date)
    {
        _logger.LogInformation("Getting or creating daily nutrition log for user {UserId} on {Date}", userId, date.Date);

        var log = await _nutritionLogRepository.GetByUserAndDateAsync(userId, date.Date);
        if (log != null)
        {
            return MapToResponse(log);
        }

        // Create new log if doesn't exist
        var newLog = new NutritionLog
        {
            UserId = userId,
            LogDate = date.Date,
            CreatedAt = DateTime.UtcNow
        };

        var createdLog = await _nutritionLogRepository.CreateAsync(newLog);

        _logger.LogInformation("Created new nutrition log {LogId} for user {UserId} on {Date}", createdLog.NutritionLogId, userId, date.Date);

        return MapToResponse(createdLog);
    }

    /// <inheritdoc />
    public async Task<FoodEntryResponse> AddFoodEntryAsync(int userId, DateTime date, CreateFoodEntryRequest request)
    {
        _logger.LogInformation("Adding food entry for user {UserId} on {Date}", userId, date.Date);

        // Get or create nutrition log for the date
        var log = await _nutritionLogRepository.GetByUserAndDateAsync(userId, date.Date);
        if (log == null)
        {
            log = new NutritionLog
            {
                UserId = userId,
                LogDate = date.Date,
                CreatedAt = DateTime.UtcNow
            };
            log = await _nutritionLogRepository.CreateAsync(log);
        }

        // Get food item to calculate nutrition values
        var foodItem = await _foodItemRepository.GetEntityByIdAsync(request.FoodItemId);
        if (foodItem == null)
        {
            throw new KeyNotFoundException($"Food item {request.FoodItemId} not found");
        }

        // Create food entry with calculated nutrition values
        var foodEntry = new FoodEntry
        {
            NutritionLogId = log.NutritionLogId,
            FoodItemId = request.FoodItemId,
            MealType = Enum.Parse<MealType>(request.MealType),
            Quantity = request.Quantity,
            Calories = request.Quantity * foodItem.CaloriesPerServing,
            ProteinG = request.Quantity * foodItem.ProteinG,
            CarbsG = request.Quantity * foodItem.CarbsG,
            FatG = request.Quantity * foodItem.FatG,
            ConsumedAt = request.ConsumedAt,
            Notes = request.Notes,
            CreatedAt = DateTime.UtcNow
        };

        await _nutritionLogRepository.AddFoodEntryAsync(foodEntry);

        // Recalculate totals
        await CalculateNutritionTotalsAsync(log);

        _logger.LogInformation("Added food entry {EntryId} to nutrition log {LogId}", foodEntry.FoodEntryId, log.NutritionLogId);

        return MapToFoodEntryResponse(foodEntry, foodItem);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteFoodEntryAsync(int userId, int entryId)
    {
        _logger.LogInformation("Deleting food entry {EntryId} for user {UserId}", entryId, userId);

        // Get the food entry with nutrition log to verify ownership
        var entry = await _nutritionLogRepository.GetFoodEntryByIdAsync(entryId);
        if (entry == null || entry.NutritionLog.UserId != userId)
        {
            return false;
        }

        var logId = entry.NutritionLogId;
        await _nutritionLogRepository.DeleteFoodEntryAsync(entryId);

        // Recalculate totals for the log
        var log = await _nutritionLogRepository.GetByIdAsync(logId);
        if (log != null)
        {
            await CalculateNutritionTotalsAsync(log);
        }

        return true;
    }

    private async Task CalculateNutritionTotalsAsync(NutritionLog log)
    {
        // Reload the log with food entries to ensure we have the latest data
        var updatedLog = await _nutritionLogRepository.GetByIdAsync(log.NutritionLogId);
        if (updatedLog == null)
        {
            return;
        }

        // Calculate totals from all food entries
        updatedLog.TotalCalories = updatedLog.FoodEntries.Sum(e => e.Calories);
        updatedLog.TotalProteinG = updatedLog.FoodEntries.Sum(e => e.ProteinG);
        updatedLog.TotalCarbsG = updatedLog.FoodEntries.Sum(e => e.CarbsG);
        updatedLog.TotalFatG = updatedLog.FoodEntries.Sum(e => e.FatG);

        // Update the log in repository
        await _nutritionLogRepository.UpdateAsync(updatedLog);
    }

    private NutritionLogResponse MapToResponse(NutritionLog log)
    {
        var response = new NutritionLogResponse
        {
            NutritionLogId = log.NutritionLogId,
            UserId = log.UserId,
            LogDate = log.LogDate,
            TotalCalories = log.TotalCalories,
            TotalProteinG = log.TotalProteinG,
            TotalCarbsG = log.TotalCarbsG,
            TotalFatG = log.TotalFatG,
            Notes = log.Notes,
            CreatedAt = log.CreatedAt
        };

        // Group food entries by meal type
        foreach (var entry in log.FoodEntries)
        {
            var mealTypeString = entry.MealType.ToString();
            if (!response.EntriesByMeal.ContainsKey(mealTypeString))
            {
                response.EntriesByMeal[mealTypeString] = new List<FoodEntryResponse>();
            }
            response.EntriesByMeal[mealTypeString].Add(MapToFoodEntryResponse(entry));
        }

        return response;
    }

    private FoodEntryResponse MapToFoodEntryResponse(FoodEntry entry, FoodItem? foodItem = null)
    {
        var item = foodItem ?? entry.FoodItem;
        if (item == null)
        {
            throw new InvalidOperationException("FoodItem data is required");
        }

        return new FoodEntryResponse
        {
            FoodEntryId = entry.FoodEntryId,
            NutritionLogId = entry.NutritionLogId,
            FoodItem = new FoodItemResponse
            {
                FoodItemId = item.FoodItemId,
                Name = item.Name,
                Category = item.Category.ToString(),
                ServingSize = item.ServingSize,
                ServingUnit = item.ServingUnit.ToString(),
                CaloriesPerServing = item.CaloriesPerServing,
                ProteinG = item.ProteinG,
                CarbsG = item.CarbsG,
                FatG = item.FatG
            },
            MealType = entry.MealType.ToString(),
            Quantity = entry.Quantity,
            Calories = entry.Calories,
            ProteinG = entry.ProteinG,
            CarbsG = entry.CarbsG,
            FatG = entry.FatG,
            ConsumedAt = entry.ConsumedAt,
            Notes = entry.Notes,
            CreatedAt = entry.CreatedAt
        };
    }
}