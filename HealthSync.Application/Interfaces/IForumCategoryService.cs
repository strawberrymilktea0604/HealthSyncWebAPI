using HealthSync.Application.DTOs.ForumCategories;

namespace HealthSync.Application.Interfaces;

public interface IForumCategoryService
{
    // CRUD operations
    Task<ForumCategoryDto> CreateAsync(CreateForumCategoryRequest request);
    Task<ForumCategoryDto?> GetByIdAsync(int categoryId);
    Task<IEnumerable<ForumCategoryDto>> GetAllAsync();
    Task<ForumCategoryDto> UpdateAsync(int categoryId, UpdateForumCategoryRequest request);
    Task<bool> DeleteAsync(int categoryId);
    
    // Validation methods
    Task<bool> ExistsByNameAsync(string name);
    Task<bool> ExistsByNameAsync(string name, int excludeCategoryId);
    Task<bool> HasRelatedPostsAsync(int categoryId);
    Task<bool> ExistsAsync(int categoryId);
    
    // Additional query methods
    Task<IEnumerable<ForumCategoryDto>> GetAllOrderedByDisplayOrderAsync();
    Task<ForumCategoryDto?> GetByIdWithPostsCountAsync(int categoryId);
}