using HealthSync.Application.DTOs;
using HealthSync.Application.DTOs.FoodItems;
using HealthSync.Domain.Entities;

namespace HealthSync.Application.Interfaces;

public interface IFoodItemRepository
{
    Task<PaginatedResult<FoodItemDto>> SearchAsync(
        string? searchTerm, 
        string? category, 
        int page = 1, 
        int pageSize = 20);
    
    Task<FoodItemDto?> GetByIdAsync(int id);
    Task<FoodItem?> GetEntityByIdAsync(int id);
    Task<FoodItem> CreateAsync(FoodItem foodItem);
    Task<FoodItem> AddAsync(FoodItem foodItem);
    Task<FoodItem?> UpdateAsync(int id, FoodItem foodItem);
    Task UpdateAsync(FoodItem foodItem);
    Task<bool> DeleteAsync(int id);
    Task<bool> ExistsByNameAsync(string name);
    Task<bool> ExistsByNameAsync(string name, int excludeId);
    Task<bool> IsUsedInFoodEntriesAsync(int id);
}
