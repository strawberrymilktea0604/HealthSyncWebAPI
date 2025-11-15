using HealthSync.Application.Interfaces;
using HealthSync.Domain.Entities;
using HealthSync.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HealthSync.Infrastructure.Repositories;

public class ForumCategoryRepository : IForumCategoryRepository
{
    private readonly ApplicationDbContext _context;

    public ForumCategoryRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ForumCategory> AddAsync(ForumCategory category)
    {
        await _context.ForumCategories.AddAsync(category);
        await _context.SaveChangesAsync();
        return category;
    }

    public async Task<ForumCategory?> GetByIdAsync(int categoryId)
    {
        return await _context.ForumCategories
            .FirstOrDefaultAsync(fc => fc.CategoryId == categoryId);
    }

    public async Task<IEnumerable<ForumCategory>> GetAllAsync()
    {
        return await _context.ForumCategories
            .OrderBy(fc => fc.DisplayOrder)
            .ThenBy(fc => fc.Name)
            .ToListAsync();
    }

    public async Task UpdateAsync(ForumCategory category)
    {
        _context.ForumCategories.Update(category);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int categoryId)
    {
        var category = await GetByIdAsync(categoryId);
        if (category != null)
        {
            _context.ForumCategories.Remove(category);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> ExistsByNameAsync(string name)
    {
        return await _context.ForumCategories
            .AnyAsync(fc => fc.Name.ToLower() == name.ToLower());
    }

    public async Task<bool> ExistsByNameAsync(string name, int excludeCategoryId)
    {
        return await _context.ForumCategories
            .AnyAsync(fc => fc.Name.ToLower() == name.ToLower() 
                         && fc.CategoryId != excludeCategoryId);
    }

    public async Task<bool> HasRelatedPostsAsync(int categoryId)
    {
        return await _context.Posts
            .AnyAsync(p => p.CategoryId == categoryId);
    }

    public async Task<IEnumerable<ForumCategory>> GetAllOrderedAsync()
    {
        return await _context.ForumCategories
            .OrderBy(fc => fc.DisplayOrder)
            .ThenBy(fc => fc.Name)
            .ToListAsync();
    }

    public async Task<ForumCategory?> GetByIdWithPostsAsync(int categoryId)
    {
        return await _context.ForumCategories
            .Include(fc => fc.Posts)
            .FirstOrDefaultAsync(fc => fc.CategoryId == categoryId);
    }

    public async Task<bool> ExistsAsync(int categoryId)
    {
        return await _context.ForumCategories
            .AnyAsync(fc => fc.CategoryId == categoryId);
    }
}