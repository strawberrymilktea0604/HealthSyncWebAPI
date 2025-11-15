using HealthSync.Application.DTOs.ForumCategories;
using HealthSync.Application.Interfaces;
using HealthSync.Domain.Entities;

namespace HealthSync.Application.Services;

public class ForumCategoryService : IForumCategoryService
{
    private readonly IForumCategoryRepository _forumCategoryRepository;

    public ForumCategoryService(IForumCategoryRepository forumCategoryRepository)
    {
        _forumCategoryRepository = forumCategoryRepository;
    }

    public async Task<ForumCategoryDto> CreateAsync(CreateForumCategoryRequest request)
    {
        // Check duplicate name
        if (await _forumCategoryRepository.ExistsByNameAsync(request.Name))
        {
            throw new InvalidOperationException("A category with this name already exists");
        }

        var entity = new ForumCategory
        {
            Name = request.Name,
            Description = request.Description ?? string.Empty,
            DisplayOrder = request.DisplayOrder ?? 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var created = await _forumCategoryRepository.AddAsync(entity);

        return MapToDto(created);
    }

    public async Task<ForumCategoryDto?> GetByIdAsync(int categoryId)
    {
        var entity = await _forumCategoryRepository.GetByIdAsync(categoryId);
        return entity != null ? MapToDto(entity) : null;
    }

    public async Task<IEnumerable<ForumCategoryDto>> GetAllAsync()
    {
        var entities = await _forumCategoryRepository.GetAllAsync();
        return entities.Select(MapToDto);
    }

    public async Task<ForumCategoryDto> UpdateAsync(int categoryId, UpdateForumCategoryRequest request)
    {
        var existingCategory = await _forumCategoryRepository.GetByIdAsync(categoryId);
        if (existingCategory == null)
        {
            throw new KeyNotFoundException("Forum category not found");
        }

        // Check duplicate name but exclude current category
        if (await _forumCategoryRepository.ExistsByNameAsync(request.Name, categoryId))
        {
            throw new InvalidOperationException("Another category with this name already exists");
        }

        existingCategory.Name = request.Name;
        existingCategory.Description = request.Description ?? existingCategory.Description;
        existingCategory.DisplayOrder = request.DisplayOrder ?? existingCategory.DisplayOrder;
        existingCategory.UpdatedAt = DateTime.UtcNow;

        await _forumCategoryRepository.UpdateAsync(existingCategory);

        return MapToDto(existingCategory);
    }

    public async Task<bool> DeleteAsync(int categoryId)
    {
        var category = await _forumCategoryRepository.GetByIdAsync(categoryId);
        if (category == null)
        {
            return false;
        }

        // Check if category has related posts
        if (await _forumCategoryRepository.HasRelatedPostsAsync(categoryId))
        {
            throw new InvalidOperationException("Cannot delete category with existing posts");
        }

        await _forumCategoryRepository.DeleteAsync(categoryId);
        return true;
    }

    public async Task<bool> ExistsByNameAsync(string name)
    {
        return await _forumCategoryRepository.ExistsByNameAsync(name);
    }

    public async Task<bool> ExistsByNameAsync(string name, int excludeCategoryId)
    {
        return await _forumCategoryRepository.ExistsByNameAsync(name, excludeCategoryId);
    }

    public async Task<bool> HasRelatedPostsAsync(int categoryId)
    {
        return await _forumCategoryRepository.HasRelatedPostsAsync(categoryId);
    }

    public async Task<bool> ExistsAsync(int categoryId)
    {
        return await _forumCategoryRepository.ExistsAsync(categoryId);
    }

    public async Task<IEnumerable<ForumCategoryDto>> GetAllOrderedByDisplayOrderAsync()
    {
        var entities = await _forumCategoryRepository.GetAllOrderedAsync();
        return entities.Select(MapToDto);
    }

    public async Task<ForumCategoryDto?> GetByIdWithPostsCountAsync(int categoryId)
    {
        var entity = await _forumCategoryRepository.GetByIdWithPostsAsync(categoryId);
        return entity != null ? MapToDto(entity) : null;
    }

    private static ForumCategoryDto MapToDto(ForumCategory entity)
    {
        return new ForumCategoryDto
        {
            Id = entity.CategoryId,
            Name = entity.Name,
            Description = entity.Description,
            DisplayOrder = entity.DisplayOrder,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }
}