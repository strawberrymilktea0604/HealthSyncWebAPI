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
        int page = 1,
        int pageSize = 20)
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
                Category = f.Category,
                Description = f.Description,
                ImageUrl = f.ImageUrl,
                ServingSize = f.ServingSize,
                ServingUnit = f.ServingUnit.ToString(),
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
                Category = f.Category,
                Description = f.Description,
                ImageUrl = f.ImageUrl,
                ServingSize = f.ServingSize,
                ServingUnit = f.ServingUnit.ToString(),
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

    public async Task<FoodItem?> GetEntityByIdAsync(int id)
    {
        return await _context.FoodItems.FindAsync(id);
    }

    public async Task<FoodItem> CreateAsync(FoodItem foodItem)
    {
        _context.FoodItems.Add(foodItem);
        await _context.SaveChangesAsync();
        return foodItem;
    }

    public async Task<FoodItem> AddAsync(FoodItem foodItem)
    {
        _context.FoodItems.Add(foodItem);
        await _context.SaveChangesAsync();
        return foodItem;
    }

    public async Task<FoodItem?> UpdateAsync(int id, FoodItem foodItem)
    {
        var existingItem = await _context.FoodItems.FindAsync(id);
        if (existingItem == null)
        {
            return null;
        }

        existingItem.Name = foodItem.Name;
        existingItem.Category = foodItem.Category;
        existingItem.Description = foodItem.Description;
        existingItem.ImageUrl = foodItem.ImageUrl;
        existingItem.ServingSize = foodItem.ServingSize;
        existingItem.ServingUnit = foodItem.ServingUnit;
        existingItem.CaloriesPerServing = foodItem.CaloriesPerServing;
        existingItem.ProteinG = foodItem.ProteinG;
        existingItem.CarbsG = foodItem.CarbsG;
        existingItem.FatG = foodItem.FatG;
        existingItem.FiberG = foodItem.FiberG;
        existingItem.SugarG = foodItem.SugarG;

        await _context.SaveChangesAsync();
        return existingItem;
    }

    public async Task UpdateAsync(FoodItem foodItem)
    {
        _context.FoodItems.Update(foodItem);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var foodItem = await _context.FoodItems.FindAsync(id);
        if (foodItem == null)
        {
            return false;
        }

        _context.FoodItems.Remove(foodItem);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ExistsByNameAsync(string name)
    {
        return await _context.FoodItems.AnyAsync(f => f.Name.ToLower() == name.ToLower());
    }

    public async Task<bool> ExistsByNameAsync(string name, int excludeId)
    {
        return await _context.FoodItems.AnyAsync(f => f.Name.ToLower() == name.ToLower() && f.FoodItemId != excludeId);
    }

    public async Task<bool> IsUsedInFoodEntriesAsync(int id)
    {
        return await _context.FoodEntries.AnyAsync(fe => fe.FoodItemId == id);
    }
}
