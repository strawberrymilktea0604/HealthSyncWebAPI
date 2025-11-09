using HealthSync.Application.DTOs;
using HealthSync.Application.DTOs.FoodItems;
using HealthSync.Application.Interfaces;
using HealthSync.Domain.Entities;
using Microsoft.AspNetCore.Http;

namespace HealthSync.Application.Services;

public class FoodItemService : IFoodItemService
{
    private readonly IFoodItemRepository _repository;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IUserRepository _userRepository; // Assuming you have this for user checks

    public FoodItemService(IFoodItemRepository repository, IHttpContextAccessor httpContextAccessor, IUserRepository userRepository)
    {
        _repository = repository;
        _httpContextAccessor = httpContextAccessor;
        _userRepository = userRepository;
    }

    public async Task<PaginatedResult<FoodItemDto>> SearchAsync(
        string? searchTerm, 
        string? category, 
        int page = 1, 
        int pageSize = 20)
    {
        return await _repository.SearchAsync(searchTerm, category, page, pageSize);
    }

    public async Task<FoodItemDto?> GetByIdAsync(int id)
    {
        return await _repository.GetByIdAsync(id);
    }

    public async Task<FoodItemDto> CreateAsync(CreateFoodItemRequest request)
    {
        // Get current user ID from JWT
        var userIdClaim = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedAccessException("Invalid user ID in token");
        }

        // Validate user exists
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            throw new KeyNotFoundException($"User with ID {userId} does not exist");
        }

        var foodItem = new FoodItem
        {
            Name = request.Name,
            Category = request.Category?.ToString(),
            Description = request.Description,
            ImageUrl = request.ImageUrl,
            ServingSize = request.ServingSize,
            ServingUnit = Enum.Parse<HealthSync.Domain.Entities.ServingUnit>(request.ServingUnit),
            CaloriesPerServing = request.CaloriesPerServing,
            ProteinG = request.ProteinG,
            CarbsG = request.CarbsG,
            FatG = request.FatG,
            FiberG = request.FiberG,
            SugarG = request.SugarG,
            CreatedByAdminId = userId, // Set here
        };

        var created = await _repository.CreateAsync(foodItem);

        return new FoodItemDto
        {
            FoodItemId = created.FoodItemId,
            Name = created.Name,
            Category = request.Category,
            Description = created.Description,
            ImageUrl = created.ImageUrl,
            ServingSize = created.ServingSize,
            ServingUnit = created.ServingUnit.ToString(),
            CaloriesPerServing = created.CaloriesPerServing,
            ProteinG = created.ProteinG,
            CarbsG = created.CarbsG,
            FatG = created.FatG,
            FiberG = created.FiberG,
            SugarG = created.SugarG
        };
    }

    public async Task<FoodItemDto?> UpdateAsync(int id, UpdateFoodItemRequest request)
    {
        var foodItem = new FoodItem
        {
            Name = request.Name,
            Category = request.Category?.ToString(),
            Description = request.Description,
            ImageUrl = request.ImageUrl,
            ServingSize = request.ServingSize,
            ServingUnit = Enum.Parse<HealthSync.Domain.Entities.ServingUnit>(request.ServingUnit),
            CaloriesPerServing = request.CaloriesPerServing,
            ProteinG = request.ProteinG,
            CarbsG = request.CarbsG,
            FatG = request.FatG,
            FiberG = request.FiberG,
            SugarG = request.SugarG
        };

        var updated = await _repository.UpdateAsync(id, foodItem);
        if (updated == null)
        {
            return null;
        }

        return new FoodItemDto
        {
            FoodItemId = updated.FoodItemId,
            Name = updated.Name,
            Category = request.Category,
            Description = updated.Description,
            ImageUrl = updated.ImageUrl,
            ServingSize = updated.ServingSize,
            ServingUnit = updated.ServingUnit.ToString(),
            CaloriesPerServing = updated.CaloriesPerServing,
            ProteinG = updated.ProteinG,
            CarbsG = updated.CarbsG,
            FatG = updated.FatG,
            FiberG = updated.FiberG,
            SugarG = updated.SugarG
        };
    }

    public async Task<bool> DeleteAsync(int id)
    {
        return await _repository.DeleteAsync(id);
    }
}
