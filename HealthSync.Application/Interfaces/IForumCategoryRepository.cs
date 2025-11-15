using HealthSync.Domain.Entities;

namespace HealthSync.Application.Interfaces;

public interface IForumCategoryRepository
{
    // Basic CRUD operations
    Task<ForumCategory> AddAsync(ForumCategory category);
    Task<ForumCategory?> GetByIdAsync(int categoryId);
    Task<IEnumerable<ForumCategory>> GetAllAsync();
    Task UpdateAsync(ForumCategory category);
    Task DeleteAsync(int categoryId);
    
    // Additional query methods
    Task<bool> ExistsByNameAsync(string name);
    Task<bool> ExistsByNameAsync(string name, int excludeCategoryId);
    Task<bool> HasRelatedPostsAsync(int categoryId);
    
    // Optional: Query with ordering
    Task<IEnumerable<ForumCategory>> GetAllOrderedAsync();
    
    // Optional: Get category with posts
    Task<ForumCategory?> GetByIdWithPostsAsync(int categoryId);
    
    // Optional: Check if category exists
    Task<bool> ExistsAsync(int categoryId);
}