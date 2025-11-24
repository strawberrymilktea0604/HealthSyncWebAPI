using HealthSync.Application.Interfaces;
using HealthSync.Domain.Entities;
using HealthSync.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HealthSync.Infrastructure.Repositories;

public class ForumPostRepository : IForumPostRepository
{
    private readonly ApplicationDbContext _context;

    public ForumPostRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Post?> GetByIdWithRepliesAsync(int postId)
    {
        return await _context.Posts
            .Include(p => p.Replies)
            .Include(p => p.User)
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.PostId == postId);
    }

    public async Task<Post?> GetByIdAsync(int postId)
    {
        return await _context.Posts
            .FirstOrDefaultAsync(p => p.PostId == postId);
    }

    public async Task<bool> ExistsAsync(int postId)
    {
        return await _context.Posts
            .AnyAsync(p => p.PostId == postId);
    }

    public async Task DeleteAsync(int postId)
    {
        var post = await GetByIdWithRepliesAsync(postId);
        if (post is not null)
        {
            // Cascade delete: replies will be deleted automatically if configured in DbContext
            _context.Posts.Remove(post);
        }
    }

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }
}
