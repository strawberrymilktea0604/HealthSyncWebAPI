using Microsoft.EntityFrameworkCore;
using HealthSync.Application.DTOs;
using HealthSync.Application.DTOs.FoodItems;
using HealthSync.Application.Interfaces;
using HealthSync.Domain.Entities;
using HealthSync.Infrastructure.Data;

namespace HealthSync.Infrastructure.Repositories;

public class FoodItemRepository : IFoodItemRepository
{
    private readonly ApplicationDbContext _context;

    public FoodItemRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PaginatedResult<FoodItemDto>> SearchAsync(
        string? searchTerm,
        string? category,
        int page,
        int pageSize)
    {
        var query = _context.FoodItems.AsQueryable();

        // Apply search filter - case insensitive using LIKE
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var search = $"%{searchTerm}%";
            query = query.Where(f => 
                EF.Functions.Like(f.Name, search) || 
                (f.Description != null && EF.Functions.Like(f.Description, search)));
        }

        // Apply category filter
        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(f => f.Category == category);
        }

        // Get total count before pagination
        var totalItems = await query.CountAsync();

        // Apply pagination and ordering
        var items = await query
            .OrderBy(f => f.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(f => new FoodItemDto
            {
                FoodItemId = f.FoodItemId,
                Name = f.Name,
                Category = Enum.Parse<FoodCategory>(f.Category),
                Description = f.Description,
                ImageUrl = f.ImageUrl,
                ServingSize = f.ServingSize,
                ServingUnit = f.ServingUnit,
                CaloriesPerServing = f.CaloriesPerServing,
                ProteinG = f.ProteinG,
                CarbsG = f.CarbsG,
                FatG = f.FatG,
                FiberG = f.FiberG,
                SugarG = f.SugarG
            })
            .ToListAsync();

        return new PaginatedResult<FoodItemDto>(items, totalItems, page, pageSize);
    }

    public async Task<FoodItemDto?> GetByIdAsync(int id)
    {
        var foodItem = await _context.FoodItems
            .Where(f => f.FoodItemId == id)
            .Select(f => new FoodItemDto
            {
                FoodItemId = f.FoodItemId,
                Name = f.Name,
                Category = Enum.Parse<FoodCategory>(f.Category),
                Description = f.Description,
                ImageUrl = f.ImageUrl,
                ServingSize = f.ServingSize,
                ServingUnit = f.ServingUnit,
                CaloriesPerServing = f.CaloriesPerServing,
                ProteinG = f.ProteinG,
                CarbsG = f.CarbsG,
                FatG = f.FatG,
                FiberG = f.FiberG,
                SugarG = f.SugarG
            })
            .FirstOrDefaultAsync();

        return foodItem;
    }
}
