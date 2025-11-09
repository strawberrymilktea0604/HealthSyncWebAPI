using HealthSync.Application.DTOs;
using HealthSync.Application.DTOs.FoodItems;

namespace HealthSync.Application.Interfaces;

public interface IFoodItemService
{
    Task<PaginatedResult<FoodItemDto>> SearchAsync(
        string? searchTerm, 
        string? category, 
        int page = 1, 
        int pageSize = 20);
    
    Task<FoodItemDto?> GetByIdAsync(int id);
    Task<FoodItemDto> CreateAsync(CreateFoodItemRequest request);
    Task<FoodItemDto?> UpdateAsync(int id, UpdateFoodItemRequest request);
    Task<bool> DeleteAsync(int id);
}
