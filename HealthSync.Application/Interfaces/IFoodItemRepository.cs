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
}
