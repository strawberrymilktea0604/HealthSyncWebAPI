using HealthSync.Application.Interfaces;

namespace HealthSync.Application.Services;

public class ForumAdminService : IForumAdminService
{
    private const string PostNotFoundMessage = "Post not found";
    private readonly IForumPostRepository _forumPostRepository;
    private readonly IForumReplyRepository _forumReplyRepository;

    public ForumAdminService(
        IForumPostRepository forumPostRepository,
        IForumReplyRepository forumReplyRepository)
    {
        _forumPostRepository = forumPostRepository;
        _forumReplyRepository = forumReplyRepository;
    }

    public async Task<(bool Success, string Message)> DeletePostAsync(int postId, int adminId)
    {
        try
        {
            var postExists = await _forumPostRepository.ExistsAsync(postId);
            if (!postExists)
                return (false, PostNotFoundMessage);

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
                return (false, PostNotFoundMessage, false);

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
                return (false, PostNotFoundMessage);

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
                return (false, PostNotFoundMessage);

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

    public async Task<(bool Success, string Message)> HideReplyAsync(int replyId, int adminId)
    {
        try
        {
            var reply = await _forumReplyRepository.GetByIdAsync(replyId);
            if (reply is null)
                return (false, "Reply not found");

            reply.IsHidden = true;
            reply.UpdatedAt = DateTime.UtcNow;
            await _forumReplyRepository.SaveChangesAsync();

            return (true, "Reply hidden successfully");
        }
        catch (Exception ex)
        {
            return (false, $"Error hiding reply: {ex.Message}");
        }
    }

    public async Task<(bool Success, string Message)> DeleteReplyAsync(int replyId, int adminId)
    {
        try
        {
            var replyExists = await _forumReplyRepository.ExistsAsync(replyId);
            if (!replyExists)
                return (false, "Reply not found");

            await _forumReplyRepository.DeleteAsync(replyId);
            await _forumReplyRepository.SaveChangesAsync();

            return (true, "Reply deleted successfully");
        }
        catch (Exception ex)
        {
            return (false, $"Error deleting reply: {ex.Message}");
        }
    }
}
