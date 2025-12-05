using HealthSync.Domain.Entities;

namespace HealthSync.Application.Interfaces;

public interface IForumPostRepository
{
    /// <summary>
    /// Get post by ID with related replies (eager loading)
    /// </summary>
    Task<Post?> GetByIdWithRepliesAsync(int postId);

    /// <summary>
    /// Get post by ID
    /// </summary>
    Task<Post?> GetByIdAsync(int postId);

    /// <summary>
    /// Check if post exists
    /// </summary>
    Task<bool> ExistsAsync(int postId);

    /// <summary>
    /// Add a new post to the database
    /// </summary>
    Task<Post> AddAsync(Post post);

    /// <summary>
    /// Update an existing post
    /// </summary>
    Task UpdateAsync(Post post);

    /// <summary>
    /// Delete post (cascade delete replies)
    /// </summary>
    Task DeleteAsync(int postId);

    /// <summary>
    /// Get all posts
    /// </summary>
    Task<IEnumerable<Post>> GetAllPostsAsync();

    /// <summary>
    /// Save changes to database
    /// </summary>
    Task<int> SaveChangesAsync();
}
