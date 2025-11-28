using HealthSync.Application.DTOs.Forum;

namespace HealthSync.Application.Interfaces;

public interface IForumAdminService
{
    /// <summary>
    /// Delete forum post (cascades to replies)
    /// </summary>
    /// <param name="postId">ID of post to delete</param>
    /// <param name="adminId">ID of admin performing deletion</param>
    /// <returns>Success response</returns>
    Task<(bool Success, string Message)> DeletePostAsync(int postId, int adminId);

    /// <summary>
    /// Pin a post to top of category
    /// </summary>
    Task<(bool Success, string Message)> PinPostAsync(int postId, int adminId);

    /// <summary>
    /// Toggle pin status of a post (pin if unpinned, unpin if pinned)
    /// </summary>
    /// <param name="postId">ID of post to toggle pin</param>
    /// <param name="adminId">ID of admin performing action</param>
    /// <returns>Success response with current pin status</returns>
    Task<(bool Success, string Message, bool IsPinned)> TogglePinPostAsync(int postId, int adminId);

    /// <summary>
    /// Lock a post (disable replies)
    /// </summary>
    Task<(bool Success, string Message)> LockPostAsync(int postId, int adminId);

    /// <summary>
    /// Unlock a post
    /// </summary>
    Task<(bool Success, string Message)> UnlockPostAsync(int postId, int adminId);
}
