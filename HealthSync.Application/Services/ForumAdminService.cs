using HealthSync.Application.Interfaces;

namespace HealthSync.Application.Services;

public class ForumAdminService : IForumAdminService
{
    private readonly IForumPostRepository _forumPostRepository;

    public ForumAdminService(IForumPostRepository forumPostRepository)
    {
        _forumPostRepository = forumPostRepository;
    }

    public async Task<(bool Success, string Message)> DeletePostAsync(int postId, int adminId)
    {
        try
        {
            var postExists = await _forumPostRepository.ExistsAsync(postId);
            if (!postExists)
                return (false, "Post not found");

            await _forumPostRepository.DeleteAsync(postId);
            await _forumPostRepository.SaveChangesAsync();

            return (true, "Post deleted successfully");
        }
        catch (Exception ex)
        {
            return (false, $"Error deleting post: {ex.Message}");
        }
    }

    public async Task<(bool Success, string Message)> PinPostAsync(int postId, int adminId)
    {
        try
        {
            var post = await _forumPostRepository.GetByIdAsync(postId);
            if (post is null)
                return (false, "Post not found");

            post.IsPinned = true;
            post.UpdatedAt = DateTime.UtcNow;
            await _forumPostRepository.SaveChangesAsync();

            return (true, "Post pinned successfully");
        }
        catch (Exception ex)
        {
            return (false, $"Error pinning post: {ex.Message}");
        }
    }

    public async Task<(bool Success, string Message, bool IsPinned)> TogglePinPostAsync(int postId, int adminId)
    {
        try
        {
            var post = await _forumPostRepository.GetByIdAsync(postId);
            if (post is null)
                return (false, "Post not found", false);

            post.IsPinned = !post.IsPinned;
            post.UpdatedAt = DateTime.UtcNow;
            await _forumPostRepository.SaveChangesAsync();

            var message = post.IsPinned ? "Post pinned successfully" : "Post unpinned successfully";
            return (true, message, post.IsPinned);
        }
        catch (Exception ex)
        {
            return (false, $"Error toggling pin status: {ex.Message}", false);
        }
    }

    public async Task<(bool Success, string Message)> LockPostAsync(int postId, int adminId)
    {
        try
        {
            var post = await _forumPostRepository.GetByIdAsync(postId);
            if (post is null)
                return (false, "Post not found");

            post.IsLocked = true;
            post.UpdatedAt = DateTime.UtcNow;
            await _forumPostRepository.SaveChangesAsync();

            return (true, "Post locked successfully");
        }
        catch (Exception ex)
        {
            return (false, $"Error locking post: {ex.Message}");
        }
    }

    public async Task<(bool Success, string Message)> UnlockPostAsync(int postId, int adminId)
    {
        try
        {
            var post = await _forumPostRepository.GetByIdAsync(postId);
            if (post is null)
                return (false, "Post not found");

            post.IsLocked = false;
            post.UpdatedAt = DateTime.UtcNow;
            await _forumPostRepository.SaveChangesAsync();

            return (true, "Post unlocked successfully");
        }
        catch (Exception ex)
        {
            return (false, $"Error unlocking post: {ex.Message}");
        }
    }
}
