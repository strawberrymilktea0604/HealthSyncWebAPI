using HealthSync.Domain.Entities;

namespace HealthSync.Application.Interfaces;

public interface IForumReplyRepository
{
    /// <summary>
    /// Get reply by ID
    /// </summary>
    Task<Reply?> GetByIdAsync(int replyId);

    /// <summary>
    /// Check if reply exists
    /// </summary>
    Task<bool> ExistsAsync(int replyId);

    /// <summary>
    /// Update an existing reply
    /// </summary>
    Task UpdateAsync(Reply reply);

    /// <summary>
    /// Delete reply
    /// </summary>
    Task DeleteAsync(int replyId);

    /// <summary>
    /// Get all replies
    /// </summary>
    Task<IEnumerable<Reply>> GetAllAsync();

    /// <summary>
    /// Save changes to database
    /// </summary>
    Task<int> SaveChangesAsync();
}